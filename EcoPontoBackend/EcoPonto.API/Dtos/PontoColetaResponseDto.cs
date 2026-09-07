using System.Collections.Generic;

namespace EcoPonto.API.Dtos
{
    public class PontoColetaResponseDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string Logradouro { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Cidade { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public string HorarioFuncionamento { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<CategoriaDto> Categorias { get; set; } = new List<CategoriaDto>();
    }
}
