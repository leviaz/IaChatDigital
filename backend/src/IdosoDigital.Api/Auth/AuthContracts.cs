namespace IdosoDigital.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "IdosoDigitalIA";
    public string Audience { get; set; } = "IdosoDigitalIA";
    public string Key { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 12;
}

public sealed record CadastroRequest(string Nome, string Email, string Senha, bool ConsentimentoLgpd);
public sealed record LoginRequest(string Email, string Senha);
public sealed record AuthResponse(Guid Id, string Nome, string Email, string Token);
public sealed record UsuarioMeResponse(Guid Id, string Nome, string Email, DateTime DataCadastro);
