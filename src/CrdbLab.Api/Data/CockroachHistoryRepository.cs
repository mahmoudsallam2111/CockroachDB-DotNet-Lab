#pragma warning disable EF1001

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Internal;

namespace CrdbLab.Api.Data;

public class CockroachHistoryRepository(HistoryRepositoryDependencies dependencies)
    : NpgsqlHistoryRepository(dependencies)
{
    public override IMigrationsDatabaseLock AcquireDatabaseLock()
        => new NoOpMigrationsDatabaseLock(this);

    public override Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IMigrationsDatabaseLock>(new NoOpMigrationsDatabaseLock(this));

    private sealed class NoOpMigrationsDatabaseLock(IHistoryRepository historyRepository) : IMigrationsDatabaseLock
    {
        IHistoryRepository IMigrationsDatabaseLock.HistoryRepository => historyRepository;

        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
