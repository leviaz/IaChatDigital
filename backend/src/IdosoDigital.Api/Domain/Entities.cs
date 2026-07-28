namespace IdosoDigital.Api.Domain;

public sealed class Usuario
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public DateTime DataCadastro { get; set; }
    public bool ConsentimentoLgpd { get; set; }

    public ICollection<ChatSessao> Chats { get; set; } = [];
    public ICollection<Conversa> Conversas { get; set; } = [];
    public ICollection<Resultado> Resultados { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];
}

/// <summary>Sessão de chat (thread) com várias perguntas/respostas.</summary>
public sealed class ChatSessao
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string Titulo { get; set; } = "Novo chat";
    public DateTime DataCriacao { get; set; }
    public DateTime DataAtualizacao { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public ICollection<Conversa> Mensagens { get; set; } = [];
}

public sealed class Conversa
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Pergunta { get; set; } = string.Empty;
    public string Resposta { get; set; } = string.Empty;
    public DateTime Data { get; set; }

    public ChatSessao Chat { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public ICollection<Feedback> Feedbacks { get; set; } = [];
}

public sealed class Exercicio
{
    public Guid Id { get; set; }
    public string Pergunta { get; set; } = string.Empty;
    /// <summary>Alternativas em JSON, ex.: ["A) ...","B) ...","C) ..."].</summary>
    public string AlternativasJson { get; set; } = "[]";
    public string RespostaCorreta { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public int NivelDificuldade { get; set; } = 1;

    public ICollection<Resultado> Resultados { get; set; } = [];
}

public sealed class Resultado
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid ExercicioId { get; set; }
    public bool Acertou { get; set; }
    public DateTime Data { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Exercicio Exercicio { get; set; } = null!;
}

public sealed class Feedback
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid ConversaId { get; set; }
    public bool Gostou { get; set; }
    public DateTime Data { get; set; }

    public Usuario Usuario { get; set; } = null!;
    public Conversa Conversa { get; set; } = null!;
}
