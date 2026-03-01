using System.Net.Http.Json;
using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Infrastructure.Kafka;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CompraProgramada.IntegrationTests;

/// <summary>
/// Factory customizada que:
/// 1. Substitui MySQL por InMemory (sem precisar de Docker)
/// 2. Remove o Kafka Hosted Consumer (sem precisar de Kafka)
/// 3. Mocka o KafkaProducer e o CotacaoService
/// 4. Faz seed da Conta Master (normalmente criada pela migration)
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 1. Trocar MySQL por InMemory
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("E2E_Test_Db"));

            // 2. Remover o BackgroundService do Kafka Consumer
            var hostedService = services.SingleOrDefault(
                d => d.ImplementationType == typeof(KafkaConsumerHostedService));
            if (hostedService != null) services.Remove(hostedService);

            // 3. Mockar Kafka Producer
            var kafkaSvc = services.SingleOrDefault(
                d => d.ServiceType == typeof(IKafkaProducerService));
            if (kafkaSvc != null) services.Remove(kafkaSvc);

            var mockKafka = new Mock<IKafkaProducerService>();
            mockKafka.Setup(x => x.PublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(mockKafka.Object);

            // 4. Mockar CotacaoService (retornar preços fixos dos 5 ativos)
            var cotacaoSvc = services.SingleOrDefault(
                d => d.ServiceType == typeof(ICotacaoService));
            if (cotacaoSvc != null) services.Remove(cotacaoSvc);

            var mockCotacao = new Mock<ICotacaoService>();
            var precosFixos = new Dictionary<string, decimal>
            {
                { "PETR4", 35.50m },
                { "VALE3", 65.20m },
                { "ITUB4", 32.10m },
                { "BBDC4", 15.80m },
                { "BBAS3", 55.40m }
            };
            mockCotacao.Setup(x => x.ObterCotacoesFechamento(It.IsAny<IEnumerable<string>>()))
                .Returns((IEnumerable<string> tickers) =>
                    tickers.Where(precosFixos.ContainsKey)
                           .ToDictionary(t => t, t => precosFixos[t]));
            mockCotacao.Setup(x => x.ObterPrecoFechamento(It.IsAny<string>()))
                .Returns((string t) => precosFixos.TryGetValue(t, out var p) ? p : (decimal?)null);

            services.AddScoped(_ => mockCotacao.Object);
        });

        builder.UseEnvironment("Testing");
    }
}

/// <summary>
/// Teste E2E completo: Adesão → Cesta → Compra → Distribuição → Consulta Carteira
/// </summary>
public class PurchaseJourneyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public PurchaseJourneyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Seed da conta Master (normalmente criada pela migration no ambiente real)
        SeedMasterAccount();
    }

    private void SeedMasterAccount()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.ContasGraficas.Any(c => c.Tipo == TipoConta.Master))
        {
            db.ContasGraficas.Add(new ContaGrafica
            {
                NumeroConta = "MST-000001",
                Tipo = TipoConta.Master,
                DataCriacao = DateTime.UtcNow,
                Custodia = new List<CustodiaItem>()
            });
            db.SaveChanges();
        }
    }

    [Fact]
    public async Task JornadaCompleta_AdesaoCompraDistribuicaoCarteira_DeveFuncionar()
    {
        // ── 1. ADESÃO ────────────────────────────────────────────────────
        var adesaoReq = new AdesaoRequest(
            "João Silva E2E", "12345678901", "joao.e2e@teste.com", 3000m);

        var adesaoRes = await _client.PostAsJsonAsync("/api/clientes/adesao", adesaoReq);
        adesaoRes.IsSuccessStatusCode.Should().BeTrue(
            $"Adesão falhou: {await adesaoRes.Content.ReadAsStringAsync()}");

        var adesaoData = await adesaoRes.Content.ReadFromJsonAsync<AdesaoResponse>();
        var clienteId = adesaoData!.ClienteId;

        adesaoData.Nome.Should().Be("João Silva E2E");
        adesaoData.ContaGrafica.Should().NotBeNull();
        adesaoData.ContaGrafica.Tipo.Should().Be("FILHOTE");

        // ── 2. CONFIGURAR CESTA TOP 5 ────────────────────────────────────
        var cestaReq = new CriarCestaRequest("Top 5 E2E Test", new List<CestaItemRequest>
        {
            new("PETR4", 25m),
            new("VALE3", 25m),
            new("ITUB4", 20m),
            new("BBDC4", 15m),
            new("BBAS3", 15m)
        });

        var cestaRes = await _client.PostAsJsonAsync("/api/admin/cesta", cestaReq);
        cestaRes.IsSuccessStatusCode.Should().BeTrue(
            $"Criação de cesta falhou: {await cestaRes.Content.ReadAsStringAsync()}");

        var cestaData = await cestaRes.Content.ReadFromJsonAsync<CestaResponse>();
        cestaData!.Itens.Should().HaveCount(5);

        // ── 3. DISPARAR COMPRA CONSOLIDADA (dia 5 do mês, 1/3) ───────────
        var disparoReq = new DisparoCompraRequest(DateTime.UtcNow);
        var disparoRes = await _client.PostAsJsonAsync("/api/admin/compra/disparar", disparoReq);
        disparoRes.IsSuccessStatusCode.Should().BeTrue(
            $"Disparo de compra falhou: {await disparoRes.Content.ReadAsStringAsync()}");

        var compraData = await disparoRes.Content.ReadFromJsonAsync<CompraConsolidadaResponse>();
        compraData!.TotalConsolidado.Should().Be(1000m);   // R$3000 / 3
        compraData.TotalClientes.Should().Be(1);
        compraData.Itens.Should().NotBeEmpty();

        var ordemId = compraData.OrdemCompraId;

        // ── 4. DISTRIBUIR PARA FILHOTE ───────────────────────────────────
        var distRes = await _client.PostAsync(
            $"/api/admin/compra/{ordemId}/distribuir", null);
        distRes.IsSuccessStatusCode.Should().BeTrue(
            $"Distribuição falhou: {await distRes.Content.ReadAsStringAsync()}");

        var distData = await distRes.Content.ReadFromJsonAsync<DistribuicaoResponse>();
        distData!.TotalClientes.Should().Be(1);

        // ── 5. CONSULTAR CARTEIRA DO CLIENTE ─────────────────────────────
        var carteiraRes = await _client.GetAsync($"/api/clientes/{clienteId}/carteira");
        carteiraRes.IsSuccessStatusCode.Should().BeTrue(
            $"Consulta carteira falhou: {await carteiraRes.Content.ReadAsStringAsync()}");

        var carteiraData = await carteiraRes.Content.ReadFromJsonAsync<CarteiraResponse>();
        carteiraData.Should().NotBeNull();
        carteiraData!.Ativos.Should().NotBeEmpty();
        carteiraData.Resumo.ValorTotalInvestido.Should().BeGreaterThan(0);
        carteiraData.Resumo.ValorAtualCarteira.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Adesao_CPFDuplicado_DeveRetornar400()
    {
        var req = new AdesaoRequest("Ana Teste", "99988877766", "ana@teste.com", 500m);

        var res1 = await _client.PostAsJsonAsync("/api/clientes/adesao", req);
        res1.IsSuccessStatusCode.Should().BeTrue();

        var res2 = await _client.PostAsJsonAsync("/api/clientes/adesao", req);
        res2.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var erro = await res2.Content.ReadFromJsonAsync<ErroResponse>();
        erro!.Codigo.Should().Be("CLIENTE_CPF_DUPLICADO");
    }
}
