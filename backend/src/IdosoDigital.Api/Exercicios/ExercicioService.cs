using System.Text.Json;
using IdosoDigital.Api.Ai;
using IdosoDigital.Api.Data;
using IdosoDigital.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace IdosoDigital.Api.Exercicios;

public sealed record GerarExercicioRequest(string? Categoria, Guid? ConteudoId);
public sealed record ResponderExercicioRequest(string Alternativa);

public sealed record ExercicioPublicoResponse(
    Guid Id,
    string Pergunta,
    IReadOnlyList<string> Alternativas,
    string Categoria,
    int NivelDificuldade,
    string Provider,
    bool UsouMock);

public sealed record RespostaExercicioResponse(
    Guid ResultadoId,
    bool Acertou,
    string RespostaCorreta,
    string Explicacao,
    int AcertosTotais,
    int NivelAtual);

public sealed record PontuacaoResponse(
    int Acertos,
    int Erros,
    int Total,
    int NivelAtual,
    string Categoria);

public interface IExercicioService
{
    Task<ExercicioPublicoResponse> GerarAsync(Guid usuarioId, GerarExercicioRequest request, CancellationToken cancellationToken = default);
    Task<RespostaExercicioResponse> ResponderAsync(Guid usuarioId, Guid exercicioId, ResponderExercicioRequest request, CancellationToken cancellationToken = default);
    Task<PontuacaoResponse> ObterPontuacaoAsync(Guid usuarioId, string? categoria, CancellationToken cancellationToken = default);
}

