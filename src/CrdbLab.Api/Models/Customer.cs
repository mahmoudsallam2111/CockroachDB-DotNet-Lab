namespace CrdbLab.Api.Models;

public class Customer
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<Transaction> Transactions { get; set; } = new();
}
