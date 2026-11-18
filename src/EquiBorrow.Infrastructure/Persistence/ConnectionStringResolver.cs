using System.IO;
using Microsoft.Extensions.Configuration;

namespace EquiBorrow.Infrastructure.Persistence;

public static class ConnectionStringResolver
{
    public const string Name = "EquiBorrow";

    public static string Resolve(string? basePath = null)
    {
        IConfigurationRoot config;
        try
        {
            config = new ConfigurationBuilder()
                .SetBasePath(basePath ?? AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Local.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidOperationException(
                "Invalid JSON in appsettings.json or appsettings.Local.json. " +
                "Fix the configuration file, or set the ConnectionStrings__EquiBorrow environment variable.",
                ex);
        }

        var cs = config.GetConnectionString(Name);
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "No PostgreSQL connection string configured. Set the environment variable " +
                "ConnectionStrings__EquiBorrow or create appsettings.Local.json (see README).");
        return cs;
    }
}