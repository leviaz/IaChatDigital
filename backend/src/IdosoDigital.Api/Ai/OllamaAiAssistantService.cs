using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace IdosoDigital.Api.Ai;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string Provider { get; set; } = "Ollama";
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2:1b";
    public bool UseMockWhenUnavailable { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 5;
}

public interface IAiAssistantService
{
    Task<AiChatResult> AskAsync(string pergunta, CancellationToken cancellationToken = default);
    Task<AiChatResult> AskWithSystemAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}

public sealed record AiChatResult(string Resposta, string Provider, bool UsouMock);

public sealed class OllamaAiAssistantService : IAiAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<OllamaAiAssistantService> _logger;
    private readonly string _systemPrompt;

    public OllamaAiAssistantService(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<OllamaAiAssistantService> logger,
        IWebHostEnvironment environment)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        var promptPath = Path.Combine(environment.ContentRootPath, "Prompts", "system-prompt.md");
        if (!File.Exists(promptPath))
        {
            promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "system-prompt.md");
        }

        _systemPrompt = File.Exists(promptPath)
            ? File.ReadAllText(promptPath)
            : FallbackSystemPrompt;
    }

    public async Task<AiChatResult> AskAsync(string pergunta, CancellationToken cancellationToken = default)
    {
        try
        {
            return await EnviarAsync(_systemPrompt, pergunta, cancellationToken);
        }
        catch (Exception ex) when (_options.UseMockWhenUnavailable)
        {
            _logger.LogWarning(ex, "Ollama indisponível. Usando resposta mock educativa.");
            return new AiChatResult(MockResposta(pergunta), "Mock", true);
        }
    }

    public async Task<AiChatResult> AskWithSystemAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await EnviarAsync(systemPrompt, userMessage, cancellationToken);
        }
        catch (Exception ex) when (_options.UseMockWhenUnavailable)
        {
            _logger.LogWarning(ex, "Ollama indisponível para geração estruturada.");
            return new AiChatResult(string.Empty, "Mock", true);
        }
    }

    private async Task<AiChatResult> EnviarAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var request = new OllamaChatRequest(
            _options.Model,
            [
                new OllamaMessage("system", systemPrompt),
                new OllamaMessage("user", userMessage)
            ],
            Stream: false);

        using var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync("/api/chat", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<OllamaChatResponse>(stream, cancellationToken: cancellationToken);

        var resposta = payload?.Message?.Content?.Trim();
        if (string.IsNullOrWhiteSpace(resposta))
        {
            throw new InvalidOperationException("Ollama retornou resposta vazia.");
        }

        return new AiChatResult(resposta, "Ollama", false);
    }

    private static string MockResposta(string pergunta)
    {
        var texto = pergunta.ToLowerInvariant();

        if (texto.Contains("golpe") || texto.Contains("sms") || texto.Contains("senha"))
        {
            return """
                Atenção: isso pode ser golpe.

                1. Não clique em links suspeitos.
                2. Não informe senha, código SMS ou token.
                3. Feche a mensagem.
                4. Abra o aplicativo oficial do banco ou do serviço.
                5. Se ainda tiver dúvida, peça ajuda a um familiar de confiança.

                Bancos e órgãos públicos não pedem senha por mensagem ou telefone.
                """;
        }

        if (texto.Contains("pix"))
        {
            return """
                Para fazer um PIX, use o aplicativo do seu banco.

                1. Abra o app do banco.
                2. Toque em PIX.
                3. Escolha Transferir ou Pagar.
                4. Digite a chave ou leia o QR Code.
                5. Confira o nome e o valor com calma.
                6. Confirme com a senha do aplicativo.

                Nunca faça PIX porque alguém pediu por mensagem ou telefone.
                """;
        }

        if (texto.Contains("whatsapp") || texto.Contains("bloquear"))
        {
            return """
                Para bloquear um número no WhatsApp:

                1. Abra a conversa.
                2. Toque no nome da pessoa no topo.
                3. Role até Bloquear.
                4. Confirme o bloqueio.

                Depois disso, a pessoa não consegue mais te enviar mensagens.
                """;
        }

        if (texto.Contains("sus") || texto.Contains("consulta"))
        {
            return """
                Para marcar consulta no SUS, o caminho mais comum é:

                1. Abra o app ou site Conecte SUS / saúde da sua cidade.
                2. Faça login com a conta Gov.br, se pedir.
                3. Procure a opção de consultas ou agendamento.
                4. Escolha a especialidade e a data disponível.
                5. Confirme e anote o dia e o horário.

                Se preferir, também pode pedir ajuda no posto de saúde mais próximo.
                """;
        }

        return """
            Posso te ajudar com PIX, WhatsApp, bancos, golpes, internet e SUS.

            Pergunte com suas palavras, por exemplo:
            - Como faço um PIX?
            - Esse SMS é golpe?
            - Como bloquear um número no WhatsApp?

            Respondo com passos simples e curtos.
            """;
    }

    private const string FallbackSystemPrompt =
        "Você é o assistente do Idoso Digital IA. Responda em português do Brasil, com frases curtas e passos numerados. Nunca peça senha ou dados bancários.";

    private sealed record OllamaChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<OllamaMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }
    }
}
