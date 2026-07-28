import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  private readonly auth = inject(AuthService);

  get autenticado(): boolean {
    return this.auth.isAuthenticated();
  }

  get nome(): string {
    return this.auth.getUsuario()?.nome ?? '';
  }

  sair(): void {
    this.auth.logout();
  }
}
