using Microsoft.EntityFrameworkCore;
using Offichat.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Offichat.Infrastructure.Persistence
{
    public class OffichatDbContext : DbContext
    {
        // Design-time creation (Migration oluştururken) için boş constructor gerekebilir
        public OffichatDbContext() { }

        public OffichatDbContext(DbContextOptions<OffichatDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<ChatLog> ChatLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Indexler
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // DATA SEEDING (Başlangıç Verileri)

            // 1. Rolleri oluştur
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "User" }
            );

            // 2. Admin Kullanıcısını oluştur
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = "admin123_HASHED", // LoginHandler'da bunu doğrulayacak algoritma olmalı
                    RoleId = 1, // Admin rolü
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            // Default Admin Oyuncusu
            modelBuilder.Entity<Player>().HasData(
                new Player
                {
                    Id = 1,
                    UserId = 1,
                    DisplayName = "Admin",
                    JobTitle = "System Administrator",
                    StatusMessage = "Watching you...",
                    AppearanceData = "{ \"type\": \"default_admin\" }"
                }
            );
        }
    }
}