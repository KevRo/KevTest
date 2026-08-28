using Microsoft.EntityFrameworkCore;
using MyMVC.NetApp.Models.Strava;

namespace MyMVC.NetApp.Data;

public class StravaDbContext : DbContext
{
    public StravaDbContext(DbContextOptions<StravaDbContext> options) : base(options)
    {
    }

    public DbSet<StravaToken> StravaTokens => Set<StravaToken>();
    public DbSet<StravaAthlete> StravaAthletes => Set<StravaAthlete>();
    public DbSet<StravaActivity> StravaActivities => Set<StravaActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StravaToken>(entity =>
        {
            entity.HasKey(t => t.AthleteId);
            entity.Property(t => t.AthleteId).ValueGeneratedNever();
            entity.Property(t => t.AccessToken).IsRequired();
            entity.Property(t => t.RefreshToken).IsRequired();
        });

        modelBuilder.Entity<StravaAthlete>(entity =>
        {
            entity.Property(a => a.Id).ValueGeneratedNever();
            entity.Property(a => a.ProfileRawJson).IsRequired();
        });

        modelBuilder.Entity<StravaActivity>(entity =>
        {
            entity.Property(a => a.Id).ValueGeneratedNever();
            entity.Property(a => a.RawJson).IsRequired();
            entity.HasIndex(a => a.AthleteId);
        });
    }
}
