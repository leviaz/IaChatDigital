using IdosoDigital.Api.Ai;
using IdosoDigital.Api.Data;
using IdosoDigital.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace IdosoDigital.Api.Chat;

public sealed record CriarChatRequest(string? Titulo);
public sealed record ChatResumoResponse(
    Guid Id,
    string Titulo,
    DateTime DataCriacao,
    DateTime DataAtualizacao,
    string? UltimaPergunta);

public sealed record MensagemRequest(string Pergunta);
public sealed record MensagemResponse(
    Guid Id,
    Guid ChatId,
    string Pergunta,
    string Resposta,
    DateTime Data,
    string Provider,
    bool UsouMock,
    bool? FeedbackGostou);

public sealed record ChatDetalheResponse(
    Guid Id,
    string Titulo,
    DateTime DataCriacao,
    DateTime DataAtualizacao,
    IReadOnlyList<MensagemResponse> Mensagens);

public sealed record FeedbackRequest(Guid ConversaId, bool Gostou);
public sealed record FeedbackResponse(Guid Id, Guid ConversaId, bool Gostou, DateTime Data);

public interface IChatService
{
    Task<ChatResumoResponse> CriarChatAsync(Guid usuarioId, CriarChatRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatResumoResponse>> ListarChatsAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<ChatDetalheResponse?> ObterChatAsync(Guid usuarioId, Guid chatId, CancellationToken cancellationToken = default);
    Task<bool> ExcluirChatAsync(Guid usuarioId, Guid chatId, CancellationToken cancellationToken = default);
    Task<MensagemResponse> EnviarMensagemAsync(Guid usuarioId, Guid chatId, MensagemRequest request, CancellationToken cancellationToken = default);
    Task<FeedbackResponse> RegistrarFeedbackAsync(Guid usuarioId, FeedbackRequest request, CancellationToken cancellationToken = default);
}

public sealed class ChatService(
    AppDbContext db,
    IAiAssistantService ai) : IChatService
{
    public async Task<ChatResumoResponse> CriarChatAsync(
        Guid usuarioId,
        CriarChatRequest request,
        CancellationToken cancellationToken = default)
    {
        await GarantirUsuarioExisteAsync(usuarioId, cancellationToken);

        var agora = DateTime.UtcNow;
        var titulo = string.IsNullOrWhiteSpace(request.Titulo)
            ? "Novo chat"
            : Truncate(request.Titulo.Trim(), 120);

        var chat = new ChatSessao
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Titulo = titulo,
            DataCriacao = agora,
            DataAtualizacao = agora
        };

        db.Chats.Add(chat);
        await db.SaveChangesAsync(cancellationToken);

        return new ChatResumoResponse(chat.Id, chat.Titulo, chat.DataCriacao, chat.DataAtualizacao, null);
    }

    public async Task<IReadOnlyList<ChatResumoResponse>> ListarChatsAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        await GarantirUsuarioExisteAsync(usuarioId, cancellationToken);

