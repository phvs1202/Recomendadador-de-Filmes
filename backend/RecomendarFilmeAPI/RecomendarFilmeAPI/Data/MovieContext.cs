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
        public DbSet<users> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ESTE TRECHO É OBRIGATÓRIO PARA A TABELA RATINGS FUNCIONAR
            modelBuilder.Entity<Rating>()
                .HasKey(r => new { r.UserId, r.MovieId });

            base.OnModelCreating(modelBuilder);
        }
    }
}