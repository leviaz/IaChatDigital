using IdosoDigital.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdosoDigital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/usuarios")]
public sealed class UsuariosController(IAuthService authService) : ControllerBase
{
    /// <summary>Exclui a conta do usuário autenticado (LGPD).</summary>
    [HttpDelete("me")]
    public async Task<IActionResult> ExcluirMe(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var removido = await authService.ExcluirContaAsync(userId.Value, cancellationToken);
        return removido ? NoContent() : NotFound();
    }
}
