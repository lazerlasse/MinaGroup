using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<SelfEvaluation> SelfEvaluations { get; set; }
        public DbSet<TaskOption> TaskOptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mange-til-mange: SelfEvaluation <-> TaskOption
            modelBuilder.Entity<SelfEvaluation>()
                .HasMany(se => se.SelectedTask)
                .WithMany() // Ingen navigation i TaskOption
                .UsingEntity(j =>
                    j.ToTable("SelfEvaluationTasks")); // Join-tabel
        }
    }
}
