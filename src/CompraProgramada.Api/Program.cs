using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Infrastructure.Kafka;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ──────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ── Swagger / OpenAPI ────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Compra Programada API",
        Version = "v1",
        Description = "Sistema de Compra Programada de Ações — Itaú Corretora"
    });
});

// ── Entity Framework Core + MySQL ────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ── Kafka ────────────────────────────────────────────────────
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

// ── Application Services ─────────────────────────────────────
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ICestaService, CestaService>();
builder.Services.AddScoped<ICotacaoService, CotacaoService>();
builder.Services.AddScoped<IMotorCompraService, MotorCompraService>();
builder.Services.AddScoped<IDistribuicaoService, DistribuicaoService>();
builder.Services.AddScoped<IRebalanceamentoService, RebalanceamentoService>();

var app = builder.Build();

// ── Middleware Pipeline ──────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Compra Programada API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

// ── Auto-migrate in Development ──────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();

// Needed for integration tests (WebApplicationFactory)
public partial class Program { }
