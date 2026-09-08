import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PontosService } from '../../services/pontos.service';
import { GeolocationService } from '../../services/geolocation.service';
import { PontoColeta } from '../../models/ponto-coleta.model';
import { MapaComponent } from '../../components/mapa/mapa.component';
import { ActivatedRoute } from '@angular/router';
import { CepService } from '../../services/cep.service';

@Component({
  selector: 'app-main',
  standalone: true,
  imports: [CommonModule, MapaComponent],
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {
  pontos: PontoColeta[] = [];
  categorias: any[] = [];
  selected?: PontoColeta;
  userPos: { latitude: number; longitude: number } | null = null;
  mapCenter: [number, number] | null = null;
  erroBusca = '';

  constructor(private pontosService: PontosService, private geo: GeolocationService, private route: ActivatedRoute, private cepService: CepService) {}

  ngOnInit(): void {
    this.loadCategorias();
    this.geo.getCurrentPosition().subscribe(pos => { this.userPos = pos; });
    this.loadPontosFromQuery();
  }

  loadCategorias() {
    this.pontosService.getCategorias().subscribe({
      next: categorias => this.categorias = categorias,
      error: () => this.erroBusca = 'Não foi possível carregar as categorias. Verifique se a API está em execução.'
    });
  }

  loadPontos(categoriaId?: number, busca?: string) {
    this.pontosService.getPontos(categoriaId, busca).subscribe({
      next: pontos => this.pontos = pontos,
      error: () => this.erroBusca = 'Não foi possível carregar os EcoPontos. Verifique se a API está em execução.'
    });
  }

  loadPontosFromQuery() {
    this.route.queryParams.subscribe(q => {
      const categoriaId = q['categoriaId'] ? Number(q['categoriaId']) : undefined;
      const busca = q['busca'] ?? q['q'];
      const cep = q['cep'];
      const categoriaSlug = q['categoria'];
      const lat = q['lat'];
      const lng = q['lng'];
      if (lat && lng) {
        // if coords passed, center map via userPos
        this.userPos = { latitude: Number(lat), longitude: Number(lng) };
      }
      if (cep) {
        this.erroBusca = '';
        this.cepService.buscarCoordenadas(cep).subscribe({
          next: center => {
            this.mapCenter = center;
            this.pontosService.getPontosProximos(center[0], center[1]).subscribe({
              next: pontos => this.pontos = pontos,
              error: () => this.erroBusca = 'Não foi possível carregar os EcoPontos desta região.'
            });
          },
          error: error => {
            this.erroBusca = error instanceof Error
              ? error.message
              : 'Não foi possível localizar este CEP.';
            console.error('Não foi possível localizar o CEP:', error);
          }
        });
        return;
      }
      if (categoriaId) {
        this.loadPontos(categoriaId, busca);
        return;
      }

      if (categoriaSlug) {
        this.pontosService.getCategorias().subscribe(categorias => {
          const requestedSlug = this.slug(categoriaSlug);
          const categoria = categorias.find(item => {
            const categorySlug = this.slug(item.nome);
            return categorySlug === requestedSlug
              || (requestedSlug === 'pilhas' && categorySlug === 'pilhas-baterias');
          });
          this.loadPontos(categoria?.id, busca);
        });
        return;
      }

      this.loadPontos(undefined, busca);
    });
  }

  private slug(value: string): string {
    return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase().replace(/[^a-z0-9]+/g, '-');
  }

  onSelect(p: PontoColeta) { this.selected = p; }

  getCategoriasNames(p: PontoColeta) {
    if (!p || !p.categorias) return '';
    return p.categorias.map(c => c.nome).join(', ');
  }
}
