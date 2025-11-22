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
        public DbSet<GoogleDriveConfig> GoogleDriveConfigs { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mange-til-mange: SelfEvaluation <-> TaskOption
            modelBuilder.Entity<SelfEvaluation>()
                .HasMany(se => se.SelectedTask)
                .WithMany() // Ingen navigation i TaskOption
                .UsingEntity(j =>
                    j.ToTable("SelfEvaluationTasks"));

            // Cascade delete: AppUser -> SelfEvaluations
            modelBuilder.Entity<SelfEvaluation>()
                .HasOne(se => se.User)
                .WithMany(u => u.SelfEvaluations)
                .HasForeignKey(se => se.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}