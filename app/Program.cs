using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Data.Odbc;
using System.Runtime.InteropServices;

string GetLatestSqlServerOdbcDriver()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\ODBC\ODBCINST.INI\ODBC Drivers", false);
        if (key == null) throw new Exception("No ODBC drivers registry key found.");

        var driverNames = key.GetValueNames()
            .Where(d => d.StartsWith("ODBC Driver") && d.Contains("SQL Server"))
            .Select(d => new
            {
                Name = d,
                Version = Version.TryParse(
                    Regex.Match(d, @"\d+").Value, out var v) ? v : new Version(0,0)
            })
            .OrderByDescending(d => d.Version)
            .ToList();

        if (!driverNames.Any()) throw new Exception("No SQL Server ODBC driver found.");

        return driverNames.First().Name;
    }
    else
    {
        throw new NotSupportedException("This application requires a Windows environment for ODBC driver detection.");
    }
}

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
var masterConnStr = "Server=localhost,1433;Database=master;UID=sa;PWD=Fateh!123;TrustServerCertificate=True;";

string driver;
try
{
    driver = "ODBC Driver 18 for SQL Server"; // change this your ODBC driver version here if needed
    Console.WriteLine($"Using ODBC Driver: {driver}");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to detect ODBC driver: {ex.Message}");
    throw;
}

var oltpOdbcConnectionString = $"Driver={{{driver}}};Server=localhost,1433;Database=CareServicesOLTP;Uid=sa;Pwd=Fateh!123;TrustServerCertificate=Yes;";
var olapOdbcConnectionString = $"Driver={{{driver}}};Server=localhost,1433;Database=CareServicesOLAP;Uid=sa;Pwd=Fateh!123;TrustServerCertificate=Yes;";
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

    // Run creation script
    ExecuteSqlFile(scriptPath, connection);
    Console.WriteLine($"{dbName} created successfully.");

    // Only seed OLTP database
    if (dbName.Equals("CareServicesOLTP", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Seeding OLTP database with initial data...");
        ExecuteSqlFile("../CareServicesOLTPInsertion.sql", connection);
        Console.WriteLine("OLTP database seeded successfully.");
    }
}

// Helper function to run any SQL file
void ExecuteSqlFile(string path, SqlConnection connection)
{
    var script = File.ReadAllText(path);
    var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    foreach (var batch in batches)
    {
        if (string.IsNullOrWhiteSpace(batch)) continue;
        using var cmd = new SqlCommand(batch, connection);
        cmd.ExecuteNonQuery();
    }
}

Console.WriteLine("Visit http://localhost:5292/api/etl/run to run OLTP -> ETL -> OLAP.");

// Register OLAP connection for DI
builder.Services.AddScoped<SqlConnection>(_ => new SqlConnection(olapConnStr));
builder.Services.AddSingleton(new ETLService(oltpOdbcConnectionString, olapOdbcConnectionString));

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
