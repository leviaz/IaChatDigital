# Plano de Ação – Idoso Digital IA

Plano executável derivado do PRD da plataforma **Idoso Digital IA**.

**Stack:** Angular 18 · ASP.NET Core 8 Web API · SQL Server · **Ollama (`llama3.2:1b`, gratuito/local)** · JWT

> Projeto de **extensão universitária**: a IA escolhida é local e **sem custo**. Sem OpenAI paga.
> Detalhes em [`docs/DECISAO-IA.md`](DECISAO-IA.md).

---

## Princípios de execução

1. **MVP primeiro** — chat + autenticação + histórico + acessibilidade; depois biblioteca, exercícios e simulações.
2. **Acessibilidade desde o dia 1** — letras grandes, contraste alto e botões amplos (não deixar para o final).
3. **IA atrás da API** — o frontend nunca chama o Ollama diretamente.
4. **LGPD desde o desenho** — consentimento, retenção e exclusão de conta.
5. **Custo zero na IA** — Ollama local; fallback mock para aulas/demos sem GPU.

---

## Fase 0 — Fundação (1–2 semanas)

| # | Ação | Entrega | Status |
|---|------|---------|--------|
| 0.1 | Definir provedor de IA **gratuito** (Ollama + `llama3.2:1b`) | [`DECISAO-IA.md`](DECISAO-IA.md) | Feito |
| 0.2 | Criar monorepo (`frontend/`, `backend/`, `docs/`) | Estrutura Git | Feito |
| 0.3 | Subir SQL Server **LocalDB** (sem Docker) | `(localdb)\MSSQLLocalDB` | Feito |
| 0.4 | Scaffold Angular 18 + ASP.NET Core 8 Web API | Apps no repositório | Feito |
| 0.5 | Configurar HTTPS, CORS e variáveis de ambiente | `Program.cs` + `appsettings` | Feito |
| 0.6 | Criar system prompt da IA (linguagem simples, passos curtos) | `docs/prompts/system-prompt.md` | Feito |
| 0.7 | Definir Design System acessível (tipografia ≥18px, contraste WCAG AA, botões ≥44px) | `frontend/src/styles/_tokens.scss` | Feito |

**Critério de saída:** `ng serve` + API Swagger ok; banco conectado (ou health reportando status).

**Como validar a Fase 0**

```bash
sqllocaldb start MSSQLLocalDB
cd backend && dotnet run --project src/IdosoDigital.Api --urls http://localhost:5298
cd frontend && npm start
```

Opcional: `ollama pull llama3.2:1b` — sem isso, a API usa mock educativo.

---

## Fase 1 — Autenticação e modelo de dados (1–2 semanas)

### Backend

- [x] Entidades: `Usuario`, `Conversa`, `Exercicio`, `Resultado`, `Feedback`
- [x] Migrations com EF Core (`Data/Migrations`)
- [x] **RF01 / RF02:** cadastro, login, BCrypt, JWT
- [x] Endpoint `DELETE /usuarios/me` (LGPD)

### Frontend

- [x] Telas de Login e Cadastro
- [x] Guard de rotas autenticadas
- [x] Interceptor JWT
- [x] Layout base com navegação mínima

**Critério de saída:** usuário cadastra, faz login e acessa a área logada. ✅

**Endpoints**

| Método | Rota | Auth |
|--------|------|------|
| POST | `/api/auth/cadastro` | Não |
| POST | `/api/auth/login` | Não |
| GET | `/api/auth/me` | Sim |
| DELETE | `/api/usuarios/me` | Sim |

---

## Fase 2 — Chat com IA + histórico + feedback (2–3 semanas) — MVP core

### Backend

- [x] `POST /api/chat` → IA → salva `Conversa`
- [x] Timeout de IA em 5s (fallback mock se Ollama não responder a tempo)
- [x] `GET /api/conversas` (histórico)
- [x] `POST /api/feedback` (👍/👎) — **RF10**

### Frontend

- [x] Tela de chat com bolhas legíveis e sugestões
- [x] Histórico lateral/lista
- [x] Feedback pós-resposta
- [x] Estados de carregamento e erro amigável

### IA

- [x] System prompt + guardrails (já na Fase 0)
- [x] Mock educativo quando Ollama indisponível

**Critério de saída:** **RF03–RF05** + **RF10**. ✅

**Endpoints**

| Método | Rota | Auth |
|--------|------|------|
| POST | `/api/chat` | Sim |
| GET | `/api/conversas` | Sim |
| GET | `/api/conversas/{id}` | Sim |
| POST | `/api/feedback` | Sim |

---

## Fase 3 — Biblioteca de conteúdo (1–2 semanas)

### Dados

- [x] Seed das 8 categorias do PRD
- [x] Modelo `Conteudo`: título, tipo (artigo/vídeo/imagem/FAQ), categoria, corpo/URL

### Backend

- [x] `GET /api/categorias`
- [x] `GET /api/categorias/{slug}`
- [x] `GET /api/conteudos?categoria=`
- [x] `GET /api/conteudos/{id}`

