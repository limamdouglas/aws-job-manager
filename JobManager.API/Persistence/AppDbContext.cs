using JobManager.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobManager.API.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobApplication> JobApplications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Job>(e =>
        {
            e.HasKey(j => j.Id);

            e.HasMany(j => j.Applications)
                .WithOne(ja => ja.Job)
                .HasForeignKey(ja => ja.JobId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobApplication>(e =>
        {
            e.HasKey(ja => ja.Id);
        });
    }
}