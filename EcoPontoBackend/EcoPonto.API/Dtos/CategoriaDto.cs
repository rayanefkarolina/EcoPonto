namespace EcoPonto.API.Dtos
{
    public class CategoriaDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string Icone { get; set; } = string.Empty;
        public string OrientacaoDescarte { get; set; } = string.Empty;
    }
}