### Frontend

- [x] `/biblioteca` com cards grandes
- [x] `/biblioteca/:slug` lista de conteúdos
- [x] `/conteudo/:id` + botão “Perguntar à IA sobre isso”

**Critério de saída:** **RF08**. ✅ (PIX, Golpes, WhatsApp e SUS com 3 itens cada)

---

## Fase 4 — Exercícios inteligentes (2 semanas)

### Backend

- [x] Geração via IA (JSON) com fallback mock por categoria
- [x] Persistência `Exercicio` + `Resultado` (**RF06**, **RF07**)
- [x] Dificuldade sobe após 3/5 acertos seguidos (níveis 1–3)

### Frontend

- [x] `/praticar` com alternativas A/B/C grandes
- [x] Feedback imediato + pontuação
- [x] “Quer praticar?” no conteúdo e no chat

**Critério de saída:** exercício gerado e pontuação salva. ✅

**Endpoints**

| Método | Rota | Auth |
|--------|------|------|
| POST | `/api/exercicios/gerar` | Sim |
| POST | `/api/exercicios/{id}/responder` | Sim |
| GET | `/api/exercicios/pontuacao` | Sim |

---

## Fase 5 — Simulações de golpes (1–2 semanas)

### Backend

- Catálogo de cenários (seed + geração opcional via IA)
- `POST /simulacoes/{id}/responder` → resultado + explicação

### Frontend

- Cenário fictício + 3 ações: Confiar / Ignorar / Verificar
- Explicação clara ao final

**Critério de saída:** **RF09** e critério de aceitação de simulações atendido.

---

## Fase 6 — Qualidade, acessibilidade e harden (1–2 semanas)

| Ação | Motivo |
|------|--------|
| Testes manuais em celular, tablet e desktop | Compatibilidade (RNF) |
| Auditoria de contraste, fonte e teclado | Acessibilidade (RNF) |
| Rate limit no chat | Custo e abuso |
| Logs sem PII sensível | LGPD |
| Política de privacidade + consentimento no cadastro | LGPD |
| Smoke test de latência do chat | Tempo médio &lt; 5s |
| Deploy (ex.: Azure App Service + SQL + Angular static) | Disponibilidade |

**Critério de saída:** checklist do §13 do PRD atendido.

---

## Ordem de requisitos funcionais (prioridade)

| Prioridade | Requisitos |
|------------|------------|
| **P0 (MVP)** | RF01 → RF02 → RF03 → RF04 → RF05 → RF10 |
| **P1** | RF08 |
| **P2** | RF06 → RF07 |
| **P3** | RF09 |

---

## Estrutura sugerida do repositório

```text
/frontend          # Angular 18
/backend           # ASP.NET Core 8
/docs              # PRD, prompts, ADRs, este plano
```

---

## Papéis e ritmo

| Papel | Foco |
|-------|------|
| Backend | API, JWT, EF Core, integração IA |
| Frontend | UI acessível, chat, fluxos |
| Conteúdo | Artigos, FAQs e cenários de golpe |
| Product / UX | Validação com idosos reais a cada fase |

### Estimativa

- Sprints sugeridos: 1 semana nas fases 0–1; 2 semanas nas fases 2–4; 1–2 semanas nas fases 5–6
- **Total estimado:** cerca de **10–14 semanas** para MVP completo do PRD (1–2 pessoas em tempo integral)

---

## Riscos e mitigações

| Risco | Mitigação |
|-------|-----------|
| IA inventar orientações perigosas | Prompt rígido + disclaimer + categorias fechadas |
| Custo de API paga | **Ollama local gratuito** + mock educativo em aula |
| Idosos abandonam UI complexa | Testar com 3–5 usuários reais após a Fase 2 |
| LGPD | Minimizar dados; não logar conteúdo sensível; permitir exclusão |
| LocalDB indisponível no laboratório | Instalar SQL Server Express/LocalDB; health da API reporta status sem derrubar o app |

---

## Evoluções (fora do MVP)

Itens a tratar **somente depois** dos critérios de aceitação do PRD:

- Assistente por voz
- Integração com WhatsApp
- Integração com Alexa ou Google Assistente
- Reconhecimento de imagens para identificar mensagens suspeitas
- Tradução para Libras
- Leitura em voz alta das respostas
- Painel administrativo com métricas de uso e desempenho

---

## Critérios de aceitação (referência do PRD)

O sistema será considerado concluído quando:

- [ ] O chatbot responder perguntas em linguagem simples
- [ ] Exercícios forem gerados automaticamente
- [ ] O histórico for armazenado corretamente
- [ ] As simulações de golpes estiverem funcionando
- [ ] O sistema funcionar em dispositivos móveis
- [ ] O tempo médio de resposta permanecer abaixo de 5 segundos

---

## Próximo passo imediato

Iniciar a **Fase 0**:

1. Scaffold Angular 18 + ASP.NET Core 8
2. SQL Server
3. Escolha da API de IA
4. Tema acessível (letras grandes e alto contraste)
