using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompraProgramada.Infrastructure.Kafka;

/// <summary>
/// Serviço de background que lê os eventos do Kafka (IR Dedo-Duro e IR Venda).
/// Exigência da fase 6: Leitura para log/registro no console.
/// </summary>
public class KafkaConsumerHostedService : BackgroundService
{
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaConsumerHostedService> _logger;
    private IConsumer<string, string>? _consumer;

    public KafkaConsumerHostedService(
        IOptions<KafkaSettings> settings,
        ILogger<KafkaConsumerHostedService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = "compra-programada-ir-logger",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();

        // Inscreve nos dois tópicos de IR
        _consumer.Subscribe(new[] { _settings.TopicIRDedoDuro, _settings.TopicIRVenda });

        _logger.LogInformation("Kafka Consumer iniciado. Ouvindo os tópicos: {T1}, {T2}",
            _settings.TopicIRDedoDuro, _settings.TopicIRVenda);

        return base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Roda em uma thread separada para não travar o startup da API
        return Task.Run(() =>
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer?.Consume(stoppingToken);

                        if (consumeResult != null)
                        {
                            _logger.LogInformation(
                                "\n=== MENSAGEM RECEBIDA DO KAFKA ===\n" +
                                "Tópico: {Topic}\n" +
                                "Chave:  {Key}\n" +
                                "Valor:  {Value}\n" +
                                "==================================",
                                consumeResult.Topic, consumeResult.Message.Key, consumeResult.Message.Value);
                        }
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError("Erro ao consumir mensagem: {Reason}", e.Error.Reason);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cancelamento solicitado. Encerrando consumer...");
            }
        }, stoppingToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer?.Close();
        _consumer?.Dispose();
        _logger.LogInformation("Kafka Consumer encerrado.");

        return base.StopAsync(cancellationToken);
    }
}
