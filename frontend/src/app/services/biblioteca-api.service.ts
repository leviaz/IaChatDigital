import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Categoria {
  id: string;
  nome: string;
  slug: string;
  descricao: string;
  ordem: number;
  totalConteudos: number;
}

export interface ConteudoResumo {
  id: string;
  categoriaId: string;
  categoriaNome: string;
  categoriaSlug: string;
  titulo: string;
  tipo: string;
  ordem: number;
}

export interface ConteudoDetalhe extends ConteudoResumo {
  corpo: string;
  urlMidia: string | null;
}

@Injectable({ providedIn: 'root' })
export class BibliotecaApiService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  listarCategorias(): Observable<Categoria[]> {
    return this.http.get<Categoria[]>(`${this.api}/categorias`);
  }

  obterCategoria(slug: string): Observable<Categoria> {
    return this.http.get<Categoria>(`${this.api}/categorias/${slug}`);
  }

  listarConteudos(categoriaSlug?: string): Observable<ConteudoResumo[]> {
    let params = new HttpParams();
    if (categoriaSlug) {
      params = params.set('categoria', categoriaSlug);
    }

    return this.http.get<ConteudoResumo[]>(`${this.api}/conteudos`, { params });
  }

  obterConteudo(id: string): Observable<ConteudoDetalhe> {
    return this.http.get<ConteudoDetalhe>(`${this.api}/conteudos/${id}`);
  }
}
