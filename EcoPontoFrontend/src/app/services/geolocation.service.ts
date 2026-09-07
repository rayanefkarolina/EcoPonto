import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface PositionCoords {
  latitude: number;
  longitude: number;
}

@Injectable({ providedIn: 'root' })
export class GeolocationService {
  private default: PositionCoords = { latitude: -23.55052, longitude: -46.633308 };

  getCurrentPosition(): Observable<PositionCoords> {
    return new Observable((subscriber) => {
      if (!navigator.geolocation) {
        subscriber.next(this.default);
        subscriber.complete();
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (pos) => {
          subscriber.next({ latitude: pos.coords.latitude, longitude: pos.coords.longitude });
          subscriber.complete();
        },
        () => {
          subscriber.next(this.default);
          subscriber.complete();
        },
        { enableHighAccuracy: true, timeout: 5000 }
      );
    });
  }
}
