using IdosoDigital.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace IdosoDigital.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var dbOk = false;
        string? dbError = null;

        try
        {
            dbOk = await dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
        }

        return Ok(new
        {
            status = "ok",
            projeto = "Idoso Digital IA",
            database = new { connected = dbOk, error = dbError },
            ai = new { provider = "Ollama (gratuito/local)", mockFallback = true }
        });
    }
}
