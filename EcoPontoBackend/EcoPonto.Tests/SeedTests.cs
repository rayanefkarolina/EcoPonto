using EcoPonto.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EcoPonto.Tests
{
    public class SeedTests
    {
        [Fact]
        public void SeedData_Inserts_Categories_And_Pontos()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            using (var context = new AppDbContext(options))
            {
                // create schema and seed
                AppDbContext.SeedData(context);

                Assert.True(context.Categorias.Any());
                Assert.True(context.Pontos.Any());
            }

            connection.Close();
        }
    }
}
