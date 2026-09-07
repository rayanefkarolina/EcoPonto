import { Component, ElementRef, OnDestroy, AfterViewInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PontoColeta } from '../../models/ponto-coleta.model';
import { GeolocationService } from '../../services/geolocation.service';
import { PontosService } from '../../services/pontos.service';
import * as L from 'leaflet';

@Component({
  selector: 'app-mapa',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mapa.component.html',
  styleUrls: ['./mapa.component.scss']
})
export class MapaComponent implements AfterViewInit, OnDestroy {
  @ViewChild('map',{ static: true }) mapRef!: ElementRef<HTMLDivElement>;
  pontos: PontoColeta[] = [];

  private map?: L.Map;
  private userMarker?: L.Marker;
  private pontoMarkers: L.LayerGroup = L.layerGroup();

  constructor(private geolocation: GeolocationService, private pontosService: PontosService) {}

  ngAfterViewInit(): void {
    this.map = L.map(this.mapRef.nativeElement, { center: [-23.55052, -46.633308], zoom: 13 });
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    this.pontoMarkers.addTo(this.map);

    this.geolocation.getCurrentPosition().subscribe(pos => {
      this.map?.setView([pos.latitude, pos.longitude], 14);
      if (this.userMarker) this.userMarker.setLatLng([pos.latitude, pos.longitude]);
      else {
        this.userMarker = L.marker([pos.latitude, pos.longitude], { title: 'Você' }).addTo(this.map!);
      }
    });

    this.pontosService.getPontos().subscribe(p => { this.pontos = p; this.renderPontos(); });
  }

  ngOnChanges(): void {
    this.renderPontos();
  }

  private renderPontos() {
    if (!this.map) return;
    this.pontoMarkers.clearLayers();
    for (const p of this.pontos) {
      const marker = L.marker([p.latitude, p.longitude]).bindPopup(this.popupHtml(p));
      this.pontoMarkers.addLayer(marker);
    }
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
