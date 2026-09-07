using EcoPonto.API.Dtos;
using EcoPonto.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                query = query.Where(p => p.Nome.ToLower().Contains(termo) || p.Bairro.ToLower().Contains(termo) || p.Cidade.ToLower().Contains(termo));
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
