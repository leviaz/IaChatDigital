import { Injectable, NgZone } from '@angular/core';

type SpeechRecognitionCtor = new () => SpeechRecognitionLike;

interface SpeechRecognitionLike {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  maxAlternatives: number;
  start(): void;
  stop(): void;
  abort(): void;
  onstart: ((ev: Event) => void) | null;
  onend: ((ev: Event) => void) | null;
  onerror: ((ev: SpeechRecognitionErrorEventLike) => void) | null;
  onresult: ((ev: SpeechRecognitionEventLike) => void) | null;
}

interface SpeechRecognitionEventLike {
  resultIndex: number;
  results: ArrayLike<{
    isFinal: boolean;
    0: { transcript: string };
    length: number;
  }>;
}

interface SpeechRecognitionErrorEventLike {
  error: string;
  message?: string;
}

export type VozErro =
  | 'nao-suportado'
  | 'permissao'
  | 'sem-audio'
  | 'rede'
  | 'outro';

@Injectable({ providedIn: 'root' })
export class VozService {
  private recognition: SpeechRecognitionLike | null = null;
  private ouvindo = false;

  constructor(private readonly zone: NgZone) {}

  get suportado(): boolean {
    return !!this.getCtor();
  }

  get sinteseSuportada(): boolean {
    return typeof window !== 'undefined' && 'speechSynthesis' in window;
  }

  get estaOuvindo(): boolean {
    return this.ouvindo;
  }

  iniciarDitacao(handlers: {
    onParcial?: (texto: string) => void;
    onFinal: (texto: string) => void;
    onErro: (tipo: VozErro, detalhe?: string) => void;
    onFim?: () => void;
  }): void {
    if (!this.suportado) {
      handlers.onErro('nao-suportado');
      return;
    }

    this.pararDitacao();

    const Ctor = this.getCtor()!;
    const rec = new Ctor();
    this.recognition = rec;
    rec.lang = 'pt-BR';
    rec.continuous = false;
    rec.interimResults = true;
    rec.maxAlternatives = 1;

    rec.onstart = () => {
      this.zone.run(() => {
        this.ouvindo = true;
      });
    };

    rec.onresult = (event) => {
      this.zone.run(() => {
        let parcial = '';
        let final = '';
        for (let i = event.resultIndex; i < event.results.length; i++) {
          const result = event.results[i];
          const texto = result[0]?.transcript ?? '';
          if (result.isFinal) {
            final += texto;
          } else {
            parcial += texto;
          }
        }

        if (parcial) {
          handlers.onParcial?.(parcial.trim());
        }
        if (final.trim()) {
          handlers.onFinal(final.trim());
        }
      });
    };

    rec.onerror = (event) => {
      this.zone.run(() => {
        this.ouvindo = false;
        handlers.onErro(this.mapErro(event.error), event.message);
      });
    };

    rec.onend = () => {
      this.zone.run(() => {
        this.ouvindo = false;
        this.recognition = null;
        handlers.onFim?.();
      });
    };

    try {
      rec.start();
    } catch {
      this.ouvindo = false;
      handlers.onErro('outro', 'Não foi possível iniciar o microfone.');
    }
  }

  pararDitacao(): void {
    if (!this.recognition) {
      this.ouvindo = false;
      return;
    }

    try {
      this.recognition.stop();
    } catch {
      try {
        this.recognition.abort();
      } catch {
        /* ignore */
      }
    }

    this.recognition = null;
    this.ouvindo = false;
  }

  falar(texto: string, onFim?: () => void): void {
    if (!this.sinteseSuportada || !texto.trim()) {
      return;
    }

    this.pararFala();
    const utter = new SpeechSynthesisUtterance(texto);
    utter.lang = 'pt-BR';
    utter.rate = 0.92;
    utter.onend = () => {
      this.zone.run(() => onFim?.());
    };
    utter.onerror = () => {
      this.zone.run(() => onFim?.());
    };
    window.speechSynthesis.speak(utter);
  }

  pararFala(): void {
    if (this.sinteseSuportada) {
      window.speechSynthesis.cancel();
    }
  }

  private getCtor(): SpeechRecognitionCtor | null {
    const w = window as Window & {
      SpeechRecognition?: SpeechRecognitionCtor;
      webkitSpeechRecognition?: SpeechRecognitionCtor;
    };
    return w.SpeechRecognition ?? w.webkitSpeechRecognition ?? null;
  }

  private mapErro(code: string): VozErro {
    switch (code) {
      case 'not-allowed':
      case 'service-not-allowed':
        return 'permissao';
      case 'no-speech':
      case 'audio-capture':
        return 'sem-audio';
      case 'network':
        return 'rede';
      case 'aborted':
        return 'outro';
      default:
        return 'outro';
    }
  }
}
