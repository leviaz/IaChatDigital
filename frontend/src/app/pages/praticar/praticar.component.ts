import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  ExercicioPublico,
  ExerciciosApiService,
  Pontuacao,
  RespostaExercicio
} from '../../services/exercicios-api.service';

@Component({
  selector: 'app-praticar',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './praticar.component.html',
  styleUrl: './praticar.component.scss'
})
export class PraticarComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(ExerciciosApiService);

  categoria = 'golpes';
  conteudoId: string | null = null;
  exercicio: ExercicioPublico | null = null;
  resultado: RespostaExercicio | null = null;
  pontuacao: Pontuacao | null = null;
  loading = false;
  enviando = false;
  erro: string | null = null;
  escolhida: string | null = null;

  ngOnInit(): void {
    this.categoria = this.route.snapshot.queryParamMap.get('categoria') ?? 'golpes';
    this.conteudoId = this.route.snapshot.queryParamMap.get('conteudoId');
    this.carregarPontuacao();
    this.gerar();
  }

  gerar(): void {
    this.loading = true;
    this.erro = null;
    this.resultado = null;
    this.escolhida = null;
    this.exercicio = null;

    this.api.gerar(this.categoria, this.conteudoId ?? undefined).subscribe({
      next: (ex) => {
        this.exercicio = ex;
        this.loading = false;
      },
      error: () => {
        this.erro = 'Não foi possível gerar o exercício. Tente de novo.';
        this.loading = false;
      }
    });
  }

  responder(alternativa: string): void {
    if (!this.exercicio || this.enviando || this.resultado) {
      return;
    }

    this.escolhida = alternativa;
    this.enviando = true;
    const letra = alternativa.trim().charAt(0).toUpperCase();

    this.api.responder(this.exercicio.id, letra).subscribe({
      next: (res) => {
        this.resultado = res;
        this.enviando = false;
        this.carregarPontuacao();
      },
      error: () => {
        this.erro = 'Não foi possível registrar sua resposta.';
        this.enviando = false;
      }
    });
  }

  letra(alternativa: string): string {
    return alternativa.trim().charAt(0).toUpperCase();
  }

  private carregarPontuacao(): void {
    this.api.pontuacao(this.categoria).subscribe({
      next: (p) => {
        this.pontuacao = p;
      }
    });
  }
}
