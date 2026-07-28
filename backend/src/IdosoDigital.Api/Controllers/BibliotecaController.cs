using IdosoDigital.Api.Data;
using IdosoDigital.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IdosoDigital.Api.Controllers;

public sealed record CategoriaResponse(Guid Id, string Nome, string Slug, string Descricao, int Ordem, int TotalConteudos);

public sealed record ConteudoResumoResponse(
    Guid Id,
    Guid CategoriaId,
    string CategoriaNome,
    string CategoriaSlug,
    string Titulo,
    string Tipo,
    int Ordem);

public sealed record ConteudoDetalheResponse(
    Guid Id,
    Guid CategoriaId,
    string CategoriaNome,
    string CategoriaSlug,
    string Titulo,
    string Tipo,
    string Corpo,
    string? UrlMidia,
    int Ordem);

[ApiController]
[Authorize]
[Route("api/categorias")]
public sealed class CategoriasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var itens = await db.Categorias
            .AsNoTracking()
            .OrderBy(x => x.Ordem)
            .Select(x => new CategoriaResponse(
                x.Id,
                x.Nome,
                x.Slug,
                x.Descricao,
                x.Ordem,
                x.Conteudos.Count))
            .ToListAsync(cancellationToken);

        return Ok(itens);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> ObterPorSlug(string slug, CancellationToken cancellationToken)
    {
        var categoria = await db.Categorias
            .AsNoTracking()
            .Where(x => x.Slug == slug)
            .Select(x => new CategoriaResponse(
                x.Id,
                x.Nome,
                x.Slug,
                x.Descricao,
                x.Ordem,
                x.Conteudos.Count))
            .FirstOrDefaultAsync(cancellationToken);

        return categoria is null
            ? NotFound(new { mensagem = "Categoria não encontrada." })
            : Ok(categoria);
    }
}

[ApiController]
[Authorize]
[Route("api/conteudos")]
public sealed class ConteudosController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? categoria, CancellationToken cancellationToken)
    {
        var query = db.Conteudos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            var slug = categoria.Trim().ToLowerInvariant();
            query = query.Where(x => x.Categoria.Slug == slug);
        }

        var itens = await query
            .OrderBy(x => x.Categoria.Ordem)
            .ThenBy(x => x.Ordem)
            .Select(x => new ConteudoResumoResponse(
                x.Id,
                x.CategoriaId,
                x.Categoria.Nome,
                x.Categoria.Slug,
                x.Titulo,
                x.Tipo.ToString(),
                x.Ordem))
            .ToListAsync(cancellationToken);

        return Ok(itens);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.Conteudos
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ConteudoDetalheResponse(
                x.Id,
                x.CategoriaId,
                x.Categoria.Nome,
                x.Categoria.Slug,
                x.Titulo,
                x.Tipo.ToString(),
                x.Corpo,
                x.UrlMidia,
                x.Ordem))
            .FirstOrDefaultAsync(cancellationToken);

        return item is null
            ? NotFound(new { mensagem = "Conteúdo não encontrado." })
            : Ok(item);
    }
}
