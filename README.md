# Idoso Digital IA

Projeto de **extensão universitária** para inclusão digital de idosos.

## Stack

| Camada | Tecnologia |
|--------|------------|
| Frontend | Angular 18 + Bootstrap + Design System acessível |
| Backend | ASP.NET Core 8 Web API |
| Banco | SQL Server LocalDB (local, sem Docker) |
| IA | **Ollama + `llama3.2:1b`** (local, **sem custo**) |

## Estrutura

```text
frontend/     Angular 18
backend/      ASP.NET Core 8
docs/         PRD, plano, decisão de IA, prompts
```

## Pré-requisitos

- Node.js **20+** (recomendado via nvm: `nvm use 20.18.0`)
- .NET SDK 8+
- SQL Server **LocalDB** (incluso com Visual Studio / SQL Server Express)
- Ollama (opcional na Fase 0 — há fallback mock)

## Subir o ambiente

### 1. Banco de dados (LocalDB)

```bash
sqllocaldb start MSSQLLocalDB
```

A connection string já aponta para `(localdb)\MSSQLLocalDB` e o banco `IdosoDigital` é criado na subida da API.

### 2. Backend

```bash
cd backend
dotnet run --project src/IdosoDigital.Api --urls http://localhost:5298
```

Swagger: http://localhost:5298/swagger

### 3. Frontend

Em um terminal com Node 20:

```bash
cd frontend
npm start
```

App: http://localhost:4200

### 4. IA gratuita (Ollama)

1. Instalar: https://ollama.com/download
2. Baixar o modelo leve:

```bash
ollama pull llama3.2:1b
```

Se o Ollama estiver desligado, a API usa **respostas mock educativas** automaticamente (`UseMockWhenUnavailable: true`).

## Documentação

- [Plano de ação](docs/PLANO-DE-ACAO.md)
- [Decisão de IA (sem custo)](docs/DECISAO-IA.md)
- [System prompt](docs/prompts/system-prompt.md)

## Fase 0 — status

- [x] IA gratuita definida (Ollama)
- [x] Monorepo criado
- [x] SQL Server LocalDB
- [x] Scaffold Angular + .NET
- [x] CORS, HTTPS, appsettings
- [x] System prompt
- [x] Design System acessível

## Fase 1 — status

- [x] Modelo de dados + migration
- [x] Cadastro / login JWT (BCrypt)
- [x] `DELETE /usuarios/me` (LGPD)
- [x] Telas Login / Cadastro / Início
- [x] Guard + interceptor JWT

## Fase 2 — status

- [x] Chat autenticado (`POST /api/chat`)
- [x] Histórico (`GET /api/conversas`)
- [x] Feedback Sim/Não (`POST /api/feedback`)
- [x] Tela `/chat` com bolhas, sugestões e histórico
