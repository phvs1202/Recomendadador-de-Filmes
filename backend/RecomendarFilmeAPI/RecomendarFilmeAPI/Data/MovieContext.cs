using Microsoft.EntityFrameworkCore;
using RecomendarFilmeAPI.Models;

namespace RecomendarFilmeAPI.Data
{
    public class MovieContext : DbContext
    {
        public MovieContext(DbContextOptions<MovieContext> options) : base(options)
        {
        }

        public DbSet<Movie> Movies { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        // ADICIONE ESTE BLOCO PARA CONFIGURAR A CHAVE COMPOSTA
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Diz ao EF que a chave da tabela Ratings é composta por UserId e MovieId juntos
            modelBuilder.Entity<Rating>()
                .HasKey(r => new { r.UserId, r.MovieId });

            base.OnModelCreating(modelBuilder);
        }
    }
}