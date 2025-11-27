using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ML;
using Microsoft.EntityFrameworkCore;
using RecomendarFilmeAPI.Data;
using RecomendarFilmeAPI.Models;
using RecomendarFilmeAPI.Services;

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

        // POST: api/recommendations/predict
        // Previsão para um cenário específico (JSON de entrada)
        [HttpPost("predict")]
        public IActionResult Predict([FromBody] HybridMovieRating input)
        {
            // --- ADICIONE ESTE BLOCO DE LOG ---
            Console.WriteLine($"Recebido -> User: {input.UserId}, Filme: {input.Year} - {input.Genre}");

            if (input == null) return BadRequest("Input is null");
            // ----------------------------------

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
    }
}