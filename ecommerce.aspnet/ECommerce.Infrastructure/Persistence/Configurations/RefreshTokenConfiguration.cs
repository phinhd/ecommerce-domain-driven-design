using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.JwtId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsRevoked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.AddedDate)
            .IsRequired();

        builder.Property(x => x.ExpiryDate)
            .IsRequired();

        // Unique Index on Token (SHA-256 hash)
        builder.HasIndex(x => x.Token)
            .IsUnique()
            .HasDatabaseName("ix_refresh_tokens_token");

        // Index on UserId for performance
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("ix_refresh_tokens_user_id");

        // Relationship: User (1) - (N) RefreshToken
        // ON DELETE CASCADE
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
