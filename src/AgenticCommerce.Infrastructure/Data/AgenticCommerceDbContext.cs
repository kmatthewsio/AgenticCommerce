using AgenticCommerce.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AgenticCommerce.Infrastructure.Data;

public class AgenticCommerceDbContext : DbContext
{
    public AgenticCommerceDbContext(DbContextOptions<AgenticCommerceDbContext> options)
        : base(options)
    {
    }

    public DbSet<AgentEntity> Agents => Set<AgentEntity>();
    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AgentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WalletAddress);
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.Transactions)
                .WithOne(e => e.Agent)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TransactionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.AgentId);
            entity.HasIndex(e => e.TransactionId);
        });
    }
}