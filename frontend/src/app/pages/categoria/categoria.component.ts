import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BibliotecaApiService, Categoria, ConteudoResumo } from '../../services/biblioteca-api.service';

@Component({
  selector: 'app-categoria',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './categoria.component.html',
  styleUrl: './categoria.component.scss'
})
export class CategoriaComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(BibliotecaApiService);

  categoria: Categoria | null = null;
  conteudos: ConteudoResumo[] = [];
  erro: string | null = null;

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug') ?? '';
    this.api.obterCategoria(slug).subscribe({
      next: (cat) => {
        this.categoria = cat;
      },
      error: () => {
        this.erro = 'Categoria não encontrada.';
      }
    });

    this.api.listarConteudos(slug).subscribe({
      next: (lista) => {
        this.conteudos = lista;
      },
      error: () => {
        this.erro = 'Não foi possível carregar os conteúdos.';
      }
    });
  }

  tipoLabel(tipo: string): string {
    switch (tipo) {
      case 'Faq':
        return 'FAQ';
      case 'Video':
        return 'Vídeo';
      case 'Imagem':
        return 'Imagem';
      default:
        return 'Artigo';
    }
  }
}
