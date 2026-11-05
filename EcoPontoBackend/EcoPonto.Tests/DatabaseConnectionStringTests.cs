using EcoPonto.API.Configuration;
using Npgsql;

namespace EcoPonto.Tests;

public class DatabaseConnectionStringTests
{
    [Fact]
    public void NormalizePostgres_ConvertsRenderUrlToNpgsqlConnectionString()
    {
        var connectionString = DatabaseConnectionString.NormalizePostgres(
            "postgresql://eco_user:p%40ss%3Aword@dpg-example-a.oregon-postgres.render.com/ecoponto");

        var parsed = new NpgsqlConnectionStringBuilder(connectionString);
        Assert.Equal("dpg-example-a.oregon-postgres.render.com", parsed.Host);
        Assert.Equal(5432, parsed.Port);
        Assert.Equal("ecoponto", parsed.Database);
        Assert.Equal("eco_user", parsed.Username);
        Assert.Equal("p@ss:word", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void IsPostgres_RecognizesRenderDatabaseUrl()
    {
        Assert.True(DatabaseConnectionString.IsPostgres("postgres://user:password@host/database"));
        Assert.True(DatabaseConnectionString.IsPostgres("Host=localhost;Database=ecoponto"));
        Assert.False(DatabaseConnectionString.IsPostgres("Data Source=ecoponto.db"));
    }
}
