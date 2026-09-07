import { Component, ElementRef, OnDestroy, AfterViewInit, ViewChild, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PontoColeta } from '../../models/ponto-coleta.model';
import { GeolocationService } from '../../services/geolocation.service';
import { Icon, marker, Marker } from 'leaflet';

const defaultIcon = Icon.Default.prototype;
// no-op: we'll set icon options in ngAfterViewInit
import * as L from 'leaflet';
// Use asset URLs for marker images instead of importing binaries
const markerIcon2x = '/images/marker-icon-2x.png';
const markerIcon = '/images/marker-icon.png';
const markerShadow = '/images/marker-shadow.png';

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
  @Output() selected = new EventEmitter<PontoColeta>();

  private map?: L.Map;
  private userMarker?: Marker;
  private pontoMarkers?: L.LayerGroup;

  constructor(private geolocation: GeolocationService) {}

  ngAfterViewInit(): void {
    // set default marker icon paths to work with angular build
    // configure default Leaflet marker icons (works with bundlers)
    try {
      Icon.Default.mergeOptions({ iconRetinaUrl: markerIcon2x, iconUrl: markerIcon, shadowUrl: markerShadow } as any);
    } catch (e) {
      // ignore if import resolution fails
    }

    this.map = L.map(this.mapRef.nativeElement, { center: [-23.55052, -46.633308], zoom: 13 });
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors'
    }).addTo(this.map);

    this.pontoMarkers = L.layerGroup();
    this.pontoMarkers.addTo(this.map);

    this.geolocation.getCurrentPosition().subscribe(pos => {
      this.map?.setView([pos.latitude, pos.longitude], 14);
      if (this.userMarker) this.userMarker.setLatLng([pos.latitude, pos.longitude]);
      else {
        this.userMarker = marker([pos.latitude, pos.longitude], { title: 'Você' }).addTo(this.map!);
      }
    });

    this.renderPontos();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['pontos']) this.renderPontos();
  }

  private renderPontos() {
    if (!this.map || !this.pontoMarkers) return;
    this.pontoMarkers.clearLayers();
    for (const p of this.pontos) {
      const m = marker([p.latitude, p.longitude]).bindPopup(this.popupHtml(p));
      m.on('click', () => this.selected.emit(p));
      this.pontoMarkers.addLayer(m);
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
