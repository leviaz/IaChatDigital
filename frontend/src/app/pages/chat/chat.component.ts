import { Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatApiService, ChatResumo, Mensagem } from '../../services/chat-api.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss'
})
export class ChatComponent implements OnInit {
  private readonly chatApi = inject(ChatApiService);

  @ViewChild('threadEl') threadEl?: ElementRef<HTMLElement>;

  pergunta = '';
  loading = false;
  erro: string | null = null;
  chats: ChatResumo[] = [];
  chatAtivoId: string | null = null;
  mensagens: Mensagem[] = [];
  feedbackMsg: string | null = null;
  feedbackMensagemId: string | null = null;
  sugestoes = [
    'Como faço um PIX?',
    'Esse SMS é golpe?',
    'Como bloquear um número no WhatsApp?',
    'Como marcar consulta pelo SUS?'
  ];

  get tituloAtivo(): string {
    return this.chats.find((c) => c.id === this.chatAtivoId)?.titulo ?? 'Novo chat';
  }

  ngOnInit(): void {
    this.carregarChats(true);
  }

  novoChat(): void {
    this.erro = null;
    this.feedbackMsg = null;
    this.chatApi.criarChat().subscribe({
      next: (chat) => {
        this.chats = [chat, ...this.chats.filter((c) => c.id !== chat.id)];
        this.abrirChat(chat.id);
      },
      error: () => {
        this.erro = 'Não foi possível criar um novo chat.';
      }
    });
  }

  abrirChat(id: string): void {
    this.chatAtivoId = id;
    this.erro = null;
    this.feedbackMsg = null;
    this.feedbackMensagemId = null;

    this.chatApi.obterChat(id).subscribe({
      next: (detalhe) => {
        this.mensagens = detalhe.mensagens;
        const idx = this.chats.findIndex((c) => c.id === id);
        if (idx >= 0) {
          this.chats[idx] = {
            ...this.chats[idx],
            titulo: detalhe.titulo,
            dataAtualizacao: detalhe.dataAtualizacao
          };
        }
        setTimeout(() => this.scrollToBottom(), 50);
      },
      error: () => {
        this.erro = 'Não foi possível abrir este chat.';
      }
    });
  }

  excluirChat(event: Event, id: string): void {
    event.stopPropagation();
    const ok = confirm('Apagar este chat e todas as mensagens?');
    if (!ok) {
      return;
    }

    this.chatApi.excluirChat(id).subscribe({
      next: () => {
        this.chats = this.chats.filter((c) => c.id !== id);
        if (this.chatAtivoId === id) {
          if (this.chats.length > 0) {
            this.abrirChat(this.chats[0].id);
          } else {
            this.chatAtivoId = null;
            this.mensagens = [];
            this.novoChat();
          }
        }
      },
      error: () => {
        this.erro = 'Não foi possível apagar o chat.';
      }
    });
  }

  usarSugestao(texto: string): void {
    this.pergunta = texto;
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.enviar();
    }
  }

  enviar(): void {
    const texto = this.pergunta.trim();
    if (!texto || this.loading) {
      return;
    }

    const enviarNoChat = (chatId: string) => {
      this.loading = true;
      this.erro = null;
      this.feedbackMsg = null;
      this.scrollToBottom();

      this.chatApi.enviarMensagem(chatId, texto).subscribe({
        next: (msg) => {
          this.pergunta = '';
          this.mensagens = [...this.mensagens, msg];
          this.feedbackMensagemId = msg.id;
          this.loading = false;

          const idx = this.chats.findIndex((c) => c.id === chatId);
          if (idx >= 0) {
            const atualizado: ChatResumo = {
              ...this.chats[idx],
              titulo: this.chats[idx].titulo === 'Novo chat' ? texto.slice(0, 60) : this.chats[idx].titulo,
              dataAtualizacao: msg.data,
              ultimaPergunta: texto
            };
            this.chats = [atualizado, ...this.chats.filter((c) => c.id !== chatId)];
          }

          setTimeout(() => this.scrollToBottom(), 50);
        },
        error: (err) => {
          this.loading = false;
          this.erro = err?.error?.mensagem ?? 'Não foi possível responder agora. Tente de novo.';
        }
      });
    };

    if (!this.chatAtivoId) {
      this.chatApi.criarChat().subscribe({
        next: (chat) => {
          this.chats = [chat, ...this.chats];
          this.chatAtivoId = chat.id;
          this.mensagens = [];
          enviarNoChat(chat.id);
        },
        error: () => {
          this.erro = 'Não foi possível criar um novo chat.';
        }
      });
      return;
    }

    enviarNoChat(this.chatAtivoId);
  }

  feedback(mensagem: Mensagem, gostou: boolean): void {
    this.feedbackMensagemId = mensagem.id;
    this.chatApi.enviarFeedback(mensagem.id, gostou).subscribe({
      next: () => {
        mensagem.feedbackGostou = gostou;
        this.feedbackMsg = gostou
          ? 'Obrigado! Ficamos felizes em ajudar.'
          : 'Obrigado. Vamos melhorar as respostas.';
      },
      error: () => {
        this.feedbackMsg = 'Não foi possível salvar sua opinião agora.';
      }
    });
  }

  private carregarChats(abrirMaisRecente: boolean): void {
    this.chatApi.listarChats().subscribe({
      next: (lista) => {
        this.chats = lista;
        if (lista.length === 0) {
          this.novoChat();
          return;
        }

        if (abrirMaisRecente) {
          this.abrirChat(lista[0].id);
        }
      },
      error: () => {
        this.erro = 'Não foi possível carregar seus chats.';
      }
    });
  }

  private scrollToBottom(): void {
    const el = this.threadEl?.nativeElement;
    if (!el) {
      return;
    }

    el.scrollTop = el.scrollHeight;
  }
}