        return await db.Chats
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.DataAtualizacao)
            .Select(x => new ChatResumoResponse(
                x.Id,
                x.Titulo,
                x.DataCriacao,
                x.DataAtualizacao,
                x.Mensagens
                    .OrderByDescending(m => m.Data)
                    .Select(m => m.Pergunta)
                    .FirstOrDefault()))
            .Take(50)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatDetalheResponse?> ObterChatAsync(
        Guid usuarioId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        var chat = await db.Chats
            .AsNoTracking()
            .Where(x => x.Id == chatId && x.UsuarioId == usuarioId)
            .Select(x => new
            {
                x.Id,
                x.Titulo,
                x.DataCriacao,
                x.DataAtualizacao,
                Mensagens = x.Mensagens
                    .OrderBy(m => m.Data)
                    .Select(m => new MensagemResponse(
                        m.Id,
                        m.ChatId,
                        m.Pergunta,
                        m.Resposta,
                        m.Data,
                        "Histórico",
                        false,
                        m.Feedbacks
                            .Where(f => f.UsuarioId == usuarioId)
                            .Select(f => (bool?)f.Gostou)
                            .FirstOrDefault()))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (chat is null)
        {
            return null;
        }

        return new ChatDetalheResponse(chat.Id, chat.Titulo, chat.DataCriacao, chat.DataAtualizacao, chat.Mensagens);
    }

    public async Task<bool> ExcluirChatAsync(
        Guid usuarioId,
        Guid chatId,
        CancellationToken cancellationToken = default)
    {
        var chat = await db.Chats.FirstOrDefaultAsync(x => x.Id == chatId && x.UsuarioId == usuarioId, cancellationToken);
        if (chat is null)
        {
            return false;
        }

        var mensagemIds = await db.Conversas
            .Where(x => x.ChatId == chatId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var feedbacks = await db.Feedbacks
            .Where(x => mensagemIds.Contains(x.ConversaId))
            .ToListAsync(cancellationToken);

        db.Feedbacks.RemoveRange(feedbacks);
        db.Chats.Remove(chat);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MensagemResponse> EnviarMensagemAsync(
        Guid usuarioId,
        Guid chatId,
        MensagemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Pergunta))
        {
            throw new ArgumentException("Digite sua pergunta.");
        }

        var pergunta = request.Pergunta.Trim();
        if (pergunta.Length > 2000)
        {
            throw new ArgumentException("A pergunta é muito longa. Tente resumir em poucas frases.");
        }

        var chat = await db.Chats.FirstOrDefaultAsync(x => x.Id == chatId && x.UsuarioId == usuarioId, cancellationToken);
        if (chat is null)
        {
            throw new KeyNotFoundException("Chat não encontrado.");
        }

        var aiResult = await ai.AskAsync(pergunta, cancellationToken);
        var agora = DateTime.UtcNow;

        var mensagem = new Conversa
        {
            Id = Guid.NewGuid(),
            ChatId = chat.Id,
            UsuarioId = usuarioId,
            Pergunta = pergunta,
            Resposta = Truncate(aiResult.Resposta, 8000),
            Data = agora
        };

        if (chat.Titulo == "Novo chat")
        {
            chat.Titulo = Truncate(pergunta, 60);
        }

        chat.DataAtualizacao = agora;
        db.Conversas.Add(mensagem);
        await db.SaveChangesAsync(cancellationToken);

        return new MensagemResponse(
            mensagem.Id,
            mensagem.ChatId,
            mensagem.Pergunta,
            mensagem.Resposta,
            mensagem.Data,
            aiResult.Provider,
            aiResult.UsouMock,
            null);
    }

    public async Task<FeedbackResponse> RegistrarFeedbackAsync(
        Guid usuarioId,
        FeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        var conversa = await db.Conversas
            .FirstOrDefaultAsync(x => x.Id == request.ConversaId && x.UsuarioId == usuarioId, cancellationToken);

        if (conversa is null)
        {
            throw new KeyNotFoundException("Mensagem não encontrada.");
        }

        var existente = await db.Feedbacks
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.ConversaId == request.ConversaId, cancellationToken);

        if (existente is not null)
        {
            existente.Gostou = request.Gostou;
            existente.Data = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return new FeedbackResponse(existente.Id, existente.ConversaId, existente.Gostou, existente.Data);
        }

        var feedback = new Feedback
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            ConversaId = request.ConversaId,
            Gostou = request.Gostou,
            Data = DateTime.UtcNow
        };

        db.Feedbacks.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);

        return new FeedbackResponse(feedback.Id, feedback.ConversaId, feedback.Gostou, feedback.Data);
    }

    private async Task GarantirUsuarioExisteAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var existe = await db.Usuarios.AnyAsync(x => x.Id == usuarioId, cancellationToken);
        if (!existe)
        {
            throw new UnauthorizedAccessException("Sua sessão expirou ou a conta não existe mais. Entre novamente.");
        }
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