public sealed class ExercicioService(
    AppDbContext db,
    IAiAssistantService ai,
    ILogger<ExercicioService> logger) : IExercicioService
{
    private const string PromptExercicio = """
        Você cria exercícios educativos para idosos sobre inclusão digital.
        Responda APENAS com JSON válido, sem markdown, neste formato:
        {"pergunta":"...","alternativas":["A) ...","B) ...","C) ..."],"respostaCorreta":"B","explicacao":"..."}
        Regras: português simples, 3 alternativas, uma correta, explicação curta e segura.
        """;

    public async Task<ExercicioPublicoResponse> GerarAsync(
        Guid usuarioId,
        GerarExercicioRequest request,
        CancellationToken cancellationToken = default)
    {
        var categoria = await ResolverCategoriaAsync(request, cancellationToken);
        var nivel = await CalcularNivelAsync(usuarioId, categoria, cancellationToken);

        var gerado = await TentarGerarComIaAsync(categoria, nivel, cancellationToken)
            ?? MockBanco.Sortear(categoria, nivel);

        var exercicio = new Exercicio
        {
            Id = Guid.NewGuid(),
            Pergunta = gerado.Pergunta,
            AlternativasJson = JsonSerializer.Serialize(gerado.Alternativas),
            RespostaCorreta = gerado.RespostaCorreta.Trim().ToUpperInvariant()[..1],
            Explicacao = gerado.Explicacao,
            Categoria = categoria,
            NivelDificuldade = nivel
        };

        db.Exercicios.Add(exercicio);
        await db.SaveChangesAsync(cancellationToken);

        return new ExercicioPublicoResponse(
            exercicio.Id,
            exercicio.Pergunta,
            gerado.Alternativas,
            exercicio.Categoria,
            exercicio.NivelDificuldade,
            gerado.Provider,
            gerado.UsouMock);
    }

    public async Task<RespostaExercicioResponse> ResponderAsync(
        Guid usuarioId,
        Guid exercicioId,
        ResponderExercicioRequest request,
        CancellationToken cancellationToken = default)
    {
        var exercicio = await db.Exercicios.FirstOrDefaultAsync(x => x.Id == exercicioId, cancellationToken)
            ?? throw new KeyNotFoundException("Exercício não encontrado.");

        var alternativa = (request.Alternativa ?? string.Empty).Trim().ToUpperInvariant();
        if (alternativa.Length == 0)
        {
            throw new ArgumentException("Escolha uma alternativa (A, B ou C).");
        }

        var letra = alternativa[..1];
        var acertou = letra == exercicio.RespostaCorreta.Trim().ToUpperInvariant()[..1];

        var resultado = new Resultado
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            ExercicioId = exercicio.Id,
            Acertou = acertou,
            Data = DateTime.UtcNow
        };

        db.Resultados.Add(resultado);
        await db.SaveChangesAsync(cancellationToken);

        var acertos = await db.Resultados.CountAsync(
            x => x.UsuarioId == usuarioId && x.Exercicio.Categoria == exercicio.Categoria && x.Acertou,
            cancellationToken);

        var nivel = await CalcularNivelAsync(usuarioId, exercicio.Categoria, cancellationToken);

        return new RespostaExercicioResponse(
            resultado.Id,
            acertou,
            exercicio.RespostaCorreta,
            exercicio.Explicacao,
            acertos,
            nivel);
    }

    public async Task<PontuacaoResponse> ObterPontuacaoAsync(
        Guid usuarioId,
        string? categoria,
        CancellationToken cancellationToken = default)
    {
        var cat = NormalizarCategoria(categoria) ?? "geral";
        var query = db.Resultados.AsNoTracking().Where(x => x.UsuarioId == usuarioId);

        if (cat != "geral")
        {
            query = query.Where(x => x.Exercicio.Categoria == cat);
        }

        var acertos = await query.CountAsync(x => x.Acertou, cancellationToken);
        var total = await query.CountAsync(cancellationToken);
        var erros = total - acertos;
        var nivel = await CalcularNivelAsync(usuarioId, cat == "geral" ? "pix" : cat, cancellationToken);

        return new PontuacaoResponse(acertos, erros, total, nivel, cat);
    }

    private async Task<string> ResolverCategoriaAsync(GerarExercicioRequest request, CancellationToken cancellationToken)
    {
        if (request.ConteudoId is Guid conteudoId)
        {
            var slug = await db.Conteudos
                .AsNoTracking()
                .Where(x => x.Id == conteudoId)
                .Select(x => x.Categoria.Slug)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(slug))
            {
                return slug;
            }
        }

        return NormalizarCategoria(request.Categoria) ?? "golpes";
    }

    private async Task<int> CalcularNivelAsync(Guid usuarioId, string categoria, CancellationToken cancellationToken)
    {
        var recentes = await db.Resultados
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.Exercicio.Categoria == categoria)
            .OrderByDescending(x => x.Data)
            .Take(5)
            .Select(x => x.Acertou)
            .ToListAsync(cancellationToken);

        var sequencia = 0;
        foreach (var acertou in recentes)
        {
            if (!acertou)
            {
                break;
            }

            sequencia++;
        }

        // Sobe dificuldade após 3 acertos seguidos; máximo nível 3.
        if (sequencia >= 5)
        {
            return 3;
        }

        if (sequencia >= 3)
        {
            return 2;
        }

        return 1;
    }

    private async Task<ExercicioGerado?> TentarGerarComIaAsync(string categoria, int nivel, CancellationToken cancellationToken)
    {
        var userPrompt = $"""
            Crie 1 exercício de nível {nivel} (1 fácil, 2 médio, 3 difícil) sobre a categoria "{categoria}".
            Tema para idosos: segurança digital, PIX, WhatsApp, golpes, bancos ou SUS.
            """;

        var aiResult = await ai.AskWithSystemAsync(PromptExercicio, userPrompt, cancellationToken);
        if (aiResult.UsouMock || string.IsNullOrWhiteSpace(aiResult.Resposta))
        {
            return null;
        }

        try
        {
            var json = ExtrairJson(aiResult.Resposta);
            var dto = JsonSerializer.Deserialize<ExercicioIaDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dto is null
                || string.IsNullOrWhiteSpace(dto.Pergunta)
                || dto.Alternativas is null
                || dto.Alternativas.Count < 3
                || string.IsNullOrWhiteSpace(dto.RespostaCorreta))
            {
                return null;
            }

            return new ExercicioGerado(
                dto.Pergunta.Trim(),
                dto.Alternativas.Take(3).Select(a => a.Trim()).ToList(),
                dto.RespostaCorreta.Trim().ToUpperInvariant()[..1],
                string.IsNullOrWhiteSpace(dto.Explicacao)
                    ? "Revise a resposta correta e peça ajuda se ainda tiver dúvida."
                    : dto.Explicacao.Trim(),
                aiResult.Provider,
                false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao interpretar JSON do exercício. Usando banco mock.");
            return null;
        }
    }

    private static string ExtrairJson(string texto)
    {
        var inicio = texto.IndexOf('{');
        var fim = texto.LastIndexOf('}');
        if (inicio < 0 || fim <= inicio)
        {
            return texto;
        }

        return texto[inicio..(fim + 1)];
    }

    private static string? NormalizarCategoria(string? categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria))
        {
            return null;
        }

        return categoria.Trim().ToLowerInvariant();
    }

    private sealed record ExercicioIaDto(
        string Pergunta,
        List<string> Alternativas,
        string RespostaCorreta,
        string? Explicacao);

    private sealed record ExercicioGerado(
        string Pergunta,
        IReadOnlyList<string> Alternativas,
        string RespostaCorreta,
        string Explicacao,
        string Provider,
        bool UsouMock);

    private static class MockBanco
    {
        private static readonly Random Rng = new();

        private static readonly Dictionary<string, ExercicioGerado[]> Itens = new()
        {
            ["pix"] =
            [
                new("O que você deve conferir antes de confirmar um PIX?",
                    ["A) Só o valor", "B) Nome da pessoa e o valor", "C) A cor do aplicativo"],
                    "B",
                    "Sempre confira o nome e o valor com calma antes de confirmar.",
                    "Mock", true),
                new("Alguém pediu PIX urgente por WhatsApp. O que fazer?",
                    ["A) Enviar na hora", "B) Pedir a senha do banco", "C) Confirmar por ligação com a pessoa conhecida"],
                    "C",
                    "Golpistas se passam por familiares. Confirme por outro caminho seguro.",
                    "Mock", true)
            ],
            ["golpes"] =
            [
                new("O que você faria se recebesse uma ligação pedindo a senha do banco?",
                    ["A) Informar a senha", "B) Desligar e conferir no aplicativo oficial", "C) Enviar um PIX"],
                    "B",
                    "Banco de verdade não pede senha por telefone.",
                    "Mock", true),
                new("Você recebeu: \"Sua conta será bloqueada. Clique aqui.\" Qual ação é mais segura?",
                    ["A) Clicar no link", "B) Ignorar e abrir o app oficial do banco", "C) Responder com seus dados"],
                    "B",
                    "Links urgentes costumam ser golpe. Use só o aplicativo oficial.",
                    "Mock", true)
            ],
            ["whatsapp"] =
            [
                new("Como bloquear um número no WhatsApp?",
                    ["A) Apagar só a última mensagem", "B) Abrir o contato e escolher Bloquear", "C) Desligar o Wi-Fi"],
                    "B",
                    "Em dados do contato existe a opção Bloquear.",
                    "Mock", true)
            ],
            ["sus"] =
            [
                new("Se não conseguir marcar consulta online no SUS, o que pode fazer?",
                    ["A) Desistir", "B) Pedir ajuda no posto de saúde", "C) Enviar senha por SMS"],
                    "B",
                    "O posto de saúde pode ajudar no agendamento.",
                    "Mock", true)
            ],
            ["bancos"] =
            [
                new("Onde baixar o aplicativo do banco com mais segurança?",
                    ["A) Em qualquer site", "B) Na loja oficial do celular", "C) Por link de SMS"],
                    "B",
                    "Baixe apenas na Play Store ou App Store.",
                    "Mock", true)
            ]
        };

        public static ExercicioGerado Sortear(string categoria, int nivel)
        {
            if (!Itens.TryGetValue(categoria, out var lista) || lista.Length == 0)
            {
                lista = Itens["golpes"];
            }

            var item = lista[Rng.Next(lista.Length)];
            if (nivel >= 2)
            {
                return item with
                {
                    Pergunta = item.Pergunta + " (Pense com calma antes de responder.)"
                };
            }

            return item;
        }
    }
}
