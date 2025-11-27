using Microsoft.ML.Data;

namespace RecomendarFilmeAPI.Models
{
    public class MovieRatingPrediction
    {
        [ColumnName("Score")] // Importante para o ML.NET saber que é o resultado
        public float Score { get; set; }
    }
}