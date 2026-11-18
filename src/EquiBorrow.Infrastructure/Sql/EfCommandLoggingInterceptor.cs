using System;
using System.Data.Common;
using System.IO;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EquiBorrow.Infrastructure.Sql;

public class EfCommandLoggingInterceptor : DbCommandInterceptor
{
    private readonly string _logPath;

    public EfCommandLoggingInterceptor()
    {
        // Resolve the repository logs directory from the application's base directory.
        var baseDir = AppContext.BaseDirectory ?? string.Empty;
        var repoRoot = FindRepositoryRoot(baseDir) ?? Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));

        var repoLogsDir = Path.Combine(repoRoot, "logs");
        _logPath = Path.Combine(repoLogsDir, "ef-commands.log");

        try
        {
            Directory.CreateDirectory(repoLogsDir);
        }
        catch
        {
            // ignore directory creation failures; writing will fail later if needed
        }
    }

    private static string? FindRepositoryRoot(string startDirectory)
    {
        try
        {
            var dir = new DirectoryInfo(startDirectory);
            for (int i = 0; i < 20 && dir != null; i++)
            {
                // check for solution files or git folder or README.md as repo indicators
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                    return dir.FullName;

                var slnFiles = Directory.GetFiles(dir.FullName, "*.sln");
                if (slnFiles.Length > 0)
                    return dir.FullName;

                var slnxFiles = Directory.GetFiles(dir.FullName, "*.slnx");
                if (slnxFiles.Length > 0)
                    return dir.FullName;

                var readme = Path.Combine(dir.FullName, "README.md");
                if (File.Exists(readme))
                    return dir.FullName;

                dir = dir.Parent;
            }
        }
        catch
        {
            // ignore and fallback
        }

        return null;
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
