# Decisão de IA — Extensão Universitária

## Contexto

O **Idoso Digital IA** é um projeto de **extensão universitária**. Por isso a IA deve ser:

- **sem custo** (sem API paga);
- **simples** de instalar e manter em laboratório;
- **adequada** a respostas curtas e em português.

## Decisão

| Item | Escolha |
|------|---------|
| Provedor | **Ollama** (local) |
| Modelo recomendado | **`llama3.2:1b`** (leve) ou **`phi3:mini`** |
| Custo | **R$ 0** — roda no PC do laboratório/aluno |
| Endpoint | `http://localhost:11434` |
| Integração | Backend .NET chama a API HTTP do Ollama |

## Por que não OpenAI / APIs pagas?

- Extensão de faculdade: orçamento zero ou mínimo.
- OpenAI e equivalentes exigem cartão e cobrança por token.
- Ollama permite demos offline em oficinas com idosos.

## Alternativa leve (se a máquina for fraca)

Se `llama3.2:1b` ainda for pesado, usar:

```bash
ollama pull tinydolphin
```

Ou manter o **modo mock** do backend (respostas educativas fixas) só para apresentar a UI sem GPU/RAM.

## Instalação rápida (Windows)

1. Baixar: https://ollama.com/download
2. Instalar e abrir o terminal:

```bash
ollama pull llama3.2:1b
ollama run llama3.2:1b
```

3. Conferir se a API responde:

```bash
curl http://localhost:11434/api/tags
```

## Configuração no backend

Arquivo `backend/src/IdosoDigital.Api/appsettings.json`:

```json
"Ai": {
  "Provider": "Ollama",
  "BaseUrl": "http://localhost:11434",
  "Model": "llama3.2:1b",
  "UseMockWhenUnavailable": true
}
```

`UseMockWhenUnavailable: true` garante que o projeto continue funcionando em aula mesmo se o Ollama estiver desligado.

## Status

- [x] Decisão documentada (Fase 0.1)
- [x] Ollama instalado na máquina de desenvolvimento
- [x] Modelo baixado (`llama3.2:1b`)
- [x] API respondendo com `provider=Ollama` (não mock)
