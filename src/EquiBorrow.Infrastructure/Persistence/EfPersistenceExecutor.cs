using System;
using System.Data.Common;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EquiBorrow.Infrastructure.Persistence;

internal static class EfPersistenceExecutor
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception) when (IsPersistenceException(exception))
        {
            throw PersistenceExceptionTranslator.ToKnownFailure(exception);
        }
    }

    private static bool IsPersistenceException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException or DbUpdateException or IOException or SocketException or TimeoutException)
                return true;
        }

        return false;
    }
}
