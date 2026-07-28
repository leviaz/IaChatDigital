using IdosoDigital.Api.Auth;
using IdosoDigital.Api.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdosoDigital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/chats")]
public sealed class ChatsController(IChatService chatService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { mensagem = "Faça login novamente." });
        }

        try
        {
            var chats = await chatService.ListarChatsAsync(userId.Value, cancellationToken);
            return Ok(chats);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensagem = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarChatRequest? request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { mensagem = "Faça login novamente." });
        }

        try
        {
            var chat = await chatService.CriarChatAsync(userId.Value, request ?? new CriarChatRequest(null), cancellationToken);
            return StatusCode(StatusCodes.Status201Created, chat);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensagem = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { mensagem = "Faça login novamente." });
        }

        try
        {
            var chat = await chatService.ObterChatAsync(userId.Value, id, cancellationToken);
            return chat is null ? NotFound(new { mensagem = "Chat não encontrado." }) : Ok(chat);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensagem = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { mensagem = "Faça login novamente." });
        }

        try
        {
            var ok = await chatService.ExcluirChatAsync(userId.Value, id, cancellationToken);
            return ok ? NoContent() : NotFound(new { mensagem = "Chat não encontrado." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensagem = ex.Message });
        }
    }

    [HttpPost("{id:guid}/mensagens")]
    public async Task<IActionResult> EnviarMensagem(Guid id, [FromBody] MensagemRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { mensagem = "Faça login novamente." });
        }

        try
        {
            var result = await chatService.EnviarMensagemAsync(userId.Value, id, request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { mensagem = ex.Message });
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
}

[ApiController]
[Authorize]
[Route("api/feedback")]
public sealed class FeedbackController(IChatService chatService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] FeedbackRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { mensagem = "Faça login novamente." });
        }

        try
        {
            var result = await chatService.RegistrarFeedbackAsync(userId.Value, request, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensagem = ex.Message });
        }
    }
}
