using System;
using System.Data.Common;
using System.IO;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EquiBorrow.Infrastructure.Sql;

public class EfCommandLoggingInterceptor : DbCommandInterceptor
{
    private readonly string _logPath = Path.Combine("logs", "ef-commands.log");
    private readonly string _docsPath = Path.Combine("docs", "database-queries.sql");

    public EfCommandLoggingInterceptor()
    {
        try
        {
            Directory.CreateDirectory("logs");
            Directory.CreateDirectory("docs");
        }
        catch
        {
            // ignore directory creation failures; writing will fail later if needed
        }
    }

    private void AppendText(string text)
    {
        try
        {
            File.AppendAllText(_logPath, text + Environment.NewLine + "--" + Environment.NewLine);
        }
        catch
        {
            // swallow
        }
        try
        {
            File.AppendAllText(_docsPath, text + Environment.NewLine + Environment.NewLine);
        }
        catch
        {
            // swallow
        }
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        AppendText(command.CommandText);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AppendText(command.CommandText);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        AppendText(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        AppendText(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object?> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object?> result)
    {
        AppendText(command.CommandText);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object?>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object?> result, CancellationToken cancellationToken = default)
    {
        AppendText(command.CommandText);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}
