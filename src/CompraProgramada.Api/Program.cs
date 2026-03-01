using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Infrastructure.Kafka;
using CompraProgramada.Infrastructure.Services;
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

// ── CORS (permite o frontend chamar a API direto do browser) ─
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
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
    options.UseMySql(connectionString!, ServerVersion.Parse("8.0.0-mysql")));

// ── Kafka ────────────────────────────────────────────────────
builder.Services.Configure<KafkaSettings>(
    builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddHostedService<KafkaConsumerHostedService>();

// ── Application Services ─────────────────────────────────────
builder.Services.AddScoped<IClienteService, CompraProgramada.Infrastructure.Services.ClienteService>();
builder.Services.AddScoped<ICestaService, CompraProgramada.Infrastructure.Services.CestaService>();
builder.Services.AddScoped<ICotacaoService, CompraProgramada.Infrastructure.Services.CotacaoInfraService>();
builder.Services.AddScoped<IMotorCompraService, CompraProgramada.Infrastructure.Services.MotorCompraService>();
builder.Services.AddScoped<IDistribuicaoService, CompraProgramada.Infrastructure.Services.DistribuicaoService>();
builder.Services.AddScoped<IRebalanceamentoService, CompraProgramada.Infrastructure.Services.RebalanceamentoService>();

var app = builder.Build();

// ── Middleware Pipeline ──────────────────────────────────────
app.UseCors();
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
    if (db.Database.IsRelational()) db.Database.Migrate();
}

app.Run();

// Needed for integration tests (WebApplicationFactory)
public partial class Program { }
