import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-cadastro',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './cadastro.component.html',
  styleUrl: './cadastro.component.scss'
})
export class CadastroComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  loading = false;
  erro: string | null = null;

  form = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(6)]],
    consentimentoLgpd: [false, [Validators.requiredTrue]]
  });

  enviar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.erro = 'Preencha todos os campos e aceite o uso dos dados.';
      return;
    }

    this.loading = true;
    this.erro = null;

    this.auth.cadastrar(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading = false;
        void this.router.navigate(['/inicio']);
      },
      error: (err) => {
        this.loading = false;
        this.erro = err?.error?.mensagem ?? 'Não foi possível criar a conta. Tente de novo.';
      }
    });
  }
}
