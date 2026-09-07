using System.Collections.Generic;

namespace EcoPonto.Domain
{
    public class CategoriaResiduo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string Icone { get; set; } = string.Empty;
        public string OrientacaoDescarte { get; set; } = string.Empty;

        public ICollection<PontoColeta> Pontos { get; set; } = new List<PontoColeta>();
    }
}
