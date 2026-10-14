using Carter;
using CrdbLab.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBankDatabase(builder.Configuration); // EF Core + CockroachDB (Data/DatabaseSetup.cs)
builder.Services.AddCarter();                            
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.ApplyMigrations();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapCarter();

app.Run();
