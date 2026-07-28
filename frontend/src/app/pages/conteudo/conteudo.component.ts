import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BibliotecaApiService, ConteudoDetalhe } from '../../services/biblioteca-api.service';

@Component({
  selector: 'app-conteudo',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './conteudo.component.html',
  styleUrl: './conteudo.component.scss'
})
export class ConteudoComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(BibliotecaApiService);

  conteudo: ConteudoDetalhe | null = null;
  erro: string | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.api.obterConteudo(id).subscribe({
      next: (item) => {
        this.conteudo = item;
      },
      error: () => {
        this.erro = 'Conteúdo não encontrado.';
      }
    });
  }

  get perguntaSugestao(): string {
    if (!this.conteudo) {
      return '';
    }

    return `Quero saber mais sobre: ${this.conteudo.titulo}`;
  }
}
