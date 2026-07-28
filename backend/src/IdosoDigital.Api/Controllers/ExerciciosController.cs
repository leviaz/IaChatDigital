using IdosoDigital.Api.Auth;
using IdosoDigital.Api.Exercicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdosoDigital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/exercicios")]
public sealed class ExerciciosController(IExercicioService exercicioService) : ControllerBase
{
    [HttpPost("gerar")]
    public async Task<IActionResult> Gerar([FromBody] GerarExercicioRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { mensagem = "Faça login novamente." });
        }

        try
        {
            var result = await exercicioService.GerarAsync(
                userId.Value,
                request ?? new GerarExercicioRequest(null, null),
                cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensagem = ex.Message });
        }
        catch (Exception)
        {
            return BadRequest(new { mensagem = "Não foi possível gerar o exercício agora. Tente de novo." });
        }
    }

    [HttpPost("{id:guid}/responder")]
    public async Task<IActionResult> Responder(Guid id, [FromBody] ResponderExercicioRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { mensagem = "Faça login novamente." });
        }

        try
        {
            var result = await exercicioService.ResponderAsync(userId.Value, id, request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensagem = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }

    [HttpGet("pontuacao")]
    public async Task<IActionResult> Pontuacao([FromQuery] string? categoria, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { mensagem = "Faça login novamente." });
        }

        var result = await exercicioService.ObterPontuacaoAsync(userId.Value, categoria, cancellationToken);
        return Ok(result);
    }
}
