using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdosoDigital.Api.Data;
using IdosoDigital.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IdosoDigital.Api.Auth;

public interface IAuthService
{
    Task<AuthResponse> CadastrarAsync(CadastroRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioMeResponse?> ObterMeAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<bool> ExcluirContaAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    AppDbContext db,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse> CadastrarAsync(CadastroRequest request, CancellationToken cancellationToken = default)
    {
        ValidarCadastro(request);

        var email = request.Email.Trim().ToLowerInvariant();
        var existe = await db.Usuarios.AnyAsync(x => x.Email == email, cancellationToken);
        if (existe)
        {
            throw new InvalidOperationException("Já existe uma conta com este e-mail.");
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Email = email,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha),
            DataCadastro = DateTime.UtcNow,
            ConsentimentoLgpd = request.ConsentimentoLgpd
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse(usuario.Id, usuario.Nome, usuario.Email, GerarToken(usuario));
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Senha))
        {
            return null;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.SenhaHash))
        {
            return null;
        }

        return new AuthResponse(usuario.Id, usuario.Nome, usuario.Email, GerarToken(usuario));
    }

    public async Task<UsuarioMeResponse?> ObterMeAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return await db.Usuarios
            .Where(x => x.Id == usuarioId)
            .Select(x => new UsuarioMeResponse(x.Id, x.Nome, x.Email, x.DataCadastro))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExcluirContaAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return false;
        }

        // Remove dependências explicitamente para evitar conflito de cascade no SQL Server.
        var feedbacks = await db.Feedbacks.Where(x => x.UsuarioId == usuarioId).ToListAsync(cancellationToken);
        var resultados = await db.Resultados.Where(x => x.UsuarioId == usuarioId).ToListAsync(cancellationToken);
        var conversas = await db.Conversas.Where(x => x.UsuarioId == usuarioId).ToListAsync(cancellationToken);
        var chats = await db.Chats.Where(x => x.UsuarioId == usuarioId).ToListAsync(cancellationToken);

        db.Feedbacks.RemoveRange(feedbacks);
        db.Resultados.RemoveRange(resultados);
        db.Conversas.RemoveRange(conversas);
        db.Chats.RemoveRange(chats);
        db.Usuarios.Remove(usuario);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidarCadastro(CadastroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || request.Nome.Trim().Length < 2)
        {
            throw new ArgumentException("Informe seu nome.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            throw new ArgumentException("Informe um e-mail válido.");
        }

        if (string.IsNullOrWhiteSpace(request.Senha) || request.Senha.Length < 6)
        {
            throw new ArgumentException("A senha deve ter pelo menos 6 caracteres.");
        }

        if (!request.ConsentimentoLgpd)
        {
            throw new ArgumentException("É necessário aceitar o uso dos dados (LGPD) para criar a conta.");
        }
    }

    private string GerarToken(Usuario usuario)
    {
        if (string.IsNullOrWhiteSpace(_jwt.Key) || _jwt.Key.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Key deve ter pelo menos 32 caracteres.");
        }

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(_jwt.ExpirationHours);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public static class AuthUserExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(value, out var id) ? id : null;
    }
}
