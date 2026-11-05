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
        private readonly ILogger<PontosController> _logger;

        public PontosController(AppDbContext context, ILogger<PontosController> logger)
        {
            _context = context;
            _logger = logger;
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
            raioKm = raioMetros / 1000;
            var query = $"[out:json][timeout:12];(nwr[\"amenity\"=\"recycling\"](around:{raioMetros},{latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)});nwr[\"recycling_type\"=\"centre\"](around:{raioMetros},{latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}););out center tags;";

            var latitudeRange = raioKm / 110.574;
            var longitudeRange = Math.Min(180, raioKm / (111.320 * Math.Max(Math.Cos(latitude * Math.PI / 180), 0.01)));
            var pontosSalvos = await _context.Pontos
                .AsNoTracking()
                .Include(ponto => ponto.Categorias)
                .Where(ponto =>
                    ponto.Latitude >= Math.Max(-90, latitude - latitudeRange)
                    && ponto.Latitude <= Math.Min(90, latitude + latitudeRange)
                    && ponto.Longitude >= Math.Max(-180, longitude - longitudeRange)
                    && ponto.Longitude <= Math.Min(180, longitude + longitudeRange))
                .ToListAsync();
            var pontosLocais = pontosSalvos
                .Where(ponto => DistanciaEmKm(latitude, longitude, ponto.Latitude, ponto.Longitude) <= raioKm)
                .Select(ponto => new PontoColetaResponseDto
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
                    Categorias = ponto.Categorias.Select(categoria => new CategoriaDto
                    {
                        Id = categoria.Id,
                        Nome = categoria.Nome,
                        Icone = categoria.Icone,
                        OrientacaoDescarte = categoria.OrientacaoDescarte
                    }).ToList()
                })
                .ToList();

            var client = httpClientFactory.CreateClient("OpenStreetMap");
            HttpResponseMessage? response = null;
            var failures = new List<string>();
            foreach (var endpoint in new[]
            {
                "https://overpass.osm.ch/api/interpreter",
                "https://overpass-api.de/api/interpreter",
                "https://overpass.kumi.systems/api/interpreter"
            })
            {
                try
                {
                    using var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("data", query) });
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    response = await client.PostAsync(endpoint, content, timeout.Token);
                    if (response.IsSuccessStatusCode) break;

                    failures.Add($"{endpoint}: HTTP {(int)response.StatusCode}");
                    _logger.LogWarning("Overpass endpoint {Endpoint} returned HTTP {StatusCode}.", endpoint, (int)response.StatusCode);
                    response.Dispose();
                    response = null;
                }
                catch (HttpRequestException exception)
                {
                    failures.Add($"{endpoint}: {exception.Message}");
                    _logger.LogWarning(exception, "Could not reach Overpass endpoint {Endpoint}.", endpoint);
                    response?.Dispose();
                    response = null;
                }
                catch (TaskCanceledException exception)
                {
                    failures.Add($"{endpoint}: request timed out");
                    _logger.LogWarning(exception, "Overpass endpoint {Endpoint} timed out.", endpoint);
                    response?.Dispose();
                    response = null;
                }
            }

            if (response == null)
            {
                _logger.LogError("All Overpass endpoints failed: {Failures}", string.Join("; ", failures));
                if (pontosLocais.Count > 0) return Ok(pontosLocais);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "O serviço de busca de pontos está indisponível no momento.");
            }

            using (response)
            {
                try
                {
                    using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

                    var pontos = document.RootElement.GetProperty("elements")
                        .EnumerateArray()
                        .Select((element, index) => ToOpenStreetMapPoint(element, index))
                        .Where(point => point != null)
                        .Select(point => point!)
                        .ToList();

                    foreach (var pontoLocal in pontosLocais)
                    {
                        if (!pontos.Any(ponto => DistanciaEmKm(
                            pontoLocal.Latitude,
                            pontoLocal.Longitude,
                            ponto.Latitude,
                            ponto.Longitude) < 0.1))
                        {
                            pontos.Add(pontoLocal);
                        }
                    }

                    return Ok(pontos);
                }
                catch (JsonException exception)
                {
                    _logger.LogError(exception, "Overpass returned an invalid JSON response.");
                    if (pontosLocais.Count > 0) return Ok(pontosLocais);
                    return StatusCode(StatusCodes.Status502BadGateway, "O serviço de busca retornou uma resposta inválida.");
                }
                catch (InvalidOperationException exception)
                {
                    _logger.LogError(exception, "Overpass returned an unexpected response structure.");
                    if (pontosLocais.Count > 0) return Ok(pontosLocais);
                    return StatusCode(StatusCodes.Status502BadGateway, "O serviço de busca retornou uma resposta inválida.");
                }
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

        private static double DistanciaEmKm(double latitudeOrigem, double longitudeOrigem, double latitudeDestino, double longitudeDestino)
        {
            var latitudeOrigemRad = latitudeOrigem * Math.PI / 180;
            var latitudeDestinoRad = latitudeDestino * Math.PI / 180;
            var deltaLatitude = latitudeDestinoRad - latitudeOrigemRad;
            var deltaLongitude = (longitudeDestino - longitudeOrigem) * Math.PI / 180;
            var a = Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2)
                + Math.Cos(latitudeOrigemRad) * Math.Cos(latitudeDestinoRad)
                * Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2);
            return 6371 * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
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
