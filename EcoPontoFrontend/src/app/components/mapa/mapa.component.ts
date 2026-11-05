import { Component, ElementRef, OnDestroy, AfterViewInit, ViewChild, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PontoColeta } from '../../models/ponto-coleta.model';
import { GeolocationService } from '../../services/geolocation.service';
import { CepService } from '../../services/cep.service';
import { PontosService } from '../../services/pontos.service';
import * as L from 'leaflet';
import { marker, Marker } from 'leaflet';

@Component({
  selector: 'app-mapa',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mapa.component.html',
  styleUrls: ['./mapa.component.scss']
})
export class MapaComponent implements AfterViewInit, OnDestroy, OnChanges {
  @ViewChild('map',{ static: true }) mapRef!: ElementRef<HTMLDivElement>;
  @Input() pontos: PontoColeta[] = [];
  @Input() mapCenter: [number, number] | null = null;
  @Input() erroBusca = '';
  @Output() selected = new EventEmitter<PontoColeta>();

  pontoSelecionado: PontoColeta | null = null;
  filtroAtivo = 'todos';
  mensagemBusca = '';

  private map?: L.Map;
  private userMarker?: Marker;
  private userPosition?: [number, number];
  private cepMarker?: Marker;
  private pontoMarkers?: L.LayerGroup;

  private readonly iconeEcoPonto = L.icon({
    iconUrl: '/images/pino-verde.png',
    iconSize: [32, 56],
    iconAnchor: [16, 56],
    popupAnchor: [0, -56]
  });

  private readonly iconeUsuario = L.icon({
    iconUrl: '/images/pino-azul.svg',
    iconSize: [40, 40],
    iconAnchor: [20, 40],
    popupAnchor: [0, -40]
  });

  constructor(
    private geolocation: GeolocationService,
    private cepService: CepService,
    private pontosService: PontosService
  ) {}

  ngAfterViewInit(): void {
    this.map = L.map(this.mapRef.nativeElement, { center: [-23.55052, -46.633308], zoom: 13, zoomControl: false });
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);
    L.control.zoom({ position: 'bottomleft' }).addTo(this.map);

    this.pontoMarkers = L.layerGroup();
    this.pontoMarkers.addTo(this.map);

    this.geolocation.getCurrentPosition().subscribe(pos => {
      this.userPosition = [pos.latitude, pos.longitude];
      if (!this.mapCenter) {
        this.map?.setView([pos.latitude, pos.longitude], 14);
      }
      this.atualizarMarcadorUsuario();
    });

