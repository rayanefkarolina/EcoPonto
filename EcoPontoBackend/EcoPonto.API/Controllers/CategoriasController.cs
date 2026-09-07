using EcoPonto.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcoPonto.API.Dtos;

namespace EcoPonto.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoriaDto>>> Get()
        {
            var categorias = await _context.Categorias.AsNoTracking()
                .Select(c => new CategoriaDto { Id = c.Id, Nome = c.Nome, Icone = c.Icone, OrientacaoDescarte = c.OrientacaoDescarte })
                .ToListAsync();

            return Ok(categorias);
        }
    }
}
