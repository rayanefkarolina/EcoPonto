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

        [Fact]
        public void SeedData_AddsSertaozinhoEcopontosOnlyOnce()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            using (var context = new AppDbContext(options))
            {
                AppDbContext.SeedData(context);
                AppDbContext.SeedData(context);

                var pontosSertaozinho = context.Pontos.Where(ponto => ponto.Cidade == "Sertãozinho").ToList();
                Assert.Equal(12, pontosSertaozinho.Count);
                Assert.All(pontosSertaozinho, ponto => Assert.Contains(
                    ponto.Categorias,
                    categoria => categoria.Nome == "Centro de Reciclagem"));
            }

            connection.Close();
        }

        [Fact]
        public void SeedData_AddsRibeiraoPretoEcopontosOnlyOnce()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            using (var context = new AppDbContext(options))
            {
                AppDbContext.SeedData(context);
                AppDbContext.SeedData(context);

                var pontosRibeiraoPreto = context.Pontos
                    .Where(ponto => ponto.Cidade == "Ribeirão Preto")
                    .ToList();
                Assert.Equal(8, pontosRibeiraoPreto.Count);
                Assert.All(pontosRibeiraoPreto, ponto => Assert.Contains(
                    ponto.Categorias,
                    categoria => categoria.Nome == "Centro de Reciclagem"));
            }

            connection.Close();
        }

        [Fact]
        public void SeedData_AddsOfficialSaoPauloEcopontosOnlyOnce()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;

            using (var context = new AppDbContext(options))
            {
                AppDbContext.SeedData(context);
                AppDbContext.SeedData(context);

                var pontosSaoPaulo = context.Pontos
                    .Where(ponto => ponto.Cidade == "São Paulo")
                    .ToList();
                Assert.Equal(80, pontosSaoPaulo.Count);
                Assert.All(pontosSaoPaulo, ponto =>
                {
                    Assert.InRange(ponto.Latitude, -24, -23);
                    Assert.InRange(ponto.Longitude, -47, -46);
                    Assert.Contains(ponto.Categorias, categoria => categoria.Nome == "Centro de Reciclagem");
                });
            }

            connection.Close();
        }
    }
}
