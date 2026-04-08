namespace CreditsSim.Domain.Entities;

public class SimulationHistory
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public int TermMonths { get; set; }
    public decimal AnnualRate { get; set; }
    public string InstallmentType { get; set; } = "FIXED";
    public string ScheduleJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
