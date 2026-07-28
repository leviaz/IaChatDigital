import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ExercicioPublico {
  id: string;
  pergunta: string;
  alternativas: string[];
  categoria: string;
  nivelDificuldade: number;
  provider: string;
  usouMock: boolean;
}

export interface RespostaExercicio {
  resultadoId: string;
  acertou: boolean;
  respostaCorreta: string;
  explicacao: string;
  acertosTotais: number;
  nivelAtual: number;
}

export interface Pontuacao {
  acertos: number;
  erros: number;
  total: number;
  nivelAtual: number;
  categoria: string;
}

@Injectable({ providedIn: 'root' })
export class ExerciciosApiService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  gerar(categoria?: string, conteudoId?: string): Observable<ExercicioPublico> {
    return this.http.post<ExercicioPublico>(`${this.api}/exercicios/gerar`, {
      categoria: categoria ?? null,
      conteudoId: conteudoId ?? null
    });
  }

  responder(id: string, alternativa: string): Observable<RespostaExercicio> {
    return this.http.post<RespostaExercicio>(`${this.api}/exercicios/${id}/responder`, { alternativa });
  }

  pontuacao(categoria?: string): Observable<Pontuacao> {
    let params = new HttpParams();
    if (categoria) {
      params = params.set('categoria', categoria);
    }

    return this.http.get<Pontuacao>(`${this.api}/exercicios/pontuacao`, { params });
  }
}
