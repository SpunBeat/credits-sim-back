using CreditsSim.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditsSim.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<SimulationHistory> SimulationHistories => Set<SimulationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SimulationHistory>(entity =>
        {
            entity.ToTable("simulation_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            entity.Property(e => e.TermMonths).HasColumnName("term_months");
            entity.Property(e => e.AnnualRate).HasColumnName("annual_rate").HasColumnType("numeric(6,3)");
            entity.Property(e => e.InstallmentType).HasColumnName("installment_type").HasMaxLength(20);
            entity.Property(e => e.ScheduleJson).HasColumnName("schedule_json").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}
