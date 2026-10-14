using Carter;
using CrdbLab.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CrdbLab.Api.Endpoints;

public class InfrastructureModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/db/node", async (BankDbContext db) =>
        {
            var nodeId = await db.Database
                .SqlQuery<long>($"SELECT crdb_internal.node_id() AS \"Value\"")
                .SingleAsync();

            return Results.Ok(new { gatewayNodeId = nodeId });
        })
        .WithTags("Infrastructure");
    }
}
