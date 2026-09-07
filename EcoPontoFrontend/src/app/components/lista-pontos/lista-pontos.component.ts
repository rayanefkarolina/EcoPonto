import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PontosService } from '../../services/pontos.service';
import { GeolocationService } from '../../services/geolocation.service';
import { PontoColeta } from '../../models/ponto-coleta.model';
import { Categoria } from '../../models/categoria.model';

@Component({
  selector: 'app-lista-pontos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './lista-pontos.component.html',
  styleUrls: ['./lista-pontos.component.scss']
})
export class ListaPontosComponent implements OnInit {
  pontos = signal<PontoColeta[]>([]);
  categorias = signal<Categoria[]>([]);
  busca = signal('');
  categoriaAtiva = signal<number | null>(null);
  userPos: { latitude: number; longitude: number } | null = null;

  constructor(private pontosService: PontosService, private geolocation: GeolocationService) {}

  ngOnInit(): void {
    this.loadCategorias();
    this.geolocation.getCurrentPosition().subscribe(pos => {
      this.userPos = pos;
      this.loadPontos();
    });
  }

  loadCategorias() {
    this.pontosService.getCategorias().subscribe(c => this.categorias.set(c));
  }

  loadPontos() {
    const categoriaId = this.categoriaAtiva();
    const busca = this.busca();
    this.pontosService.getPontos(categoriaId ?? undefined, busca ?? undefined).subscribe(p => this.pontos.set(p));
  }

  setCategoria(id: number | null) {
    this.categoriaAtiva.set(id);
    this.loadPontos();
  }

  distancia(p: PontoColeta) {
    if (!this.userPos) return '—';
    const R = 6371; // km
    const dLat = this.toRad(p.latitude - this.userPos.latitude);
    const dLon = this.toRad(p.longitude - this.userPos.longitude);
    const a = Math.sin(dLat/2) * Math.sin(dLat/2) + Math.cos(this.toRad(this.userPos.latitude)) * Math.cos(this.toRad(p.latitude)) * Math.sin(dLon/2) * Math.sin(dLon/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    const d = R * c;
    return d < 1 ? `${Math.round(d*1000)} m` : `${d.toFixed(2)} km`;
  }

  private toRad(v: number) { return v * Math.PI / 180; }

  getCategoriasNames(p: PontoColeta): string {
    if (!p?.categorias) return '';
    return p.categorias.map(c => c.nome).filter(Boolean).join(', ');
  }
}
