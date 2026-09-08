import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent {
  enderecoOuCep: string = '';

  constructor(private router: Router) {}

  onSubmit(event: Event): void {
    event.preventDefault();
    const busca = this.enderecoOuCep.trim();
    if (busca) {
      const cep = busca.replace(/\D/g, '');
      this.router.navigate(['/mapa'], { 
        queryParams: cep.length === 8 ? { cep } : { busca }
      });
    }
  }

  irParaMapa(categoriaSlug: string): void {
    this.router.navigate(['/mapa'], { 
      queryParams: { categoria: categoriaSlug } 
    });
  }
}
