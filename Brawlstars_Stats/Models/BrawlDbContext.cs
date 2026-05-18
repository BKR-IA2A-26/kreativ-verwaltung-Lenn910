using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Brawlstars_Stats.Models;

public partial class BrawlDbContext : DbContext
{
    public BrawlDbContext()
    {
    }

    public BrawlDbContext(DbContextOptions<BrawlDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Brawler> Brawlers { get; set; }

    public virtual DbSet<Map> Maps { get; set; }

    public virtual DbSet<Match> Matches { get; set; }

    public virtual DbSet<Modi> Modis { get; set; }

    public virtual DbSet<SpielerInfo> SpielerInfos { get; set; }

    public virtual DbSet<Werte> Wertes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Brawler>(entity =>
        {
            entity.HasKey(e => e.BrawlerId).HasName("PRIMARY");

            entity.ToTable("brawler");

            entity.Property(e => e.BrawlerId)
                .ValueGeneratedNever()
                .HasColumnName("brawler_id");
            entity.Property(e => e.Ang).HasColumnName("ang");
            entity.Property(e => e.Hp).HasColumnName("hp");
            entity.Property(e => e.Lvl)
                .HasDefaultValueSql("'1'")
                .HasColumnName("lvl");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Seltenheit)
                .HasMaxLength(100)
                .HasColumnName("seltenheit");
            entity.Property(e => e.Skin)
                .HasMaxLength(150)
                .HasDefaultValueSql("'Standard'")
                .HasColumnName("skin");
            entity.Property(e => e.Typ)
                .HasMaxLength(100)
                .HasColumnName("typ");
            entity.Property(e => e.Vert)
                .HasDefaultValueSql("'0'")
                .HasColumnName("vert");
        });

        modelBuilder.Entity<Map>(entity =>
        {
            entity.HasKey(e => e.MapId).HasName("PRIMARY");

            entity.ToTable("map");

            entity.Property(e => e.MapId)
                .ValueGeneratedNever()
                .HasColumnName("map_id");
            entity.Property(e => e.EmpfohlenerTyp)
                .HasMaxLength(100)
                .HasColumnName("empfohlener_typ");
            entity.Property(e => e.MapBezeichnung)
                .HasMaxLength(150)
                .HasColumnName("map_bezeichnung");
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.HasKey(e => e.MatchId).HasName("PRIMARY");

            entity.ToTable("match");

            entity.HasIndex(e => e.BrawlerId, "brawler_id");

            entity.HasIndex(e => e.Kuerzel, "kuerzel");

            entity.HasIndex(e => e.MapId, "map_id");

            entity.HasIndex(e => e.ModiId, "modi_id");

            entity.Property(e => e.MatchId).HasColumnName("match_id");
            entity.Property(e => e.BrawlerId).HasColumnName("brawler_id");
            entity.Property(e => e.Kuerzel)
                .HasMaxLength(50)
                .HasColumnName("kuerzel");
            entity.Property(e => e.MapId).HasColumnName("map_id");
            entity.Property(e => e.ModiId).HasColumnName("modi_id");

            entity.HasOne(d => d.Brawler).WithMany(p => p.Matches)
                .HasForeignKey(d => d.BrawlerId)
                .HasConstraintName("match_ibfk_2");

            entity.HasOne(d => d.KuerzelNavigation).WithMany(p => p.Matches)
                .HasForeignKey(d => d.Kuerzel)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("match_ibfk_1");

            entity.HasOne(d => d.Map).WithMany(p => p.Matches)
                .HasForeignKey(d => d.MapId)
                .HasConstraintName("match_ibfk_4");

            entity.HasOne(d => d.Modi).WithMany(p => p.Matches)
                .HasForeignKey(d => d.ModiId)
                .HasConstraintName("match_ibfk_3");
        });

        modelBuilder.Entity<Modi>(entity =>
        {
            entity.HasKey(e => e.ModiId).HasName("PRIMARY");

            entity.ToTable("modi");

            entity.Property(e => e.ModiId)
                .ValueGeneratedNever()
                .HasColumnName("modi_id");
            entity.Property(e => e.Bezeichnung)
                .HasMaxLength(100)
                .HasColumnName("bezeichnung");
            entity.Property(e => e.Gift).HasColumnName("gift");
            entity.Property(e => e.SpielerInsg).HasColumnName("spieler_insg");
            entity.Property(e => e.TeamGroesse).HasColumnName("team_groesse");
        });

        modelBuilder.Entity<SpielerInfo>(entity =>
        {
            entity.HasKey(e => e.Kuerzel).HasName("PRIMARY");

            entity.ToTable("spieler_info");

            entity.Property(e => e.Kuerzel)
                .HasMaxLength(50)
                .HasColumnName("kuerzel");
            entity.Property(e => e.Attribute)
                .HasMaxLength(255)
                .HasColumnName("attribute");
            entity.Property(e => e.GesamteKills)
                .HasDefaultValueSql("'0'")
                .HasColumnName("gesamte_kills");
            entity.Property(e => e.GesamteTode)
                .HasDefaultValueSql("'0'")
                .HasColumnName("gesamte_tode");
            entity.Property(e => e.KD)
                .HasDefaultValueSql("'0'")
                .HasColumnName("k_d");
            entity.Property(e => e.WinPercentInsg)
                .HasDefaultValueSql("'0'")
                .HasColumnName("win_percent_insg");
        });

        modelBuilder.Entity<Werte>(entity =>
        {
            entity.HasKey(e => e.WerteId).HasName("PRIMARY");

            entity.ToTable("werte");

            entity.HasIndex(e => e.MatchId, "match_id");

            entity.Property(e => e.WerteId).HasColumnName("werte_id");
            entity.Property(e => e.Kills)
                .HasDefaultValueSql("'0'")
                .HasColumnName("kills");
            entity.Property(e => e.MatchId).HasColumnName("match_id");
            entity.Property(e => e.Platz).HasColumnName("platz");
            entity.Property(e => e.Schaden)
                .HasDefaultValueSql("'0'")
                .HasColumnName("schaden");
            entity.Property(e => e.Starspieler)
                .HasDefaultValueSql("'0'")
                .HasColumnName("starspieler");
            entity.Property(e => e.Tode)
                .HasDefaultValueSql("'0'")
                .HasColumnName("tode");

            entity.HasOne(d => d.Match).WithMany(p => p.Wertes)
                .HasForeignKey(d => d.MatchId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("werte_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
