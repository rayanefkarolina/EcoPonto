import { Routes } from '@angular/router';
import { provideRouter } from '@angular/router';
import { LandingComponent } from './pages/landing/landing.component';
import { HomeComponent } from './pages/home/home.component';
import { MainComponent } from './pages/main/main.component';

export const routes: Routes = [
	{ path: '', component: HomeComponent },
	{ path: 'map', component: MainComponent },
	{ path: 'mapa', component: MainComponent },
	{ path: '**', redirectTo: '' }
];
