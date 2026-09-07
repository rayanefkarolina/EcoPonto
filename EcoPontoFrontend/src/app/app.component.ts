import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MapaComponent } from './components/mapa/mapa.component';
import { ListaPontosComponent } from './components/lista-pontos/lista-pontos.component';
import { GuiaDescarteComponent } from './components/guia-descarte/guia-descarte.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, MapaComponent, ListaPontosComponent, GuiaDescarteComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'EcoPonto Local';
}
