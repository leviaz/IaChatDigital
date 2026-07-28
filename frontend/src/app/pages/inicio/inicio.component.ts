import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-inicio',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './inicio.component.html',
  styleUrl: './inicio.component.scss'
})
export class InicioComponent {
  private readonly auth = inject(AuthService);

  usuario = this.auth.getUsuario();
  mensagem: string | null = null;
  excluindo = false;

  sair(): void {
    this.auth.logout();
  }

  excluirConta(): void {
    const ok = confirm('Tem certeza? Sua conta e o histórico serão apagados.');
    if (!ok) {
      return;
    }

    this.excluindo = true;
    this.auth.excluirConta().subscribe({
      next: () => {
        this.excluindo = false;
        void this.auth.logout();
      },
      error: () => {
        this.excluindo = false;
        this.mensagem = 'Não foi possível excluir a conta agora.';
      }
    });
  }
}
