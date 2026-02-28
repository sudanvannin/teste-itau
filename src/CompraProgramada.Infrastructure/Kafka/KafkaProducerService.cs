using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CompraProgramada.Infrastructure.Kafka;

public interface IKafkaProducerService
{
    Task PublishAsync<TMessage>(string topic, string key, TMessage message);
}

/// <summary>
/// Implementação real do produtor Kafka usando Confluent.Kafka.
/// Envia eventos serializados em JSON para os tópicos configurados.
/// </summary>
public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaProducerService(IOptions<KafkaSettings> settings, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var config = new ProducerConfig
        {
            BootstrapServers = settings.Value.BootstrapServers,
            Acks = Acks.All, // Garantia de entrega (all = líder + réplicas síncronas)
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<TMessage>(string topic, string key, TMessage message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            var kafkaMessage = new Message<string, string> { Key = key, Value = json };

            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage);

            _logger.LogInformation(
                "Mensagem enviada no topico {Topic} | Particao: {Partition} | Offset: {Offset} | Key: {Key}",
                deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset, key);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Erro ai publicar mensagem no Kafka (Topico: {Topic})", topic);
            throw; // Repassa erro para abortar a transação caso a política exija
        }
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
