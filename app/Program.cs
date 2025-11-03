using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5292")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Connection strings
var olapConnStr = builder.Configuration.GetConnectionString("OLAPConnection");
var oltpConnStr = builder.Configuration.GetConnectionString("OLTPConnection");
var masterConnStr = "Server=.;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

// Ensure databases exist and run creation scripts
EnsureDatabaseExists("CareServicesOLTP", "../CareServicesOLTPCreation.sql", masterConnStr);
EnsureDatabaseExists("CareServicesOLAP", "../CareServicesOLAPCreation.sql", masterConnStr);

void EnsureDatabaseExists(string dbName, string scriptPath, string masterConn)
{
    using var connection = new SqlConnection(masterConn);
    connection.Open();

    // Check if database exists
    var checkCmd = new SqlCommand($"IF DB_ID('{dbName}') IS NULL SELECT 0 ELSE SELECT 1", connection);
    var exists = (int)checkCmd.ExecuteScalar() == 1;
    if (exists)
    {
        Console.WriteLine($"{dbName} already exists.");
        return;
    }

    Console.WriteLine($"{dbName} does not exist. Creating...");

    // Read script
    var script = File.ReadAllText(scriptPath);

    // Split by GO statements
    var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    foreach (var batch in batches)
    {
        if (string.IsNullOrWhiteSpace(batch)) continue;

        try
        {
            using var cmd = new SqlCommand(batch, connection);
            cmd.ExecuteNonQuery();  // catch errors if a batch fails
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing batch in {dbName}: {ex.Message}");
            throw; // rethrow if you want app to stop
        }
    }

    Console.WriteLine($"{dbName} created successfully.");
}

// Register OLAP connection for DI
builder.Services.AddScoped<SqlConnection>(_ => new SqlConnection(olapConnStr));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseStaticFiles();
app.MapControllers();

app.MapGet("/", context =>
{
    context.Response.Redirect("/chart.html");
    return Task.CompletedTask;
});

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
