using EcoPonto.Infrastructure;
using EcoPonto.API.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient("OpenStreetMap", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("EcoPontoLocal/1.0 (contacto@ecoponto.local)");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Configure at least one origin in Cors:AllowedOrigins.");
}

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// Configure DbContext: read connection string or fallback to sqlite
var configuration = builder.Configuration;
var conn = configuration.GetConnectionString("DefaultConnection") ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (!string.IsNullOrWhiteSpace(conn) && DatabaseConnectionString.IsPostgres(conn))
{
    var postgresConnectionString = DatabaseConnectionString.NormalizePostgres(conn);
    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(postgresConnectionString));
}
else
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("Configure ConnectionStrings__DefaultConnection with the Render PostgreSQL Internal Database URL.");
    }

    // fallback to sqlite local file
    var sqliteConn = conn ?? "Data Source=ecoponto.db";
    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(sqliteConn));
}

var app = builder.Build();

// Ensure DB and seed
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    AppDbContext.SeedData(ctx);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
