import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { GeolocationService } from '../../services/geolocation.service';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent {
  cep = '';

  categorias = [
    { id: 1, nome: 'Óleo de Cozinha' },
    { id: 2, nome: 'Pilhas e Baterias' },
    { id: 3, nome: 'Eletrônicos' },
    { id: 4, nome: 'Medicamentos' },
    { id: 5, nome: 'Recicláveis' }
  ];

  constructor(private router: Router, private geo: GeolocationService) {}

  usarMinhaLocalizacao() {
    this.geo.getCurrentPosition().subscribe(pos => {
      // navegar para a página principal com coords
      this.router.navigate(['/map'], { queryParams: { lat: pos.latitude, lng: pos.longitude } });
    });
  }

  buscar() {
    // navegar para /map com busca
    this.router.navigate(['/map'], { queryParams: { q: this.cep } });
  }

  goToCategoria(id: number) {
    this.router.navigate(['/map'], { queryParams: { categoriaId: id } });
  }
}
