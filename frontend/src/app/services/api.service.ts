import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface HealthResponse {
  status: string;
  projeto: string;
  database: { connected: boolean; error?: string | null };
  ai: { provider: string; mockFallback: boolean };
}

export interface ChatSmokeResponse {
  resposta: string;
  provider: string;
  usouMock: boolean;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  getHealth(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(`${this.baseUrl}/health`);
  }

  chatSmoke(pergunta: string): Observable<ChatSmokeResponse> {
    return this.http.post<ChatSmokeResponse>(`${this.baseUrl}/health/chat-smoke`, { pergunta });
  }
}
