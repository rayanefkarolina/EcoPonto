using EcoPonto.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
            context.Database.EnsureCreated();

            CategoriaResiduo ObterOuCriarCategoria(string nome, string icone, string orientacaoDescarte)
            {
                var categoriaExistente = context.Categorias.SingleOrDefault(categoria => categoria.Nome == nome);
                if (categoriaExistente != null) return categoriaExistente;

                var categoria = new CategoriaResiduo { Nome = nome, Icone = icone, OrientacaoDescarte = orientacaoDescarte };
                context.Categorias.Add(categoria);
                return categoria;
            }

            var oleo = ObterOuCriarCategoria("Óleo de Cozinha", "oil", "Armazenar em garrafa PET e levar ao ponto.");
            var pilhas = ObterOuCriarCategoria("Pilhas/Baterias", "battery", "Guardar em saco/plástico e entregar em local apropriado.");
            var eletronicos = ObterOuCriarCategoria("Eletrônicos", "devices", "Levar para pontos de coleta eletrônicos ou recicladores autorizados.");
            var remedios = ObterOuCriarCategoria("Medicamentos", "pills", "Devolver à farmácia participante ou à unidade de saúde.");
            var centrosReciclagem = ObterOuCriarCategoria("Centro de Reciclagem", "recycling", "Confirme com o SAEMAS os materiais aceitos.");

            if (!context.Pontos.Any())
            {
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

                ponto1.Categorias.Add(oleo);
                ponto1.Categorias.Add(eletronicos);

                ponto2.Categorias.Add(oleo);
                ponto2.Categorias.Add(pilhas);

                ponto3.Categorias.Add(remedios);

                ponto4.Categorias.Add(eletronicos);
                ponto4.Categorias.Add(pilhas);

                context.Pontos.AddRange(ponto1, ponto2, ponto3, ponto4);
            }

            context.SaveChanges();

            var pontosSertaozinho = new[]
            {
                new PontoColeta
                {
                    Nome = "ECOPONTO 11°",
                    Logradouro = "R. Francisco Rodrigues, 1901 (posição aproximada)",
                    Bairro = "Jardim Paraíso",
                    Telefone = "(16) 3946-4646",
                    HorarioFuncionamento = "Aberto 24 horas",
                    Latitude = -21.1284992,
                    Longitude = -47.9749476
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 09°",
                    Logradouro = "São João (posição aproximada)",
                    Bairro = "São João",
                    Telefone = "(16) 3946-4646",
                    HorarioFuncionamento = "Aberto 24 horas",
                    Latitude = -21.1423330,
                    Longitude = -48.0047562
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 10°",
                    Logradouro = "Jardim São José (posição aproximada)",
                    Bairro = "Jardim São José",
                    Telefone = "(16) 3946-4646",
                    HorarioFuncionamento = "Aberto 24 horas",
                    Latitude = -21.1450112,
                    Longitude = -47.9905150
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 06°",
                    Logradouro = "Conj. Hab. Lúcia Fabro Sverzut (posição aproximada)",
                    Bairro = "Conj. Hab. Lúcia Fabro Sverzut",
                    Telefone = "(16) 3946-4646",
                    HorarioFuncionamento = "Aberto 24 horas",
                    Latitude = -21.1132471,
                    Longitude = -47.9944403
                },
                new PontoColeta
                {
                    Nome = "15° ECOPONTO",
                    Logradouro = "R. Antônio Sanches, 482 (posição aproximada)",
                    Bairro = "Jardim Tropical",
                    Telefone = "(16) 3946-4646",
                    HorarioFuncionamento = "Aberto 24 horas",
                    Latitude = -21.1486627,
                    Longitude = -47.9681409
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 12°",
                    Logradouro = "Jardim Veneto I (posição aproximada)",
                    Bairro = "Jardim Veneto I",
                    Telefone = "(16) 3946-4646",
                    HorarioFuncionamento = "Consulte o SAEMAS",
                    Latitude = -21.1366846,
                    Longitude = -47.9659459
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 07°",
                    Logradouro = "Jardim Itapuã (posição aproximada)",
                    Bairro = "Jardim Itapuã",
                    Telefone = "(16) 3946-4646",
                    HorarioFuncionamento = "Consulte o SAEMAS",
                    Latitude = -21.1272174,
                    Longitude = -47.9990086
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO",
                    Logradouro = "R. Antônio Soares, 1758 (posição aproximada)",
                    Bairro = "Alvorada",
                    Latitude = -21.1216895,
                    Longitude = -47.9837303
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 02°",
                    Logradouro = "Cruz das Posses (posição aproximada)",
                    Bairro = "Cruz das Posses",
                    Telefone = "(16) 3946-4646",
                    HorarioFuncionamento = "Aberto 24 horas",
                    Latitude = -21.0518542,
                    Longitude = -47.9281623
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 04°",
                    Logradouro = "Av. César Mingossi, 262 (posição aproximada)",
                    Bairro = "Jardim das Palmeiras",
                    HorarioFuncionamento = "Consulte o SAEMAS",
                    Latitude = -21.1315054,
                    Longitude = -48.0188265
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 05°",
                    Logradouro = "Cruz das Posses (posição aproximada)",
                    Bairro = "Cruz das Posses",
                    Telefone = "(16) 3946-4646",
                    HorarioFuncionamento = "Consulte o SAEMAS",
                    Latitude = -21.0518542,
                    Longitude = -47.9281623
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 03°",
                    Logradouro = "Jardim Nova Europa (posição aproximada)",
                    Bairro = "Jardim Nova Europa",
                    HorarioFuncionamento = "Consulte o SAEMAS",
                    Latitude = -21.1255567,
                    Longitude = -48.0254450
                }
            };

            foreach (var cadastro in pontosSertaozinho)
            {
                var ponto = context.Pontos
                    .Include(item => item.Categorias)
                    .SingleOrDefault(item => item.Nome == cadastro.Nome && item.Cidade == "Sertãozinho");

                if (ponto == null)
                {
                    ponto = cadastro;
                    context.Pontos.Add(ponto);
                }
                else
                {
                    ponto.Logradouro = cadastro.Logradouro;
                    ponto.Bairro = cadastro.Bairro;
                    ponto.Telefone = cadastro.Telefone;
                    ponto.HorarioFuncionamento = cadastro.HorarioFuncionamento;
                    ponto.Latitude = cadastro.Latitude;
                    ponto.Longitude = cadastro.Longitude;
                }

                ponto.Cidade = "Sertãozinho";
                if (!ponto.Categorias.Any(categoria => categoria.Id == centrosReciclagem.Id))
                {
                    ponto.Categorias.Add(centrosReciclagem);
                }
            }

            var pontosRibeiraoPreto = new[]
            {
                new PontoColeta
                {
                    Nome = "ECOPONTO 1 - ALEXANDRE BALBO",
                    Logradouro = "Avenida Ettore e Aurora Corauci, 1375 (coordenadas aproximadas)",
                    Bairro = "Alexandre Balbo",
                    Cidade = "Ribeirão Preto",
                    Latitude = -21.1306236,
                    Longitude = -47.8388186
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 2 - JARDIM CENTENÁRIO",
                    Logradouro = "Rua Arthur Ramos, 1185",
                    Bairro = "Jardim Centenário",
                    Cidade = "Ribeirão Preto",
                    Latitude = -21.1912153,
                    Longitude = -47.8368990
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 3 - JARDIM SANTOS DUMONT",
                    Logradouro = "Rua Guará, 2225 (coordenadas aproximadas)",
                    Bairro = "Jardim Santos Dumont",
                    Cidade = "Ribeirão Preto",
                    Latitude = -21.1328358,
                    Longitude = -47.7791142
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 4 - JARDIM DAS PALMEIRAS II",
                    Logradouro = "Rua Poeta Fernando Pessoa, 15 (coordenadas aproximadas)",
                    Bairro = "Jardim das Palmeiras II",
                    Cidade = "Ribeirão Preto",
                    Latitude = -21.1885479,
                    Longitude = -47.7409262
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 5 - JARDIM PAIVA",
                    Logradouro = "Av. Sen. Teotônio Vilella, 1285 (coordenadas aproximadas)",
                    Bairro = "Jardim Paiva",
                    Cidade = "Ribeirão Preto",
                    Latitude = -21.1456650,
                    Longitude = -47.8332810
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 6 - JARDIM SÃO FERNANDO",
                    Logradouro = "Avenida Miguel Padulla, 400 (coordenadas aproximadas)",
                    Bairro = "Jardim São Fernando",
                    Cidade = "Ribeirão Preto",
                    Latitude = -21.2773986,
                    Longitude = -47.8050548
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 7 - ADELINO SIMIONI",
                    Logradouro = "Av. Gal. Euclides Figueiredo, 120 (coordenadas aproximadas)",
                    Bairro = "Adelino Simioni",
                    Cidade = "Ribeirão Preto",
                    Latitude = -21.1117866,
                    Longitude = -47.7910864
                },
                new PontoColeta
                {
                    Nome = "ECOPONTO 8 - JARDIM DIVA TARLÁ DE CARVALHO",
                    Logradouro = "Av. Dr. Fernando Mendes Garcia, 3480 (coordenadas aproximadas)",
                    Bairro = "Jardim Diva Tarlá de Carvalho",
                    Cidade = "Ribeirão Preto",
                    Latitude = -21.1348239,
                    Longitude = -47.7412475
                }
            };

            foreach (var cadastro in pontosRibeiraoPreto)
            {
                var ponto = context.Pontos
                    .Include(item => item.Categorias)
                    .SingleOrDefault(item => item.Nome == cadastro.Nome && item.Cidade == "Ribeirão Preto");

                if (ponto == null)
                {
                    ponto = cadastro;
                    context.Pontos.Add(ponto);
                }
                else
                {
                    ponto.Logradouro = cadastro.Logradouro;
                    ponto.Bairro = cadastro.Bairro;
                    ponto.Latitude = cadastro.Latitude;
                    ponto.Longitude = cadastro.Longitude;
                }

                ponto.Cidade = "Ribeirão Preto";
                if (!ponto.Categorias.Any(categoria => categoria.Id == centrosReciclagem.Id))
                {
                    ponto.Categorias.Add(centrosReciclagem);
                }
            }

            const string resourceName = "EcoPonto.Infrastructure.Data.SaoPauloEcopontos.json";
            using var stream = typeof(AppDbContext).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded Ecopontos dataset '{resourceName}' was not found.");
            var catalogoSaoPaulo = JsonSerializer.Deserialize<CatalogoEcopontos>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("The São Paulo Ecopontos dataset is invalid.");

            foreach (var cadastro in catalogoSaoPaulo.Locations)
            {
                var ponto = context.Pontos
                    .Include(item => item.Categorias)
                    .SingleOrDefault(item => item.Nome == cadastro.Name && item.Cidade == "São Paulo");

                if (ponto == null)
                {
                    ponto = new PontoColeta
                    {
                        Nome = cadastro.Name,
                        Cidade = "São Paulo"
                    };
                    context.Pontos.Add(ponto);
                }

                ponto.Logradouro = string.Join(" - ", new[] { cadastro.Address, cadastro.Observation }
                    .Where(parte => !string.IsNullOrWhiteSpace(parte)));
                ponto.Latitude = cadastro.Latitude;
                ponto.Longitude = cadastro.Longitude;
                if (!ponto.Categorias.Any(categoria => categoria.Id == centrosReciclagem.Id))
                {
                    ponto.Categorias.Add(centrosReciclagem);
                }
            }

            context.SaveChanges();
        }

        private sealed class CatalogoEcopontos
        {
            public List<EcopontoSaoPaulo> Locations { get; set; } = new();
        }

        private sealed class EcopontoSaoPaulo
        {
            public string Name { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Observation { get; set; } = string.Empty;
            public string Subprefecture { get; set; } = string.Empty;
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

    }
}
