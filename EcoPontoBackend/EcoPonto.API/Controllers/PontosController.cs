using EcoPonto.API.Dtos;
using EcoPonto.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EcoPonto.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PontosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PontosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("proximos")]
        public async Task<ActionResult<IEnumerable<PontoColetaResponseDto>>> GetNearby(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] int raioKm = 10,
            [FromServices] IHttpClientFactory httpClientFactory = null!)
        {
            if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
                return BadRequest("Coordenadas inválidas.");

            var raioMetros = Math.Clamp(raioKm, 1, 50) * 1000;
            var query = $"[out:json][timeout:12];(nwr[\"amenity\"=\"recycling\"](around:{raioMetros},{latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)});nwr[\"recycling_type\"=\"centre\"](around:{raioMetros},{latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}););out center tags;";

            try
            {
                var client = httpClientFactory.CreateClient("OpenStreetMap");
                using var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("data", query) });
                HttpResponseMessage? response = null;
                foreach (var endpoint in new[]
                {
                    "https://overpass-api.de/api/interpreter",
                    "https://overpass.kumi.systems/api/interpreter"
                })
                {
                    try
                    {
                        response = await client.PostAsync(endpoint, content);
                        if (response.IsSuccessStatusCode) break;
                        response.Dispose();
                        response = null;
                    }
                    catch (HttpRequestException)
                    {
                        response?.Dispose();
                        response = null;
                    }
                    catch (TaskCanceledException)
                    {
                        response?.Dispose();
                        response = null;
                    }
                }

                if (response == null) return Ok(Array.Empty<PontoColetaResponseDto>());
                using (response)
                {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                var pontos = document.RootElement.GetProperty("elements")
                    .EnumerateArray()
                    .Select((element, index) => ToOpenStreetMapPoint(element, index))
                    .Where(point => point != null)
                    .Select(point => point!)
                    .ToList();

                return Ok(pontos);
                }
            }
            catch (HttpRequestException)
            {
                return Ok(Array.Empty<PontoColetaResponseDto>());
            }
            catch (TaskCanceledException)
            {
                return Ok(Array.Empty<PontoColetaResponseDto>());
            }
            catch (JsonException)
            {
                return Ok(Array.Empty<PontoColetaResponseDto>());
            }
        }

        private static PontoColetaResponseDto? ToOpenStreetMapPoint(JsonElement element, int index)
        {
            var latitude = element.TryGetProperty("lat", out var lat)
                ? lat.GetDouble()
                : element.TryGetProperty("center", out var center) && center.TryGetProperty("lat", out var centerLat)
                    ? centerLat.GetDouble()
                    : 0;
            var longitude = element.TryGetProperty("lon", out var lon)
                ? lon.GetDouble()
                : element.TryGetProperty("center", out var centerLon) && centerLon.TryGetProperty("lon", out var centerLongitude)
                    ? centerLongitude.GetDouble()
                    : 0;

            if (latitude == 0 || longitude == 0) return null;
            var tags = element.TryGetProperty("tags", out var tagValues) ? tagValues : default;
            var name = GetTag(tags, "name") ?? "EcoPonto próximo";
            var address = GetTag(tags, "addr:street") ?? GetTag(tags, "description") ?? "Endereço disponível no mapa";
            var number = GetTag(tags, "addr:housenumber");

            return new PontoColetaResponseDto
            {
                Id = -Math.Abs(HashCode.Combine("osm", index, latitude, longitude)),
                Nome = name,
                Logradouro = number == null ? address : $"{address}, {number}",
                Bairro = GetTag(tags, "addr:suburb") ?? GetTag(tags, "addr:district") ?? string.Empty,
                Cidade = GetTag(tags, "addr:city") ?? string.Empty,
                Latitude = latitude,
                Longitude = longitude,
                Categorias = new List<CategoriaDto> { new() { Id = 0, Nome = "Recicláveis" } }
            };
        }

        private static string? GetTag(JsonElement tags, string name)
        {
            return tags.ValueKind == JsonValueKind.Object && tags.TryGetProperty(name, out var value)
                ? value.GetString()
                : null;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PontoColetaResponseDto>>> Get([FromQuery] int? categoriaId, [FromQuery] string? busca)
        {
            var query = _context.Pontos.Include(p => p.Categorias).AsQueryable();

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.Categorias.Any(c => c.Id == categoriaId.Value));
            }

            if (!string.IsNullOrWhiteSpace(busca))
            {
                var termo = busca.Trim().ToLower();
                query = query.Where(p => p.Nome.ToLower().Contains(termo)
                    || p.Logradouro.ToLower().Contains(termo)
                    || p.Bairro.ToLower().Contains(termo)
                    || p.Cidade.ToLower().Contains(termo));
            }

            var result = await query.AsNoTracking().Select(p => new PontoColetaResponseDto
            {
                Id = p.Id,
                Nome = p.Nome,
                Logradouro = p.Logradouro,
                Bairro = p.Bairro,
                Cidade = p.Cidade,
                Telefone = p.Telefone,
                HorarioFuncionamento = p.HorarioFuncionamento,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Categorias = p.Categorias.Select(c => new CategoriaDto { Id = c.Id, Nome = c.Nome, Icone = c.Icone, OrientacaoDescarte = c.OrientacaoDescarte }).ToList()
            }).ToListAsync();

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PontoColetaResponseDto>> GetById(int id)
        {
            var p = await _context.Pontos.Include(x => x.Categorias).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (p == null) return NotFound();

            var dto = new PontoColetaResponseDto
            {
                Id = p.Id,
                Nome = p.Nome,
                Logradouro = p.Logradouro,
                Bairro = p.Bairro,
                Cidade = p.Cidade,
                Telefone = p.Telefone,
                HorarioFuncionamento = p.HorarioFuncionamento,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Categorias = p.Categorias.Select(c => new CategoriaDto { Id = c.Id, Nome = c.Nome, Icone = c.Icone, OrientacaoDescarte = c.OrientacaoDescarte }).ToList()
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<ActionResult<PontoColetaResponseDto>> Create(PontoColetaCreateDto create)
        {
            var ponto = new EcoPonto.Domain.PontoColeta
            {
                Nome = create.Nome,
                Logradouro = create.Logradouro,
                Bairro = create.Bairro,
                Cidade = create.Cidade,
                Telefone = create.Telefone,
                HorarioFuncionamento = create.HorarioFuncionamento,
                Latitude = create.Latitude,
                Longitude = create.Longitude
            };

            if (create.CategoriaIds?.Any() == true)
            {
                var categorias = await _context.Categorias.Where(c => create.CategoriaIds.Contains(c.Id)).ToListAsync();
                foreach (var c in categorias) ponto.Categorias.Add(c);
            }

            _context.Pontos.Add(ponto);
            await _context.SaveChangesAsync();

            var dto = new PontoColetaResponseDto
            {
                Id = ponto.Id,
                Nome = ponto.Nome,
                Logradouro = ponto.Logradouro,
                Bairro = ponto.Bairro,
                Cidade = ponto.Cidade,
                Telefone = ponto.Telefone,
                HorarioFuncionamento = ponto.HorarioFuncionamento,
                Latitude = ponto.Latitude,
                Longitude = ponto.Longitude,
                Categorias = ponto.Categorias.Select(c => new CategoriaDto { Id = c.Id, Nome = c.Nome, Icone = c.Icone, OrientacaoDescarte = c.OrientacaoDescarte }).ToList()
            };

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
    }
}
