using Microsoft.ML;
using Microsoft.ML.Trainers;
using Microsoft.EntityFrameworkCore;
using RecomendarFilmeAPI.Data;
using RecomendarFilmeAPI.Models;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration; // Necessário para IConfiguration

namespace RecomendarFilmeAPI.Services
{
    // CORREÇÃO: Adicionado ": IModelTrainingService" para cumprir o contrato da Injeção de Dependência
    public class ModelTrainingService : IModelTrainingService
    {
        private readonly MovieContext _dbContext; 
        private readonly MLContext _mlContext;
        private readonly string _modelPath;

        public ModelTrainingService(MovieContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _mlContext = new MLContext();
            _modelPath = configuration["MLModelPath"];
        }

        public string TrainModel()
        {
            var dbData = (from rating in _dbContext.Ratings
                          join movie in _dbContext.Movies on rating.MovieId equals movie.Id
                          select new HybridMovieRating
                          {
                              // MUDANÇA: Converter para String
                              UserId = rating.UserId.ToString(),

                              Label = rating.Label,
                              Year = (float)(movie.Year ?? 0),
                              Cast = movie.Elenco ?? "",
                              Genre = movie.Genre ?? ""
                          }).AsNoTracking().ToList();

            if (!dbData.Any()) return "Sem dados para treinar.";

            IDataView trainingDataView = _mlContext.Data.LoadFromEnumerable(dbData);

            // O pipeline continua igual, mas agora o OneHotEncoding vai funcionar corretamente
            // pois a entrada é String.
            var pipeline = _mlContext.Transforms.Categorical.OneHotEncoding("UserIdOneHot", nameof(HybridMovieRating.UserId))
                .Append(_mlContext.Transforms.Text.FeaturizeText("GenreFeaturized", nameof(HybridMovieRating.Genre)))
                .Append(_mlContext.Transforms.Text.FeaturizeText("CastFeaturized", nameof(HybridMovieRating.Cast)))
                .Append(_mlContext.Transforms.Concatenate("Features", "UserIdOneHot", nameof(HybridMovieRating.Year), "GenreFeaturized", "CastFeaturized"))
                .Append(_mlContext.Regression.Trainers.FastTree(labelColumnName: nameof(HybridMovieRating.Label), featureColumnName: "Features", numberOfTrees: 50)); // FastTree é bom, mas requer dados.

            var model = pipeline.Fit(trainingDataView);

            // 4. Salvar Modelo
            // CORREÇÃO: Verifica se a pasta existe antes de salvar
            var folder = Path.GetDirectoryName(_modelPath);
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _mlContext.Model.Save(model, trainingDataView.Schema, _modelPath);

            return $"Modelo treinado com {dbData.Count} registros e salvo em {_modelPath}";
        }
    }
}