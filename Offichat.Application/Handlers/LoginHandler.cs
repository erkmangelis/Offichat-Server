using Microsoft.EntityFrameworkCore;
using Offichat.Application.DTOs;
using Offichat.Domain.Interfaces; // IPasswordHasher buradan geliyor
using Offichat.Application.PacketRouting;
using Offichat.Application.Session;
using Offichat.Infrastructure.Persistence;
using Offichat.Network;
using System.Text;
using System.Text.Json;

namespace Offichat.Application.Handlers
{
    public class LoginHandler : IPacketHandler
    {
        public ushort PacketId => 1; // 1: Login

        private readonly OffichatDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly SessionManager _sessionManager;

        public LoginHandler(OffichatDbContext context, IPasswordHasher passwordHasher, SessionManager sessionManager)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _sessionManager = sessionManager;
        }

        public async Task HandleAsync(PacketBase packet, PlayerSession session)
        {
            try
            {
                // 1. Paketi Çöz (Deserialize)
                var jsonString = packet.GetPayloadAsString();
                var payload = JsonSerializer.Deserialize<LoginPayload>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload == null || string.IsNullOrWhiteSpace(payload.Username) || string.IsNullOrWhiteSpace(payload.Password))
                {
                    await SendError(session, "Invalid payload");
                    return;
                }

                // 2. Kullanıcıyı Bul (User tablosu)
                // Oyuncu verisini (Player) de dahil ediyoruz (Include) çünkü görünüm bilgisi lazım olabilir.
                var user = await _context.Users
                                         .Include(u => u.Players)
                                         .FirstOrDefaultAsync(u => u.Username == payload.Username);

                if (user == null)
                {
                    // Güvenlik: "Kullanıcı bulunamadı" demek yerine genel hata dönmek brute-force'u zorlaştırır.
                    await SendError(session, "Invalid username or password");
                    return;
                }

                // 3. Şifreyi Doğrula
                if (!_passwordHasher.VerifyPassword(payload.Password, user.PasswordHash))
                {
                    await SendError(session, "Invalid username or password");
                    return;
                }

                // Kullanıcının ana karakterini bul (Şimdilik ilk karakteri alıyoruz)
                var mainPlayer = user.Players.FirstOrDefault();

                // 4. Oturumu Başlat (Session Update)
                session.Username = user.Username;
                session.UserId = user.Id;
                session.PlayerId = mainPlayer?.Id;
                session.UpdateActivity();

                Console.WriteLine($"[Login] Success: {user.Username} (Player ID: {mainPlayer?.Id})");

                // 5. Cevap Gönder
                // İstemciye oyuncunun görünüm bilgisini (AppearanceData) de geri dönelim ki kendi karakterini oluşturabilsin.
                var responsePayload = new
                {
                    Message = "Login OK",
                    PlayerId = mainPlayer?.Id ?? 0,
                    DisplayName = mainPlayer?.DisplayName,
                    Appearance = mainPlayer?.AppearanceData // JSON string olarak gider
                };

                string responseJson = JsonSerializer.Serialize(responsePayload);
                var responsePacket = new TCPPacket((byte)PacketId, session.SessionId, Encoding.UTF8.GetBytes(responseJson));
                await session.SendTcpAsync(responsePacket);

                // 6. Diğerlerine "Ben Geldim" de (PacketId: 4 olsun - Spawn)
                var spawnPayload = new SpawnPayload
                {
                    PlayerId = mainPlayer?.Id ?? 0,
                    DisplayName = mainPlayer?.DisplayName,
                    Appearance = mainPlayer?.AppearanceData,
                    X = 0, // Ofis kapı girişi koordinatı
                    Y = 0
                };

                string spawnJson = JsonSerializer.Serialize(spawnPayload);
                var spawnPacket = new TCPPacket(4, session.SessionId, Encoding.UTF8.GetBytes(spawnJson)); // Packet ID 4: Spawn

                // Kendim hariç herkese gönder
                await _sessionManager.BroadcastTcpExceptAsync(spawnPacket, session.SessionId);

                Console.WriteLine($"[Login] Broadcasted spawn for {mainPlayer?.DisplayName}");

                // 7. İçerideki diğer oyuncuları BANA gönder
                // Mevcut sessionları geziyoruz
                var otherSessions = _sessionManager.GetAllSessions();
                foreach (var otherSession in otherSessions)
                {
                    // Kendim değilsem ve login olmuşsa
                    if (otherSession.SessionId != session.SessionId && otherSession.PlayerId.HasValue)
                    {
                        // O oyuncunun verisini veritabanından çekmek yerine
                        // Session üstünde Appearance tutabiliriz (Performans için) 
                        // AMA şimdilik veritabanından çekelim (Basitlik için)

                        // Küçük bir performans uyarısı: Burada döngü içinde DB sorgusu var. 
                        // İleride bunu optimize edeceğiz (Cache veya Session'da data tutarak).
                        var otherPlayer = await _context.Players.FindAsync(otherSession.PlayerId.Value);
                        if (otherPlayer != null)
                        {
                            var otherUserSpawn = new SpawnPayload
                            {
                                PlayerId = otherPlayer.Id,
                                DisplayName = otherPlayer.DisplayName,
                                Appearance = otherPlayer.AppearanceData,
                                X = 0, // İleride anlık konumunu session'dan alacağız
                                Y = 0
                            };

                            string otherJson = JsonSerializer.Serialize(otherUserSpawn);
                            var otherPacket = new TCPPacket(4, session.SessionId, Encoding.UTF8.GetBytes(otherJson));

                            // Sadece BANA gönder
                            await session.SendTcpAsync(otherPacket);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Login] Error: {ex.Message}");
                await SendError(session, "Internal server error");
            }
        }

        private async Task SendError(PlayerSession session, string message)
        {
            Console.WriteLine($"[Login] Failed for session {session.SessionId}: {message}");
            var response = new TCPPacket((byte)PacketId, session.SessionId, Encoding.UTF8.GetBytes("Error: " + message));
            await session.SendTcpAsync(response);
        }
    }
}