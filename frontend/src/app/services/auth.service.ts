import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AuthResponse {
  id: string;
  nome: string;
  email: string;
  token: string;
}

export interface UsuarioMe {
  id: string;
  nome: string;
  email: string;
  dataCadastro: string;
}

export interface CadastroPayload {
  nome: string;
  email: string;
  senha: string;
  consentimentoLgpd: boolean;
}

export interface LoginPayload {
  email: string;
  senha: string;
}

const TOKEN_KEY = 'id_token';
const USER_KEY = 'id_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  cadastrar(payload: CadastroPayload): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/cadastro`, payload).pipe(
      tap((res) => this.persistSession(res))
    );
  }

  login(payload: LoginPayload): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, payload).pipe(
      tap((res) => this.persistSession(res))
    );
  }

  me(): Observable<UsuarioMe> {
    return this.http.get<UsuarioMe>(`${this.baseUrl}/me`);
  }

  excluirConta(): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/usuarios/me`).pipe(
      tap(() => this.logout(false))
    );
  }

  logout(navigate = true): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    if (navigate) {
      void this.router.navigate(['/login']);
    }
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  getUsuario(): Pick<AuthResponse, 'id' | 'nome' | 'email'> | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as Pick<AuthResponse, 'id' | 'nome' | 'email'>;
    } catch {
      return null;
    }
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  private persistSession(res: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(
      USER_KEY,
      JSON.stringify({ id: res.id, nome: res.nome, email: res.email })
    );
  }
}
