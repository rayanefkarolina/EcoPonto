import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PontoColeta } from '../models/ponto-coleta.model';
import { Categoria } from '../models/categoria.model';

@Injectable({ providedIn: 'root' })
export class PontosService {
  private base = '/api';

  constructor(private http: HttpClient) {}

  getPontos(categoriaId?: number, busca?: string): Observable<PontoColeta[]> {
    let params = new HttpParams();
    if (categoriaId) params = params.set('categoriaId', categoriaId.toString());
    if (busca) params = params.set('busca', busca);
    return this.http.get<PontoColeta[]>(`${this.base}/pontos`, { params });
  }

  getCategorias(): Observable<Categoria[]> {
    return this.http.get<Categoria[]>(`${this.base}/categorias`);
  }
}
