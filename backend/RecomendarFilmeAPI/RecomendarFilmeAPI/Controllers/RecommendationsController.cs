using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;
using Microsoft.EntityFrameworkCore;
using RecomendarFilmeAPI.Data;
using RecomendarFilmeAPI.Models;
using RecomendarFilmeAPI.Services;
using Microsoft.AspNetCore.Identity.Data;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization;

namespace MovieRecommenderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationsController : ControllerBase
    {
        private readonly PredictionEnginePool<HybridMovieRating, MovieRatingPrediction> _predictionEnginePool;
        private readonly IModelTrainingService _trainingService;
        private readonly MovieContext _dbContext;

        public RecommendationsController(
            PredictionEnginePool<HybridMovieRating, MovieRatingPrediction> predictionEnginePool,
            IModelTrainingService trainingService,
            MovieContext dbContext)
        {
            _predictionEnginePool = predictionEnginePool;
            _trainingService = trainingService;
            _dbContext = dbContext;
        }

        public class RequestLogin
        {
            public string? Name { get; set; }
            public string? Password { get; set; }
        }

        // POST: api/recommendations/train
        // Dispara o retreinamento manual
        [HttpPost("train")]
        public IActionResult Train()
        {
            try
            {
                var result = _trainingService.TrainModel();
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("predict")]
        public IActionResult Predict([FromBody] HybridMovieRating input)
        {
            Console.WriteLine($"Recebido -> User: {input.UserId}, Filme: {input.Year} - {input.Genre}");

            if (input == null) return BadRequest("Input is null");

            var prediction = _predictionEnginePool.Predict(modelName: "MovieRecommenderModel", example: input);

            return Ok(new
            {
                PredictedScore = prediction.Score,
                Input = input
            });
        }

        // GET: api/recommendations/recommend/1
        // Recomenda os top 5 filmes para o usuário
        [HttpGet("recommend/{userId}")]
        public async Task<IActionResult> Recommend(int userId)
        {
            // 1. Pegar filmes que o usuário AINDA NÃO avaliou
            var moviesWatchedIds = await _dbContext.Ratings
                .Where(r => r.UserId == userId)
                .Select(r => r.MovieId)
                .ToListAsync();

            var allMovies = await _dbContext.Movies
                .Where(m => !moviesWatchedIds.Contains(m.Id)) // Filtra vistos
                .Take(100) // Otimização: Limita a 100 candidatos para não travar a CPU
                .ToListAsync();

            var recommendations = new List<object>();

            // 2. Loop para prever a nota de cada filme candidato
            foreach (var movie in allMovies)
            {
                // Cria o input combinando o User com o Filme Candidato
                var predictionInput = new HybridMovieRating
                {
                    // MUDANÇA: userId vira string
                    UserId = userId.ToString(),

                    Year = (float)(movie.Year ?? 0),
                    Genre = movie.Genre ?? "",
                    Cast = movie.Elenco ?? ""
                };

                var prediction = _predictionEnginePool.Predict(modelName: "MovieRecommenderModel", example: predictionInput);

                recommendations.Add(new
                {
                    MovieId = movie.Id,
                    Title = movie.Title,
                    PredictedRating = prediction.Score
                });
            }

            // 3. Ordenar e pegar os melhores
            var top5 = recommendations
                .OrderByDescending(r => ((dynamic)r).PredictedRating)
                .Take(5);

            return Ok(top5);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] RequestLogin user)
        {
            var request = _dbContext.Users.Where(i => i.Name == user.Name && i.Password == user.Password).FirstOrDefault();

            if (request == null)
                return Unauthorized(new { message = "Nome de usuário ou senha inválidos." });
            else
                return Ok(new { userId = request.Id });
        }

        //[HttpGet("movies")]
        //public async Task<IActionResult> GetAllMovies()
        //{
        //    var movies = await _dbContext.Movies.Take(100).ToListAsync();
        //    return Ok(movies);
        //}

        [HttpGet("movies/{userId}")]
        public async Task<IActionResult> GetMoviesWithRating(int userId)
        {
            // 1. Pega todos os filmes
            var allMovies = await _dbContext.Movies.ToListAsync();

            // 2. Pega as avaliações DESSE usuário específico
            var userRatings = await _dbContext.Ratings
                .Where(r => r.UserId == userId)
                .ToDictionaryAsync(r => r.MovieId, r => r.Label);

            // 3. Combina os dados
            var result = allMovies.Select(m => new MovieWithUserRatingDto
            {
                Id = m.Id,
                Title = m.Title,
                Year = m.Year ?? 0,
                Genre = m.Genre,
                Photo = m.Photo, // Agora retornamos a foto!

                // Se o dicionário tiver nota para este filme, usa a nota. Se não, usa 0.
                MyRating = userRatings.ContainsKey(m.Id) ? userRatings[m.Id] : 0
            });

            return Ok(result);
        }

        [HttpPost("rate")]
        public async Task<IActionResult> AddRating([FromBody] RatingDto ratingDto)
        {
            // LOG DE DEBUG: Vai aparecer na tela preta do console
            Console.WriteLine($"[DEBUG RATE] Recebido -> User: {ratingDto.UserId}, Movie: {ratingDto.MovieId}, Nota: {ratingDto.Rating}");

            if (ratingDto.UserId == 0 || ratingDto.MovieId == 0)
            {
                Console.WriteLine("[ERRO] IDs zerados. Verifique o JSON enviado.");
                return BadRequest("IDs inválidos (zero).");
            }

            try
            {
                // Busca se já existe avaliação para esse par (User + Filme)
                var existingRating = await _dbContext.Ratings
                    .FirstOrDefaultAsync(r => r.UserId == ratingDto.UserId && r.MovieId == ratingDto.MovieId);

                if (existingRating != null)
                {
                    Console.WriteLine($"[DEBUG] Atualizando nota existente de {existingRating.Label} para {ratingDto.Rating}");
                    existingRating.Label = ratingDto.Rating;
                    _dbContext.Ratings.Update(existingRating); // Força a marcação de 'Modificado'
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Criando nova avaliação.");
                    var newRating = new Rating
                    {
                        UserId = ratingDto.UserId,
                        MovieId = ratingDto.MovieId,
                        Label = ratingDto.Rating
                    };
                    await _dbContext.Ratings.AddAsync(newRating);
                }

                var linhasAfetadas = await _dbContext.SaveChangesAsync();
                Console.WriteLine($"[SUCESSO] Linhas salvas no banco: {linhasAfetadas}");

                return Ok(new { message = "Avaliação salva com sucesso!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO FATAL SQL]: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"[DETALHE]: {ex.InnerException.Message}");

                return StatusCode(500, new { error = "Erro ao salvar no banco." });
            }
        }

        public class MovieWithUserRatingDto
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Genre { get; set; }
            public int Year { get; set; }
            public string Photo { get; set; }
            public float MyRating { get; set; }
        }

        public class RatingDto
        {
            [JsonPropertyName("userId")] // Garante que lê "userId" do JS
            public int UserId { get; set; }

            [JsonPropertyName("movieId")] // Garante que lê "movieId" do JS
            public int MovieId { get; set; }

            [JsonPropertyName("rating")] // Garante que lê "rating" do JS
            public float Rating { get; set; }
        }
    }
}