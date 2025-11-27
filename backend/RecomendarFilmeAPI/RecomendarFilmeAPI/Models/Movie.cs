using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecomendarFilmeAPI.Models
{
    [Table("movies")] // Garante que o EF busque a tabela minúscula 'movies'
    public class Movie
    {
        [Key] // Define que este é o ID principal
        [Column("Id")]
        public int Id { get; set; }

        [Column("Title")]
        public string Title { get; set; }

        [Column("Year")]
        public int? Year { get; set; } // Nullable (int?) pois o banco pode ter nulos

        [Column("Genre")]
        public string Genre { get; set; }

        [Column("Elenco")]
        public string Elenco { get; set; }
    }
}