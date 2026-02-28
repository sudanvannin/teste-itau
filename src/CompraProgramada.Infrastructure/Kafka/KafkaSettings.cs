namespace CompraProgramada.Infrastructure.Kafka;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string TopicIRDedoDuro { get; set; } = "ir-dedo-duro";
    public string TopicIRVenda { get; set; } = "ir-venda";
}
