#pragma warning disable EF1001

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace CrdbLab.Api.Data;

public class CockroachMigrationsSqlGenerator(
    MigrationsSqlGeneratorDependencies dependencies,
    INpgsqlSingletonOptions npgsqlSingletonOptions)
    : NpgsqlMigrationsSqlGenerator(dependencies, npgsqlSingletonOptions)
{
    protected override void EndStatement(MigrationCommandListBuilder builder, bool suppressTransaction = false)
        => base.EndStatement(builder, suppressTransaction: true);
}
