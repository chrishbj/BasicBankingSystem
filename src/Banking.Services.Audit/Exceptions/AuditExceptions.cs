namespace Banking.Services.Audit.Exceptions;

public sealed class InvalidAuditLogException(string message) : Exception(message);

public sealed class AuditLogNotFoundException(string auditId)
    : Exception($"Audit log '{auditId}' was not found.");
