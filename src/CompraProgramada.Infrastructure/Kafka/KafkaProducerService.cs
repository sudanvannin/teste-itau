using Microsoft.Extensions.Options;

namespace CompraProgramada.Infrastructure.Kafka;

public interface IKafkaProducerService
{
    Task PublishAsync<T>(string topic, string key, T message);
}

public class KafkaProducerService : IKafkaProducerService
{
    private readonly KafkaSettings _settings;

    public KafkaProducerService(IOptions<KafkaSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task PublishAsync<T>(string topic, string key, T message)
    {
        // Will be implemented in Fase 6 with real Confluent.Kafka producer
        await Task.CompletedTask;
    }
}
