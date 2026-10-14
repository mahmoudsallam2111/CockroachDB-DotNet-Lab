namespace CrdbLab.Api;

public record CustomerRequest(string Name);
public record CustomerResponse(Guid Id, string Name);

public record CreateTransactionRequest(decimal Amount);
public record TransactionResponse(Guid Id, Guid CustomerId, decimal Amount, DateTimeOffset CreatedAt);
