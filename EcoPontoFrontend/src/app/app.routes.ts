import { Routes } from '@angular/router';
import { provideRouter } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { MainComponent } from './pages/main/main.component';
import { SobreNosComponent } from './pages/sobre-nos/sobre-nos.component';
import { ComoFuncionaComponent } from './pages/como-funciona/como-funciona.component';

export const routes: Routes = [
	{ path: '', component: HomeComponent },
	{ path: 'map', component: MainComponent },
	{ path: 'mapa', component: MainComponent },
	{ path: 'sobre-nos', component: SobreNosComponent },
	{ path: 'como-funciona', component: ComoFuncionaComponent },
	{ path: '**', redirectTo: '' }
];
