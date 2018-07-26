using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SCMWebApiCore.Models
{
    public partial class SCM_GAMEContext : DbContext
    {
        public virtual DbSet<Game> Game { get; set; }
        public virtual DbSet<GameTeamPlayerRelationship> GameTeamPlayerRelationship { get; set; }
        public virtual DbSet<InventoryInformation> InventoryInformation { get; set; }
        public virtual DbSet<Player> Player { get; set; }
        public virtual DbSet<PlayerRole> PlayerRole { get; set; }
        public virtual DbSet<PlayerTransactions> PlayerTransactions { get; set; }
        public virtual DbSet<Results> Results { get; set; }
        public virtual DbSet<Team> Team { get; set; }

        public SCM_GAMEContext(DbContextOptions<SCM_GAMEContext> options) : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>(entity =>
            {
                entity.ToTable("GAME");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.DeliveryDelay).HasColumnName("DELIVERY_DELAY");

                entity.Property(e => e.MaxPeriod).HasColumnName("MAX_PERIOD");

                entity.Property(e => e.Name).HasColumnName("NAME");

                entity.Property(e => e.DemandInformation).HasColumnName("DEMAND_INFORMATION");

                entity.Property(e => e.FacilitatorId).HasColumnName("FACILITATOR_ID");

                entity.Property(e => e.GameUrl).HasColumnName("GAME_URL");
            });

            modelBuilder.Entity<GameTeamPlayerRelationship>(entity =>
            {
                entity.ToTable("GAME_TEAM_PLAYER_RELATIONSHIP");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.GameId).HasColumnName("GAME_ID");

                entity.Property(e => e.PlayerId).HasColumnName("PLAYER_ID");

                entity.Property(e => e.TeamId).HasColumnName("TEAM_ID");

                entity.HasOne(d => d.Game)
                    .WithMany(p => p.GameTeamPlayerRelationship)
                    .HasForeignKey(d => d.GameId)
                    .HasConstraintName("FK_GAME_TEAM_PLAYER_RELATIONSHIP_GAME");

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.GameTeamPlayerRelationship)
                    .HasForeignKey(d => d.PlayerId)
                    .HasConstraintName("FK_GAME_TEAM_PLAYER_RELATIONSHIP_PLAYER");

                entity.HasOne(d => d.Team)
                    .WithMany(p => p.GameTeamPlayerRelationship)
                    .HasForeignKey(d => d.TeamId)
                    .HasConstraintName("FK_GAME_TEAM_PLAYER_RELATIONSHIP_TEAM");
            });

            modelBuilder.Entity<InventoryInformation>(entity =>
            {
                entity.ToTable("INVENTORY_INFORMATION");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Backlogs).HasColumnName("BACKLOGS");

                entity.Property(e => e.CurrentInventory).HasColumnName("CURRENT_INVENTORY");

                entity.Property(e => e.IncomingInventory).HasColumnName("INCOMING_INVENTORY");

                entity.Property(e => e.NewOrder).HasColumnName("NEW_ORDER");

                entity.Property(e => e.TotalCost).HasColumnName("TOTAL_COST");
            });

            modelBuilder.Entity<Player>(entity =>
            {
                entity.ToTable("PLAYER");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.ConnectionId).HasColumnName("CONNECTION_ID");

                entity.Property(e => e.Email).HasColumnName("EMAIL");

                entity.Property(e => e.FirstName).HasColumnName("FIRST_NAME");

                entity.Property(e => e.HasMadeDecision).HasColumnName("HAS_MADE_DECISION");

                entity.Property(e => e.InventoryId).HasColumnName("INVENTORY_ID");

                entity.Property(e => e.IsAvailable).HasColumnName("IS_AVAILABLE");

                entity.Property(e => e.LastName).HasColumnName("LAST_NAME");

                entity.Property(e => e.Password).HasColumnName("PASSWORD");

                entity.Property(e => e.PlayerRoleId).HasColumnName("PLAYER_ROLE_ID");

                entity.Property(e => e.Username).HasColumnName("USERNAME");

                entity.HasOne(d => d.Inventory)
                    .WithMany(p => p.Player)
                    .HasForeignKey(d => d.InventoryId)
                    .HasConstraintName("FK_PLAYER_INVENTORY_INFORMATION");

                entity.HasOne(d => d.PlayerRole)
                    .WithMany(p => p.Player)
                    .HasForeignKey(d => d.PlayerRoleId)
                    .HasConstraintName("FK_PLAYER_PLAYER_ROLE");
            });

            modelBuilder.Entity<PlayerRole>(entity =>
            {
                entity.ToTable("PLAYER_ROLE");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Role).HasColumnName("ROLE");
            });

            modelBuilder.Entity<PlayerTransactions>(entity =>
            {
                entity.ToTable("PLAYER_TRANSACTIONS");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Cost)
                    .HasColumnName("COST")
                    .HasColumnType("decimal(18, 0)");

                entity.Property(e => e.GameId).HasColumnName("GAME_ID");

                entity.Property(e => e.Inventory).HasColumnName("INVENTORY");

                entity.Property(e => e.OrderMadeFrom).HasColumnName("ORDER_MADE_FROM");

                entity.Property(e => e.OrderMadePeriod).HasColumnName("ORDER_MADE_PERIOD");

                entity.Property(e => e.OrderMadeTo).HasColumnName("ORDER_MADE_TO");

                entity.Property(e => e.OrderQty).HasColumnName("ORDER_QTY");

                entity.Property(e => e.SentQty).HasColumnName("SENT_QTY");

                entity.Property(e => e.OrderReceivePeriod).HasColumnName("ORDER_RECEIVE_PERIOD");

                entity.Property(e => e.TeamId).HasColumnName("TEAM_ID");
            });

            modelBuilder.Entity<Results>(entity =>
            {
                entity.ToTable("RESULTS");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.GameTeamPlayerRelationshipId).HasColumnName("GAME_TEAM_PLAYER_RELATIONSHIP_ID");

                entity.Property(e => e.Inventory).HasColumnName("INVENTORY");

                entity.Property(e => e.IncomingInventory).HasColumnName("INCOMING_INVENTORY");

                entity.Property(e => e.PreviousOrder).HasColumnName("PREVIOUS_ORDER");

                entity.Property(e => e.Period).HasColumnName("PERIOD");

                entity.Property(e => e.OrderQty).HasColumnName("ORDER_QTY");

                entity.Property(e => e.SentQty).HasColumnName("SENT_QTY");

                entity.Property(e => e.TotalCost).HasColumnName("TOTAL_COST");

                entity.Property(e => e.TotalNeeded).HasColumnName("TOTAL_NEEDED");

                entity.HasOne(d => d.GameTeamPlayerRelationship)
                    .WithMany(p => p.Results)
                    .HasForeignKey(d => d.GameTeamPlayerRelationshipId)
                    .HasConstraintName("FK_RESULTS_GAME_TEAM_PLAYER_RELATIONSHIP");
            });

            modelBuilder.Entity<Team>(entity =>
            {
                entity.ToTable("TEAM");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Name).HasColumnName("NAME");

                entity.Property(e => e.CurrentPeriod).HasColumnName("CURRENT_PERIOD");

                entity.Property(e => e.CurrentOrder).HasColumnName("CURRENT_ORDER");
            });
        }
    }
}
