using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WebApplication2.Models.VM;

namespace WebApplication2.Models;

public partial class Entities : DbContext
{
    public Entities()
    {
    }

    public Entities(DbContextOptions<Entities> options)
        : base(options)
    {
    }

    public virtual DbSet<GamesCategory> GamesCategories { get; set; }

    public virtual DbSet<LotDetail> LotDetails { get; set; }

    public virtual DbSet<Seller> Sellers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=KOMPUTER;Database=kursovoi;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GamesCategory>(entity =>
        {
            entity.HasKey(e => e.GamesCategoryId).HasName("PK__GamesCat__7372098AFE468996");

            entity.ToTable("GamesCategory");

            entity.Property(e => e.GamesCategoryId).HasColumnName("GamesCategoryID");
            entity.Property(e => e.Category)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.GameName)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<LotDetail>(entity =>
        {
            entity.HasKey(e => e.LotId).HasName("PK__LotDetai__4160EF4D17389452");

            entity.Property(e => e.LotId).HasColumnName("LotID");
            entity.Property(e => e.DescriptionLot).HasColumnType("text");
            entity.Property(e => e.GamesCategoryId).HasColumnName("GamesCategoryID");
            entity.Property(e => e.Price)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.SellerId).HasColumnName("SellerID");
            entity.Property(e => e.ServerName)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.GamesCategory).WithMany(p => p.LotDetails)
                .HasForeignKey(d => d.GamesCategoryId)
                .HasConstraintName("FK_LotDetails_GamesCatagory");

            entity.HasOne(d => d.Seller).WithMany(p => p.LotDetails)
                .HasForeignKey(d => d.SellerId)
                .HasConstraintName("FK_LotDetails_Seller");
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasKey(e => e.SellerId).HasName("PK__Sellers__7FE3DBA1D503C0B6");

            entity.Property(e => e.SellerId).HasColumnName("SellerID");
            entity.Property(e => e.RatingStar)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SellerInfo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Sellername)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

public DbSet<WebApplication2.Models.VM.LotViewModel> LotViewModel { get; set; } = default!;

public DbSet<WebApplication2.Models.VM.SellerViewModel> SellerViewModel { get; set; } = default!;
}
