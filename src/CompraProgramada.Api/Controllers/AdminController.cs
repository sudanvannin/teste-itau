using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Exceptions;
using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.Api.Controllers;

/// <summary>
/// Endpoints administrativos — cesta de recomendação, compras, distribuição e rebalanceamento.
/// </summary>
[ApiController]
[Route("api/admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly ICestaService _cestaService;
    private readonly IMotorCompraService _motorCompraService;
    private readonly IDistribuicaoService _distribuicaoService;
    private readonly IRebalanceamentoService _rebalanceamentoService;

    public AdminController(
        ICestaService cestaService,
        IMotorCompraService motorCompraService,
        IDistribuicaoService distribuicaoService,
        IRebalanceamentoService rebalanceamentoService)
    {
        _cestaService = cestaService;
        _motorCompraService = motorCompraService;
        _distribuicaoService = distribuicaoService;
        _rebalanceamentoService = rebalanceamentoService;
    }

    // ── Cesta de Recomendação ────────────────────────────────

    /// <summary>
    /// Cria ou substitui a cesta de recomendação ativa (Top Five).
    /// Se já existir uma cesta ativa, ela será desativada.
    /// </summary>
    [HttpPost("cesta")]
    [ProducesResponseType(typeof(CestaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CriarCesta([FromBody] CriarCestaRequest request)
    {
        try
        {
            var response = await _cestaService.CriarOuSubstituirAsync(request);
            return Created($"/api/admin/cesta", response);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new ErroResponse(ex.Message, ex.Codigo));
        }
    }

    /// <summary>
    /// Obtém a cesta de recomendação ativa.
    /// </summary>
    [HttpGet("cesta")]
    [ProducesResponseType(typeof(CestaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterCesta()
    {
        try
        {
            var response = await _cestaService.ObterAtivaAsync();
            return Ok(response);
        }
        catch (BusinessException ex)
        {
            return NotFound(new ErroResponse(ex.Message, ex.Codigo));
        }
    }

    // ── Motor de Compra ──────────────────────────────────────

    /// <summary>
    /// Dispara a compra consolidada na conta Master.
    /// </summary>
    [HttpPost("compra/disparar")]
    [ProducesResponseType(typeof(CompraConsolidadaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DispararCompra([FromBody] DisparoCompraRequest request)
    {
        try
        {
            var response = await _motorCompraService.ExecutarCompraAsync(request);
            return Ok(response);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new ErroResponse(ex.Message, ex.Codigo));
        }
    }

    // ── Distribuição ─────────────────────────────────────────

    /// <summary>
    /// Distribui os ativos da conta Master para as contas Filhote.
    /// </summary>
    [HttpPost("compra/{ordemCompraId:int}/distribuir")]
    [ProducesResponseType(typeof(DistribuicaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Distribuir(int ordemCompraId)
    {
        try
        {
            var response = await _distribuicaoService.DistribuirAsync(ordemCompraId);
            return Ok(response);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new ErroResponse(ex.Message, ex.Codigo));
        }
    }

    // ── Rebalanceamento ──────────────────────────────────────

    /// <summary>
    /// Rebalanceia todas as carteiras conforme a cesta ativa.
    /// </summary>
    [HttpPost("rebalanceamento")]
    [ProducesResponseType(typeof(RebalanceamentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Rebalancear()
    {
        try
        {
            var response = await _rebalanceamentoService.RebalancearAsync();
            return Ok(response);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new ErroResponse(ex.Message, ex.Codigo));
        }
    }
}
