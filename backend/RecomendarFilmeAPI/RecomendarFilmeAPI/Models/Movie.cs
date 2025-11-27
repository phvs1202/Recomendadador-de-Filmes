using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecomendarFilmeAPI.Models
{
    [Table("movies")]
    public class Movie
    {
        [Key][Column("Id")] 
        public int Id { get; set; }

        [Column("Title")] 
        public string Title { get; set; }

        [Column("Year")] 
        public int? Year { get; set; }

        [Column("Genre")] 
        public string? Genre { get; set; }

        [Column("Elenco")] 
        public string? Elenco { get; set; }

        [Column("Photo")]
        public string? Photo { get; set; }
    }
}