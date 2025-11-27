namespace RecomendarFilmeAPI.Models
{
    public class HybridMovieRating
    {
        // MUDANÇA: float -> string
        // Isso obriga o ML.NET a tratar o ID como uma categoria única e não um número.
        public string UserId { get; set; }
        public float Label { get; set; }
        public float Year { get; set; }
        public string Genre { get; set; }
        public string Cast { get; set; }
    }
}