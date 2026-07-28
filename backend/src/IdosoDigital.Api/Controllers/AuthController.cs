using IdosoDigital.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdosoDigital.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("cadastro")]
    [AllowAnonymous]
    public async Task<IActionResult> Cadastro([FromBody] CadastroRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await authService.CadastrarAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensagem = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new { mensagem = "E-mail ou senha incorretos." });
        }

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var me = await authService.ObterMeAsync(userId.Value, cancellationToken);
        return me is null
            ? Unauthorized(new { mensagem = "Sua sessão expirou ou a conta não existe mais. Entre novamente." })
            : Ok(me);
    }
}
