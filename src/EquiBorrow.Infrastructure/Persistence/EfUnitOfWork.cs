using System.IO;
using System.Net.Sockets;
using EquiBorrow.Application;
using EquiBorrow.Application.Interfaces;
using EquiBorrow.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace EquiBorrow.Infrastructure.Persistence;

public sealed class EfUnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDbContextFactory<EquipmentBorrowingDbContext> _contextFactory;

    public EfUnitOfWorkFactory(IDbContextFactory<EquipmentBorrowingDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public IUnitOfWork Create()
    {
        var db = _contextFactory.CreateDbContext();
        return new EfUnitOfWork(db);
    }
}

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly EquipmentBorrowingDbContext _db;
    private bool _committed;

    public EfUnitOfWork(EquipmentBorrowingDbContext db)
    {
        _db = db;
        Students = new EfStudentRepository(db);
        Equipment = new EfEquipmentRepository(db);
        Borrowings = new EfBorrowingRepository(db);
    }

    public IStudentRepository Students { get; }
    public IEquipmentRepository Equipment { get; }
    public IBorrowingRepository Borrowings { get; }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_committed)
            throw new InvalidOperationException("This unit of work has already been committed.");

        IDbContextTransaction transaction;
        try
        {
            transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw PersistenceExceptionTranslator.ToKnownFailure(ex);
        }

        await using (transaction)
        {
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (Exception rollbackException)
                {
                    ex.Data["RollbackFailure"] = rollbackException;
                }

                throw PersistenceExceptionTranslator.ToKnownFailure(ex);
            }

            try
            {
                await transaction.CommitAsync(cancellationToken);
                _committed = true;
            }
            catch (Exception ex) when (PersistenceExceptionTranslator.IsUncertainCommit(ex))
            {
                throw new CommitOutcomeUnknownException(
                    "The database connection was lost while confirming the transaction commit.", ex);
            }
            catch (Exception ex)
            {
                throw PersistenceExceptionTranslator.ToKnownFailure(ex);
            }
        }
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}

internal static class PersistenceExceptionTranslator
{
    public static Exception ToKnownFailure(Exception exception)
    {
        if (exception is PersistenceConflictException or PersistenceFailureException)
            return exception;

        if (exception is DbUpdateConcurrencyException)
            return new PersistenceConflictException(PersistenceConflictKind.Concurrency, innerException: exception);

        var postgres = FindPostgresException(exception);
        if (postgres?.SqlState == PostgresErrorCodes.UniqueViolation)
            return new PersistenceConflictException(
                PersistenceConflictKind.Duplicate,
                postgres.ConstraintName,
                "A record with the same unique value already exists.",
                exception);

        return new PersistenceFailureException("The database operation failed without committing changes.", exception);
    }

    public static bool IsUncertainCommit(Exception exception)
    {
        if (exception is TimeoutException or IOException or SocketException)
            return true;

        if (exception is NpgsqlException npgsqlException && npgsqlException.IsTransient)
            return true;

        return exception.InnerException is not null && IsUncertainCommit(exception.InnerException);
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
                return postgres;
        }

        return null;
    }
}