    this.renderPontos();
    if (this.mapCenter) {
      this.map.setView(this.mapCenter, 14);
      this.atualizarMarcadorCep(this.mapCenter);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['pontos']) this.renderPontos();
    if (changes['mapCenter'] && this.mapCenter) {
      this.map?.setView(this.mapCenter, 14);
      this.atualizarMarcadorCep(this.mapCenter);
      this.atualizarMarcadorUsuario();
      this.renderPontos();
    }
  }

  private renderPontos() {
    if (!this.map || !this.pontoMarkers) return;
    this.pontoMarkers.clearLayers();
    const pontosNaRegiao = this.pontosProximos();
    const pontosVisiveis = this.aplicarFiltro(pontosNaRegiao);
    if (pontosVisiveis.length > 0) {
      this.mensagemBusca = '';
    } else if (this.mapCenter && pontosNaRegiao.length === 0) {
      this.mensagemBusca = 'Nenhum EcoPonto foi encontrado próximo a este CEP.';
    } else if (this.filtroAtivo !== 'todos') {
      this.mensagemBusca = 'Nenhum EcoPonto corresponde ao filtro selecionado.';
    } else {
      this.mensagemBusca = '';
    }

    for (const p of pontosVisiveis) {
      const m = marker([p.latitude, p.longitude], { icon: this.iconeEcoPonto }).bindPopup(this.popupHtml(p));
      m.on('click', () => {
        this.pontoSelecionado = p;
        this.selected.emit(p);
      });
      this.pontoMarkers.addLayer(m);
    }

    if (this.mapCenter && pontosVisiveis.length > 0) {
      const bounds = L.latLngBounds([
        this.mapCenter,
        ...pontosVisiveis.map(ponto => [ponto.latitude, ponto.longitude] as [number, number])
      ]);
      this.map.fitBounds(bounds, { padding: [80, 80], maxZoom: 14 });
    }
  }

  filtrar(filtro: string): void {
    this.filtroAtivo = filtro;
    this.renderPontos();
  }

  buscar(termo: string): void {
    const normalizedTerm = termo.trim().toLocaleLowerCase();
    this.mensagemBusca = '';
    if (!normalizedTerm) return;

    if (/^\d{5}-?\d{3}$/.test(normalizedTerm)) {
      this.cepService.buscarCoordenadas(normalizedTerm).subscribe({
        next: center => {
          this.mapCenter = center;
          this.map?.setView(center, 14);
          this.atualizarMarcadorCep(center);
          this.atualizarMarcadorUsuario();
          this.pontosService.getPontosProximos(center[0], center[1]).subscribe({
            next: pontos => {
              this.pontos = pontos;
              this.mensagemBusca = pontos.length === 0
                ? 'Nenhum EcoPonto foi encontrado próximo a este CEP.'
                : '';
              this.renderPontos();
            },
            error: () => {
              this.pontoMarkers?.clearLayers();
              this.mensagemBusca = 'Não foi possível carregar os EcoPontos desta região.';
            }
          });
        },
        error: error => {
          this.mensagemBusca = error instanceof Error
            ? error.message
            : 'Não foi possível localizar este CEP.';
          this.pontoMarkers?.clearLayers();
          console.error('Não foi possível localizar o CEP:', error);
        }
      });
      return;
    }

    const ponto = this.pontos.find(item =>
      `${item.nome} ${item.logradouro} ${item.bairro} ${item.cidade}`.toLocaleLowerCase().includes(normalizedTerm)
    );
    if (!ponto) {
      this.mensagemBusca = 'Nenhum EcoPonto foi encontrado para essa busca.';
      return;
    }

    this.map?.setView([ponto.latitude, ponto.longitude], 15);
    this.pontoSelecionado = ponto;
    this.selected.emit(ponto);
  }

  fecharCard(): void {
    this.pontoSelecionado = null;
  }

  private atualizarMarcadorCep(center: [number, number]): void {
    if (!this.map) return;
    if (this.cepMarker) {
      this.cepMarker.setLatLng(center);
      return;
    }

    this.cepMarker = marker(center, {
      title: 'Localização pesquisada',
      icon: this.iconeUsuario
    }).bindPopup('Localização pesquisada').addTo(this.map);
  }

  private atualizarMarcadorUsuario(): void {
    if (!this.map) return;

    if (this.mapCenter || !this.userPosition) {
      this.userMarker?.remove();
      this.userMarker = undefined;
      return;
    }

    if (this.userMarker) {
      this.userMarker.setLatLng(this.userPosition).addTo(this.map);
      return;
    }

    this.userMarker = marker(this.userPosition, {
      title: 'Você',
      icon: this.iconeUsuario
    }).addTo(this.map);
  }

  rotaPara(ponto: PontoColeta): string {
    return `https://www.google.com/maps/dir/?api=1&destination=${ponto.latitude},${ponto.longitude}`;
  }

  private pontosProximos(): PontoColeta[] {
    if (this.mapCenter) {
      return this.pontos.filter(ponto =>
        this.distanciaEmKm(this.mapCenter!, [ponto.latitude, ponto.longitude]) <= 50
      );
    }

    return this.pontos;
  }

  private aplicarFiltro(pontos: PontoColeta[]): PontoColeta[] {
    if (this.filtroAtivo === 'todos') return pontos;
    return pontos.filter(ponto => ponto.categorias.some(categoria =>
      this.normalizarTexto(categoria.nome).includes(this.filtroAtivo)
    ));
  }

  private normalizarTexto(valor: string): string {
    return valor.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase();
  }

  private distanciaEmKm(origem: [number, number], destino: [number, number]): number {
    const [latitudeOrigem, longitudeOrigem] = origem.map(valor => valor * Math.PI / 180);
    const [latitudeDestino, longitudeDestino] = destino.map(valor => valor * Math.PI / 180);
    const deltaLatitude = latitudeDestino - latitudeOrigem;
    const deltaLongitude = longitudeDestino - longitudeOrigem;
    const a = Math.sin(deltaLatitude / 2) ** 2
      + Math.cos(latitudeOrigem) * Math.cos(latitudeDestino) * Math.sin(deltaLongitude / 2) ** 2;
    return 6371 * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  }

  private popupHtml(p: PontoColeta) {
    const categories = p.categorias.map(c => `<span class="tag">${c.nome}</span>`).join(' ');
    const mapsUrl = `https://www.google.com/maps/dir/?api=1&destination=${p.latitude},${p.longitude}`;
    return `<div class="popup"><strong>${p.nome}</strong><div>${p.logradouro} - ${p.bairro}</div><div>${categories}</div><a href="${mapsUrl}" target="_blank" rel="noopener">Rotas</a></div>`;
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }
}
