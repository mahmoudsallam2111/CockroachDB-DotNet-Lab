namespace CrdbLab.Api.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Customer? Customer { get; set; }
}
