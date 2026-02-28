using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CompraProgramada.Infrastructure.Data;

/// <summary>
/// Factory para gerar migrations sem precisar de conexão com o banco.
/// Usado pelo comando: dotnet ef migrations add
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Conexão dummy para geração de migrations offline
        optionsBuilder.UseMySql(
            "Server=localhost;Database=compra_programada;User=root;Password=root123;",
            ServerVersion.Parse("8.0.0-mysql"));

        return new AppDbContext(optionsBuilder.Options);
    }
}
