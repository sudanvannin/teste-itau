using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Exceptions;
using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.Api.Controllers;

/// <summary>
/// Endpoints do cliente — adesão, saída, alterações e consultas.
/// </summary>
[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    /// <summary>
    /// Adesão ao produto de compra programada.
    /// Cria o cliente e a conta gráfica filhote.
    /// </summary>
    [HttpPost("adesao")]
    [ProducesResponseType(typeof(AdesaoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Aderir([FromBody] AdesaoRequest request)
    {
        try
        {
            var response = await _clienteService.AderirAsync(request);
            return Created($"/api/clientes/{response.ClienteId}/carteira", response);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new ErroResponse(ex.Message, ex.Codigo));
        }
    }

    /// <summary>
    /// Solicita a saída do produto. O cliente fica inativo mas mantém sua custódia.
    /// </summary>
    [HttpPost("{clienteId:int}/saida")]
    [ProducesResponseType(typeof(SaidaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sair(int clienteId)
    {
        try
        {
            var response = await _clienteService.SairAsync(clienteId);
            return Ok(response);
        }
        catch (BusinessException ex) when (ex.Codigo == "CLIENTE_NAO_ENCONTRADO")
        {
            return NotFound(new ErroResponse(ex.Message, ex.Codigo));
        }
        catch (BusinessException ex)
        {
            return BadRequest(new ErroResponse(ex.Message, ex.Codigo));
        }
    }

    /// <summary>
    /// Altera o valor do aporte mensal do cliente.
    /// </summary>
    [HttpPatch("{clienteId:int}/valor-mensal")]
    [ProducesResponseType(typeof(AlterarValorMensalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarValorMensal(int clienteId, [FromBody] AlterarValorMensalRequest request)
    {
        try
        {
            var response = await _clienteService.AlterarValorMensalAsync(clienteId, request);
            return Ok(response);
        }
        catch (BusinessException ex) when (ex.Codigo == "CLIENTE_NAO_ENCONTRADO")
        {
            return NotFound(new ErroResponse(ex.Message, ex.Codigo));
        }
        catch (BusinessException ex)
        {
            return BadRequest(new ErroResponse(ex.Message, ex.Codigo));
        }
    }

    /// <summary>
    /// Consulta a carteira do cliente com cotações atuais e P/L.
    /// </summary>
    [HttpGet("{clienteId:int}/carteira")]
    [ProducesResponseType(typeof(CarteiraResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarCarteira(int clienteId)
    {
        try
        {
            var response = await _clienteService.ConsultarCarteiraAsync(clienteId);
            return Ok(response);
        }
        catch (BusinessException ex)
        {
            return NotFound(new ErroResponse(ex.Message, ex.Codigo));
        }
    }

    /// <summary>
    /// Consulta a rentabilidade detalhada do cliente com evolução histórica.
    /// </summary>
    [HttpGet("{clienteId:int}/rentabilidade")]
    [ProducesResponseType(typeof(RentabilidadeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConsultarRentabilidade(int clienteId)
    {
        try
        {
            var response = await _clienteService.ConsultarRentabilidadeAsync(clienteId);
            return Ok(response);
        }
        catch (BusinessException ex)
        {
            return NotFound(new ErroResponse(ex.Message, ex.Codigo));
        }
    }
}
