import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, concat, throwError } from 'rxjs';
import { filter, map, switchMap, take } from 'rxjs/operators';

interface ViaCepResponse {
  erro?: boolean;
  logradouro: string;
  bairro: string;
  localidade: string;
  uf: string;
}

interface NominatimResult {
  lat: string;
  lon: string;
}

@Injectable({ providedIn: 'root' })
export class CepService {
  constructor(private http: HttpClient) {}

  buscarCoordenadas(cep: string): Observable<[number, number]> {
    const cepNormalizado = cep.replace(/\D/g, '');
    if (cepNormalizado.length !== 8) {
      return throwError(() => new Error('Informe um CEP com 8 dígitos.'));
    }

    return this.http.get<ViaCepResponse>(`https://viacep.com.br/ws/${cepNormalizado}/json/`).pipe(
      switchMap(endereco => {
        if (endereco.erro) {
          return throwError(() => new Error('CEP não encontrado.'));
        }

        const consultas = [
          new HttpParams()
            .set('street', endereco.logradouro)
            .set('city', endereco.localidade)
            .set('state', endereco.uf),
          new HttpParams().set('q', [endereco.bairro, endereco.localidade, endereco.uf, 'Brasil'].join(', ')),
          new HttpParams().set('q', [endereco.localidade, endereco.uf, 'Brasil'].join(', '))
        ];

        const requisicoes = consultas.map(consulta => {
          const params = consulta
            .set('format', 'jsonv2')
            .set('limit', '1')
            .set('countrycodes', 'br');
          return this.http.get<NominatimResult[]>('https://nominatim.openstreetmap.org/search', { params });
        });

        return concat(...requisicoes).pipe(
          filter(resultados => resultados.length > 0),
          take(1)
        );
      }),
      switchMap(resultados => {
        const resultado = resultados[0];
        return resultado
          ? new Observable<[number, number]>(subscriber => {
              subscriber.next([Number(resultado.lat), Number(resultado.lon)]);
              subscriber.complete();
            })
          : throwError(() => new Error('Não foi possível localizar o endereço do CEP.'));
      }),
      map(([latitude, longitude]) => [latitude, longitude])
    );
  }
}
