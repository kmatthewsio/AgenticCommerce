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
    public DbSet<X402PaymentEntity> X402Payments => Set<X402PaymentEntity>();

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

        modelBuilder.Entity<X402PaymentEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PaymentId).IsUnique();
            entity.HasIndex(e => e.PayerAddress);
            entity.HasIndex(e => e.TransactionHash);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Network);
        });
    }
}