import { Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ChatApiService, ChatResumo, Mensagem } from '../../services/chat-api.service';
import { VozService } from '../../services/voz.service';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss'
})
export class ChatComponent implements OnInit, OnDestroy {
  private readonly chatApi = inject(ChatApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly voz = inject(VozService);

  @ViewChild('threadEl') threadEl?: ElementRef<HTMLElement>;

  pergunta = '';
  loading = false;
  erro: string | null = null;
  chats: ChatResumo[] = [];
  chatAtivoId: string | null = null;
  mensagens: Mensagem[] = [];
  feedbackMsg: string | null = null;
  feedbackMensagemId: string | null = null;
  ouvindo = false;
  statusVoz: string | null = null;
  lendoId: string | null = null;
  private rascunhoAntesVoz = '';
  private pendentePergunta: string | null = null;
  private forcarNovoChat = false;
  sugestoes = [
    'Como faço um PIX?',
    'Esse SMS é golpe?',
    'Como bloquear um número no WhatsApp?',
    'Como marcar consulta pelo SUS?'
  ];

  get tituloAtivo(): string {
    return this.chats.find((c) => c.id === this.chatAtivoId)?.titulo ?? 'Novo chat';
  }

  get vozDisponivel(): boolean {
    return this.voz.suportado;
  }

  get sinteseDisponivel(): boolean {
    return this.voz.sinteseSuportada;
  }

  ngOnInit(): void {
    const pergunta = this.route.snapshot.queryParamMap.get('pergunta');
    this.forcarNovoChat = this.route.snapshot.queryParamMap.get('novo') === '1';
    if (pergunta) {
      this.pendentePergunta = pergunta;
      this.pergunta = pergunta;
    }

    this.carregarChats(!this.forcarNovoChat);
  }

  ngOnDestroy(): void {
    this.voz.pararDitacao();
    this.voz.pararFala();
  }

  novoChat(): void {
    this.erro = null;
    this.feedbackMsg = null;
    this.chatApi.criarChat().subscribe({
      next: (chat) => {
        this.chats = [chat, ...this.chats.filter((c) => c.id !== chat.id)];
        this.abrirChat(chat.id, false);
        if (this.pendentePergunta) {
          this.pergunta = this.pendentePergunta;
          this.pendentePergunta = null;
        }
      },
      error: () => {
        this.erro = 'Não foi possível criar um novo chat.';
      }
    });
  }

  abrirChat(id: string, limparPendente = true): void {
    this.chatAtivoId = id;
    this.erro = null;
    this.feedbackMsg = null;
    this.feedbackMensagemId = null;
    this.pararVoz();
    if (limparPendente) {
      this.pendentePergunta = null;
    }

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
    this.pararVoz();
    this.pergunta = texto;
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.enviar();
    }
  }

  alternarVoz(): void {
    if (this.loading) {
      return;
    }

    if (!this.vozDisponivel) {
      this.statusVoz = 'Seu navegador não permite falar. Use Chrome ou Edge.';
      return;
    }

    if (this.ouvindo) {
      this.voz.pararDitacao();
      this.ouvindo = false;
      this.statusVoz = 'Microfone parado.';
      return;
    }

    this.rascunhoAntesVoz = this.pergunta.trim();
    this.erro = null;
    this.ouvindo = true;
    this.statusVoz = 'Ouvindo… fale agora.';

    this.voz.iniciarDitacao({
      onParcial: (texto) => {
        this.pergunta = this.juntarTexto(this.rascunhoAntesVoz, texto);
      },
      onFinal: (texto) => {
        this.pergunta = this.juntarTexto(this.rascunhoAntesVoz, texto);
        this.rascunhoAntesVoz = this.pergunta;
        this.statusVoz = 'Pronto. Confira o texto e toque em Enviar.';
      },
      onErro: (tipo) => {
        this.ouvindo = false;
        this.statusVoz = this.mensagemErroVoz(tipo);
      },
      onFim: () => {
        this.ouvindo = false;
        if (this.statusVoz === 'Ouvindo… fale agora.') {
          this.statusVoz = this.pergunta.trim()
            ? 'Pronto. Confira o texto e toque em Enviar.'
            : 'Não entendi. Toque em Falar e tente de novo.';
        }
      }
    });
  }

  ouvirResposta(msg: Mensagem): void {
    if (!this.sinteseDisponivel) {
      this.statusVoz = 'Seu navegador não lê texto em voz alta.';
      return;
    }

    if (this.lendoId === msg.id) {
      this.voz.pararFala();
      this.lendoId = null;
      this.statusVoz = 'Leitura parada.';
      return;
    }

    this.voz.pararFala();
    this.lendoId = msg.id;
    this.statusVoz = 'Lendo a resposta em voz alta…';
    this.voz.falar(msg.resposta, () => {
      if (this.lendoId === msg.id) {
        this.lendoId = null;
        this.statusVoz = null;
      }
    });
  }

  enviar(): void {
    const texto = this.pergunta.trim();
    if (!texto || this.loading) {
      return;
    }

    this.pararVoz();

    const enviarNoChat = (chatId: string) => {
      this.loading = true;
      this.erro = null;
      this.feedbackMsg = null;
      this.statusVoz = null;
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

  private pararVoz(): void {
    this.voz.pararDitacao();
    this.voz.pararFala();
    this.ouvindo = false;
    this.lendoId = null;
  }

  private juntarTexto(base: string, falado: string): string {
    if (!base) {
      return falado;
    }
    if (!falado) {
      return base;
    }
    return `${base} ${falado}`.replace(/\s+/g, ' ').trim();
  }

  private mensagemErroVoz(tipo: string): string {
    switch (tipo) {
      case 'nao-suportado':
        return 'Seu navegador não permite falar. Use Chrome ou Edge.';
      case 'permissao':
        return 'Permita o uso do microfone para falar sua pergunta.';
      case 'sem-audio':
        return 'Não ouvi nada. Toque em Falar e tente de novo.';
      case 'rede':
        return 'Sem conexão para reconhecer a voz. Tente de novo.';
      default:
        return 'Não foi possível usar o microfone agora.';
    }
  }

  private carregarChats(abrirMaisRecente: boolean): void {
    this.chatApi.listarChats().subscribe({
      next: (lista) => {
        this.chats = lista;
        if (lista.length === 0 || this.forcarNovoChat) {
          this.forcarNovoChat = false;
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
