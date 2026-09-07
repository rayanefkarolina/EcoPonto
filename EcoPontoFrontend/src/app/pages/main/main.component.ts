import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PontosService } from '../../services/pontos.service';
import { GeolocationService } from '../../services/geolocation.service';
import { PontoColeta } from '../../models/ponto-coleta.model';
import { MapaComponent } from '../../components/mapa/mapa.component';
import { ListaPontosComponent } from '../../components/lista-pontos/lista-pontos.component';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-main',
  standalone: true,
  imports: [CommonModule, MapaComponent, ListaPontosComponent],
  templateUrl: './main.component.html',
  styleUrls: ['./main.component.scss']
})
export class MainComponent implements OnInit {
  pontos: PontoColeta[] = [];
  categorias: any[] = [];
  selected?: PontoColeta;
  userPos: { latitude: number; longitude: number } | null = null;

  constructor(private pontosService: PontosService, private geo: GeolocationService, private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.loadCategorias();
    this.geo.getCurrentPosition().subscribe(pos => { this.userPos = pos; });
    this.loadPontosFromQuery();
  }

  loadCategorias() { this.pontosService.getCategorias().subscribe(c => this.categorias = c); }

  loadPontos(categoriaId?: number, busca?: string) {
    this.pontosService.getPontos(categoriaId, busca).subscribe(p => this.pontos = p);
  }

  loadPontosFromQuery() {
    this.route.queryParams.subscribe(q => {
      const categoriaId = q['categoriaId'] ? Number(q['categoriaId']) : undefined;
      const qstr = q['q'] || q['q'] === '' ? q['q'] : undefined;
      const lat = q['lat'];
      const lng = q['lng'];
      if (lat && lng) {
        // if coords passed, center map via userPos
        this.userPos = { latitude: Number(lat), longitude: Number(lng) };
      }
      this.loadPontos(categoriaId, qstr);
    });
  }

  onSelect(p: PontoColeta) { this.selected = p; }

  getCategoriasNames(p: PontoColeta) {
    if (!p || !p.categorias) return '';
    return p.categorias.map(c => c.nome).join(', ');
  }
}
