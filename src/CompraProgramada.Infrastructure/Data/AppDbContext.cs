using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ContaGrafica> ContasGraficas => Set<ContaGrafica>();
    public DbSet<CustodiaItem> CustodiaItens => Set<CustodiaItem>();
    public DbSet<CestaRecomendacao> CestasRecomendacao => Set<CestaRecomendacao>();
    public DbSet<CestaItem> CestaItens => Set<CestaItem>();
    public DbSet<OrdemCompra> OrdensCompra => Set<OrdemCompra>();
    public DbSet<OrdemCompraItem> OrdensCompraItens => Set<OrdemCompraItem>();
    public DbSet<Distribuicao> Distribuicoes => Set<Distribuicao>();
    public DbSet<DistribuicaoItem> DistribuicaoItens => Set<DistribuicaoItem>();
    public DbSet<HistoricoValorMensal> HistoricoValoresMensais => Set<HistoricoValorMensal>();
    public DbSet<VendaRebalanceamento> VendasRebalanceamento => Set<VendaRebalanceamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Cliente ──────────────────────────────────────────
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CPF).IsUnique();
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CPF).IsRequired().HasMaxLength(11);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ValorMensal).HasPrecision(18, 2);
        });

        // ── ContaGrafica ─────────────────────────────────────
        modelBuilder.Entity<ContaGrafica>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NumeroConta).IsUnique();
            entity.Property(e => e.NumeroConta).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(10);

            entity.HasOne(e => e.Cliente)
                .WithOne(c => c.ContaGrafica)
                .HasForeignKey<ContaGrafica>(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── CustodiaItem ─────────────────────────────────────
        modelBuilder.Entity<CustodiaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Ticker).IsRequired().HasMaxLength(12);
            entity.Property(e => e.PrecoMedio).HasPrecision(18, 2);

            entity.HasIndex(e => new { e.ContaGraficaId, e.Ticker }).IsUnique();

            entity.HasOne(e => e.ContaGrafica)
                .WithMany(c => c.Custodia)
                .HasForeignKey(e => e.ContaGraficaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CestaRecomendacao ────────────────────────────────
        modelBuilder.Entity<CestaRecomendacao>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
        });

        // ── CestaItem ────────────────────────────────────────
        modelBuilder.Entity<CestaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Ticker).IsRequired().HasMaxLength(12);
            entity.Property(e => e.Percentual).HasPrecision(5, 2);

            entity.HasOne(e => e.CestaRecomendacao)
                .WithMany(c => c.Itens)
                .HasForeignKey(e => e.CestaRecomendacaoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── OrdemCompra ──────────────────────────────────────
        modelBuilder.Entity<OrdemCompra>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TotalConsolidado).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        });

        // ── OrdemCompraItem ──────────────────────────────────
        modelBuilder.Entity<OrdemCompraItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Ticker).IsRequired().HasMaxLength(12);
            entity.Property(e => e.PrecoUnitario).HasPrecision(18, 2);
            entity.Property(e => e.TipoMercado).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(e => e.OrdemCompra)
                .WithMany(o => o.Itens)
                .HasForeignKey(e => e.OrdemCompraId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Distribuicao ─────────────────────────────────────
        modelBuilder.Entity<Distribuicao>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.OrdemCompra)
                .WithMany(o => o.Distribuicoes)
                .HasForeignKey(e => e.OrdemCompraId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DistribuicaoItem ─────────────────────────────────
        modelBuilder.Entity<DistribuicaoItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Ticker).IsRequired().HasMaxLength(12);
            entity.Property(e => e.PrecoUnitario).HasPrecision(18, 2);

            entity.HasOne(e => e.Distribuicao)
                .WithMany(d => d.Itens)
                .HasForeignKey(e => e.DistribuicaoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── HistoricoValorMensal ─────────────────────────────
        modelBuilder.Entity<HistoricoValorMensal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ValorAnterior).HasPrecision(18, 2);
            entity.Property(e => e.ValorNovo).HasPrecision(18, 2);

            entity.HasOne(e => e.Cliente)
                .WithMany(c => c.HistoricoValores)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── VendaRebalanceamento ─────────────────────────────
        modelBuilder.Entity<VendaRebalanceamento>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Ticker).IsRequired().HasMaxLength(12);
            entity.Property(e => e.PrecoVenda).HasPrecision(18, 2);
            entity.Property(e => e.PrecoMedio).HasPrecision(18, 2);

            entity.HasOne(e => e.Cliente)
                .WithMany()
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed: Conta Master ───────────────────────────────
        modelBuilder.Entity<ContaGrafica>().HasData(new ContaGrafica
        {
            Id = 1,
            NumeroConta = "MST-000001",
            Tipo = TipoConta.Master,
            ClienteId = null,
            DataCriacao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
