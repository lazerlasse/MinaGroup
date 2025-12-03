using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Models;

namespace MinaGroup.Backend.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Selfevaluation and taskoption entities.
        public DbSet<SelfEvaluation> SelfEvaluations { get; set; }
        public DbSet<TaskOption> TaskOptions { get; set; }


        // Organization entities.
        public DbSet<Organization> Organizations { get; set; } = default!;
        public DbSet<IntegrationProviderSettings> IntegrationProviderSettings { get; set; } = default!;
        public DbSet<OrganizationStorageIntegration> OrganizationStorageIntegrations { get; set; } = default!;
        public DbSet<GoogleDriveConfig> GoogleDriveConfigs { get; set; } = default!;


        // Global Sysadmin setting entities.
        public DbSet<GoogleDriveSystemSetting> GoogleDriveSystemSettings { get; set; } = default!;


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

            // Organization → Users
            modelBuilder.Entity<Organization>()
                .HasMany(o => o.Users)
                .WithOne(u => u.Organization)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrganizationStorageIntegration
            modelBuilder.Entity<OrganizationStorageIntegration>()
                .HasOne(o => o.Organization)
                .WithMany(o => o.StorageIntegrations)
                .HasForeignKey(o => o.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganizationStorageIntegration>()
                .HasIndex(o => new { o.OrganizationId, o.ProviderName })
                .IsUnique();

            modelBuilder.Entity<IntegrationProviderSettings>()
                .HasIndex(p => p.ProviderName)
                .IsUnique();

            // Default data for Google Drive Global Settings.
            modelBuilder.Entity<GoogleDriveSystemSetting>().HasData(new GoogleDriveSystemSetting
            {
                Id = 1,
                ClientId = string.Empty,
                EncryptedClientSecret = string.Empty,
                RedirectUri = string.Empty
            });
        }
    }
}