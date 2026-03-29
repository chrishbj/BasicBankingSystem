using System.Data.Common;
using System.Reflection;
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

        if (!await HasMissingTablesAsync(connection, tables, cancellationToken))
        {
            return;
        }

        var createScript = dbContext.Database.GenerateCreateScript();
        try
        {
            foreach (var statement in SplitSqlStatements(createScript))
            {
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(statement, cancellationToken);
                }
                catch (DbException exception) when (IsAlreadyExistsException(exception))
                {
                    // Shared local databases may already contain some objects from prior runs
                    // or another service instance may have created them first.
                    continue;
                }
            }
        }
        catch (DbException)
        {
            if (!await HasMissingTablesAsync(connection, tables, cancellationToken))
            {
                // Another instance may have created the objects after our pre-check.
                // If every required table now exists, startup can continue safely.
                return;
            }

            throw;
        }
    }

    private static async Task<bool> HasMissingTablesAsync(
        DbConnection connection,
        IReadOnlyCollection<TableIdentifier> tables,
        CancellationToken cancellationToken)
    {
        foreach (var table in tables)
        {
            if (!await TableExistsAsync(connection, table, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyCollection<string> SplitSqlStatements(string script)
    {
        return script
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(statement => !string.IsNullOrWhiteSpace(statement))
            .Select(statement => $"{statement};")
            .ToArray();
    }

    private static bool IsAlreadyExistsException(DbException exception)
    {
        var sqlState = exception.GetType()
            .GetProperty("SqlState", BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(exception)?
            .ToString();

        return string.Equals(sqlState, "42P07", StringComparison.Ordinal) ||
               string.Equals(sqlState, "42710", StringComparison.Ordinal) ||
               exception.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
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
