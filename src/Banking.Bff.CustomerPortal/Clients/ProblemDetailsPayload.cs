namespace Banking.Bff.CustomerPortal.Clients;

internal sealed record ProblemDetailsPayload(string? Title, string? Detail, int? Status);
