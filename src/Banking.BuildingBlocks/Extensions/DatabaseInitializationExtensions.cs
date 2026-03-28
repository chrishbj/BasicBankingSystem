using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Banking.BuildingBlocks.Extensions;

public static class DatabaseInitializationExtensions
{
    public static async Task EnsureContextObjectsCreatedAsync<TContext>(this IServiceProvider services, CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var providerName = dbContext.Database.ProviderName ?? string.Empty;

        if (!dbContext.Database.IsRelational() || providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        var tables = dbContext.Model
            .GetEntityTypes()
            .Where(entityType => !entityType.IsOwned())
            .Select(entityType => new TableIdentifier(
                entityType.GetSchema() ?? "public",
                entityType.GetTableName()))
            .Where(table => !string.IsNullOrWhiteSpace(table.TableName))
            .Distinct()
            .ToArray();

        if (tables.Length == 0)
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        await using var connection = dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var missingTableExists = false;
        foreach (var table in tables)
        {
            if (!await TableExistsAsync(connection, table, cancellationToken))
            {
                missingTableExists = true;
                break;
            }
        }

        if (!missingTableExists)
        {
            return;
        }

        var createScript = dbContext.Database.GenerateCreateScript();
        await dbContext.Database.ExecuteSqlRawAsync(createScript, cancellationToken);
    }

    private static async Task<bool> TableExistsAsync(DbConnection connection, TableIdentifier table, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = @schema
                  AND table_name = @table
            );
            """;

        var schemaParameter = command.CreateParameter();
        schemaParameter.ParameterName = "@schema";
        schemaParameter.Value = table.Schema;
        command.Parameters.Add(schemaParameter);

        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@table";
        tableParameter.Value = table.TableName;
        command.Parameters.Add(tableParameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true || (result is bool boolResult && boolResult);
    }

    private sealed record TableIdentifier(string Schema, string? TableName);
}
