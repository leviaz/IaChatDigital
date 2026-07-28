import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ChatResumo {
  id: string;
  titulo: string;
  dataCriacao: string;
  dataAtualizacao: string;
  ultimaPergunta: string | null;
}

export interface Mensagem {
  id: string;
  chatId: string;
  pergunta: string;
  resposta: string;
  data: string;
  provider: string;
  usouMock: boolean;
  feedbackGostou: boolean | null;
}

export interface ChatDetalhe {
  id: string;
  titulo: string;
  dataCriacao: string;
  dataAtualizacao: string;
  mensagens: Mensagem[];
}

export interface FeedbackResponse {
  id: string;
  conversaId: string;
  gostou: boolean;
  data: string;
}

@Injectable({ providedIn: 'root' })
export class ChatApiService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  listarChats(): Observable<ChatResumo[]> {
    return this.http.get<ChatResumo[]>(`${this.api}/chats`);
  }

  criarChat(titulo?: string): Observable<ChatResumo> {
    return this.http.post<ChatResumo>(`${this.api}/chats`, { titulo: titulo ?? null });
  }

  obterChat(id: string): Observable<ChatDetalhe> {
    return this.http.get<ChatDetalhe>(`${this.api}/chats/${id}`);
  }

  excluirChat(id: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/chats/${id}`);
  }

  enviarMensagem(chatId: string, pergunta: string): Observable<Mensagem> {
    return this.http.post<Mensagem>(`${this.api}/chats/${chatId}/mensagens`, { pergunta });
  }

  enviarFeedback(conversaId: string, gostou: boolean): Observable<FeedbackResponse> {
    return this.http.post<FeedbackResponse>(`${this.api}/feedback`, { conversaId, gostou });
  }
}
