import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BibliotecaApiService, Categoria } from '../../services/biblioteca-api.service';

@Component({
  selector: 'app-biblioteca',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './biblioteca.component.html',
  styleUrl: './biblioteca.component.scss'
})
export class BibliotecaComponent implements OnInit {
  private readonly api = inject(BibliotecaApiService);

  categorias: Categoria[] = [];
  erro: string | null = null;
  loading = true;

  ngOnInit(): void {
    this.api.listarCategorias().subscribe({
      next: (lista) => {
        this.categorias = lista;
        this.loading = false;
      },
      error: () => {
        this.erro = 'Não foi possível carregar a biblioteca agora.';
        this.loading = false;
      }
    });
  }
}
