using Microsoft.EntityFrameworkCore;
using DiningPhilosophers.Persistence.Entities;

namespace DiningPhilosophers.Persistence
{
    public class SimulationDbContext : DbContext
    {
        public DbSet<SimulationRun> Runs => Set<SimulationRun>();
        public DbSet<PhilosopherStateEvent> PhilosopherStateEvents => Set<PhilosopherStateEvent>();
        public DbSet<ForkStateEvent> ForkStateEvents => Set<ForkStateEvent>();

        public SimulationDbContext(DbContextOptions<SimulationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SimulationRun>(b =>
            {
                b.HasKey(r => r.Id);
                b.HasMany(r => r.PhilosopherStateEvents).WithOne().HasForeignKey(e => e.RunId);
                b.HasMany(r => r.ForkStateEvents).WithOne().HasForeignKey(e => e.RunId);
            });

            modelBuilder.Entity<PhilosopherStateEvent>(b =>
            {
                b.HasKey(e => e.Id);
                b.HasIndex(e => new { e.RunId, e.TimestampUtc });
                b.Property(e => e.PhilosopherName).HasMaxLength(200);
                b.Property(e => e.State).HasMaxLength(100);
            });

            modelBuilder.Entity<ForkStateEvent>(b =>
            {
                b.HasKey(e => e.Id);
                b.HasIndex(e => new { e.RunId, e.TimestampUtc });
                b.Property(e => e.State).HasMaxLength(100);
                b.Property(e => e.Owner).HasMaxLength(200);
            });
        }
    }
}
