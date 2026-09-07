using EcoPonto.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoPonto.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<PontoColeta> Pontos { get; set; } = null!;
        public DbSet<CategoriaResiduo> Categorias { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PontoColeta>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<CategoriaResiduo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            });

            // Many-to-many
            modelBuilder.Entity<PontoColeta>()
                .HasMany(p => p.Categorias)
                .WithMany(c => c.Pontos)
                .UsingEntity<Dictionary<string, object>>("PontoCategoria",
                    j => j.HasOne<CategoriaResiduo>().WithMany().HasForeignKey("CategoriaResiduoId").HasConstraintName("FK_PontoCategoria_Categoria"),
                    j => j.HasOne<PontoColeta>().WithMany().HasForeignKey("PontoColetaId").HasConstraintName("FK_PontoCategoria_Ponto"));
        }

        public static void SeedData(AppDbContext context)
        {
            // Ensure DB created
            context.Database.EnsureCreated();

            if (context.Categorias.Any()) return; // already seeded

            var oleo = new CategoriaResiduo { Nome = "Óleo de Cozinha", Icone = "oil", OrientacaoDescarte = "Armazenar em garrafa PET e levar ao ponto." };
            var pilhas = new CategoriaResiduo { Nome = "Pilhas/Baterias", Icone = "battery", OrientacaoDescarte = "Guardar em saco/plástico e entregar em local apropriado." };
            var eletronicos = new CategoriaResiduo { Nome = "Eletrônicos", Icone = "devices", OrientacaoDescarte = "Levar para pontos de coleta eletrônicos ou recicladores autorizados." };
            var remedios = new CategoriaResiduo { Nome = "Medicamentos", Icone = "pills", OrientacaoDescarte = "Devolver à farmácia participante ou à unidade de saúde." };

            context.Categorias.AddRange(oleo, pilhas, eletronicos, remedios);

            var ponto1 = new PontoColeta
            {
                Nome = "CEAM - Ponto de Coleta Comunitário",
                Logradouro = "Rua das Flores, 123",
                Bairro = "Centro",
                Cidade = "Cidade Exemplo",
                Telefone = "(11) 1111-1111",
                HorarioFuncionamento = "Seg-Sex 9h-17h",
                Latitude = -23.55052,
                Longitude = -46.633308,
            };

            var ponto2 = new PontoColeta
            {
                Nome = "Loja ReciclaJá",
                Logradouro = "Av. Principal, 500",
                Bairro = "Bela Vista",
                Cidade = "Cidade Exemplo",
                Telefone = "(11) 2222-2222",
                HorarioFuncionamento = "Seg-Dom 10h-20h",
                Latitude = -23.5510,
                Longitude = -46.6340,
            };

            var ponto3 = new PontoColeta
            {
                Nome = "Farmácia Saúde",
                Logradouro = "Praça da Saúde, 45",
                Bairro = "Jardim",
                Cidade = "Cidade Exemplo",
                Telefone = "(11) 3333-3333",
                HorarioFuncionamento = "24h",
                Latitude = -23.5520,
                Longitude = -46.6320,
            };

            var ponto4 = new PontoColeta
            {
                Nome = "Oficina Tech",
                Logradouro = "Rua do Porto, 88",
                Bairro = "Vila Nova",
                Cidade = "Cidade Exemplo",
                Telefone = "(11) 4444-4444",
                HorarioFuncionamento = "Seg-Sex 8h-18h",
                Latitude = -23.5530,
                Longitude = -46.6350,
            };

            // Associate categories
            ponto1.Categorias.Add(oleo);
            ponto1.Categorias.Add(eletronicos);

            ponto2.Categorias.Add(oleo);
            ponto2.Categorias.Add(pilhas);

            ponto3.Categorias.Add(remedios);

            ponto4.Categorias.Add(eletronicos);
            ponto4.Categorias.Add(pilhas);

            context.Pontos.AddRange(ponto1, ponto2, ponto3, ponto4);
            context.SaveChanges();
        }
    }
}
