using Microsoft.EntityFrameworkCore;
using Offichat.Application.DTOs;
using Offichat.Domain.Interfaces; // IPasswordHasher buradan geliyor
using Offichat.Application.PacketRouting;
using Offichat.Application.Session;
using Offichat.Infrastructure.Persistence;
using Offichat.Network;
using System.Text;
using System.Text.Json;
using Offichat.Application.Enums;

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
                // --- 1. PAKETİ ÇÖZ (DESERIALIZE) ---
                var jsonString = packet.GetPayloadAsString();
                var payload = JsonSerializer.Deserialize<LoginPayload>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload == null || string.IsNullOrWhiteSpace(payload.Username) || string.IsNullOrWhiteSpace(payload.Password))
                {
                    await SendError(session, "Invalid payload");
                    return;
                }

                // --- 2. KULLANICIYI BUL (DB) ---
                // Appearance verisi için Players tablosunu da çekiyoruz
                var user = await _context.Users
                                         .Include(u => u.Players)
                                         .FirstOrDefaultAsync(u => u.Username == payload.Username);

                if (user == null)
                {
                    await SendError(session, "Invalid username or password");
                    return;
                }

                // --- 3. ŞİFREYİ DOĞRULA ---
                if (!_passwordHasher.VerifyPassword(payload.Password, user.PasswordHash))
                {
                    await SendError(session, "Invalid username or password");
                    return;
                }

                // Kullanıcının ana karakterini bul
                var mainPlayer = user.Players.FirstOrDefault();

                // --- 4. OTURUMU GÜNCELLE (SESSION UPDATE) ---
                session.Username = user.Username;
                session.UserId = user.Id;
                session.PlayerId = mainPlayer?.Id;
                session.DisplayName = mainPlayer?.DisplayName;

                // Cache (RAM) Verilerini Hazırla
                session.AppearanceData = mainPlayer?.AppearanceData;
                session.X = 0; // Spawn noktası X
                session.Y = 0; // Spawn noktası Y
                session.Anim = AnimationState.Idle; // Varsayılan Animasyon
                session.Direction = Direction.Down; // Varsayılan Yön

                session.UpdateActivity();

                Console.WriteLine($"[Login] Success: {user.Username} (Player ID: {mainPlayer?.Id})");

                // --- 5. CEVAP GÖNDER (LOGIN OK) ---
                var responsePayload = new
                {
                    Message = "Login OK",
                    PlayerId = mainPlayer?.Id ?? 0,
                    DisplayName = mainPlayer?.DisplayName,
                    Appearance = mainPlayer?.AppearanceData // Client karakteri oluşturabilsin diye
                };

                string responseJson = JsonSerializer.Serialize(responsePayload);
                var responsePacket = new TCPPacket((byte)PacketId, session.SessionId, Encoding.UTF8.GetBytes(responseJson));
                await session.SendTcpAsync(responsePacket);

                // --- 6. BROADCAST: DİĞERLERİNE HABER VER (SPAWN) ---
                if (mainPlayer != null)
                {
                    var spawnPayload = new SpawnPayload
                    {
                        PlayerId = mainPlayer.Id,
                        DisplayName = mainPlayer.DisplayName,
                        Appearance = mainPlayer.AppearanceData,
                        X = 0,
                        Y = 0,
                        Anim = AnimationState.Idle,
                        Direction = Direction.Down
                    };

                    string spawnJson = JsonSerializer.Serialize(spawnPayload);
                    var spawnPacket = new TCPPacket(4, session.SessionId, Encoding.UTF8.GetBytes(spawnJson)); // Packet ID 4: Spawn

                    // Kendim hariç herkese gönder
                    await _sessionManager.BroadcastTcpExceptAsync(spawnPacket, session.SessionId);
                    Console.WriteLine($"[Login] Broadcasted spawn for {mainPlayer.DisplayName}");

                    // --- 7. INITIAL STATE SYNC: MEVCUT OYUNCULARI AL ---
                    // "Odaya girdiğimde içeride kimler vardı?"

                    var activeSessions = _sessionManager.GetAllSessions();

                    foreach (var otherSession in activeSessions)
                    {
                        // Kendim değilsem VE giriş yapmış (karakteri olan) biriyse
                        if (otherSession.SessionId != session.SessionId && otherSession.PlayerId.HasValue)
                        {
                            // OPTİMİZASYON: Veritabanına gitmiyoruz!
                            // Session nesnesindeki "son bilinen" (Cached) değerleri kullanıyoruz.

                            var existingPlayerSpawn = new SpawnPayload
                            {
                                PlayerId = otherSession.PlayerId.Value,
                                DisplayName = otherSession.DisplayName ?? "Unknown",
                                Appearance = otherSession.AppearanceData ?? "{}",

                                // Session'daki anlık konum ve durum
                                X = otherSession.X,
                                Y = otherSession.Y,
                                Anim = otherSession.Anim,
                                Direction = otherSession.Direction
                            };

                            string existingPlayerJson = JsonSerializer.Serialize(existingPlayerSpawn);

                            // Packet ID 4 (Spawn) olarak BANA gönder
                            var packetForMe = new TCPPacket(4, session.SessionId, Encoding.UTF8.GetBytes(existingPlayerJson));

                            await session.SendTcpAsync(packetForMe);
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