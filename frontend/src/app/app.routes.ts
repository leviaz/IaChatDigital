import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './guards/auth.guard';
import { LoginComponent } from './pages/login/login.component';
import { CadastroComponent } from './pages/cadastro/cadastro.component';
import { InicioComponent } from './pages/inicio/inicio.component';
import { ChatComponent } from './pages/chat/chat.component';
import { BibliotecaComponent } from './pages/biblioteca/biblioteca.component';
import { CategoriaComponent } from './pages/categoria/categoria.component';
import { ConteudoComponent } from './pages/conteudo/conteudo.component';
import { PraticarComponent } from './pages/praticar/praticar.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'inicio' },
  { path: 'login', component: LoginComponent, canActivate: [guestGuard] },
  { path: 'cadastro', component: CadastroComponent, canActivate: [guestGuard] },
  { path: 'inicio', component: InicioComponent, canActivate: [authGuard] },
  { path: 'biblioteca', component: BibliotecaComponent, canActivate: [authGuard] },
  { path: 'biblioteca/:slug', component: CategoriaComponent, canActivate: [authGuard] },
  { path: 'conteudo/:id', component: ConteudoComponent, canActivate: [authGuard] },
  { path: 'praticar', component: PraticarComponent, canActivate: [authGuard] },
  { path: 'chat', component: ChatComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'inicio' }
];
