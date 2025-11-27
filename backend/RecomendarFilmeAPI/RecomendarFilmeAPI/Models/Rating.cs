using System.ComponentModel.DataAnnotations.Schema;

namespace RecomendarFilmeAPI.Models
{
    [Table("ratings")] // Garante que o EF busque a tabela minúscula 'ratings'
    public class Rating
    {
        [Column("UserId")]
        public int UserId { get; set; }

        [Column("MovieId")]
        public int MovieId { get; set; }

        // O código espera 'Label', mas o banco tem 'Rating'. 
        // Fazemos o mapeamento aqui:
        [Column("Rating")]
        public float Label { get; set; }
    }
}