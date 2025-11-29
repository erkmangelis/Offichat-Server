using Offichat.Application.DTOs;
using Offichat.Domain.Interfaces;
using Offichat.Application.PacketRouting;
using Offichat.Application.Session;
using Offichat.Domain.Entities;
using Offichat.Infrastructure.Persistence;
using Offichat.Network;
using System.Text;
using System.Text.Json;

namespace Offichat.Application.Handlers
{
    public class RegisterHandler : IPacketHandler
    {
        public ushort PacketId => 3;

        private readonly OffichatDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterHandler(OffichatDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task HandleAsync(PacketBase packet, PlayerSession session)
        {
            try
            {
                var jsonString = packet.GetPayloadAsString();

                // JSON'u deserialize et (Büyük/küçük harf duyarsız)
                var payload = JsonSerializer.Deserialize<RegisterPayload>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload == null || string.IsNullOrWhiteSpace(payload.Username) || string.IsNullOrWhiteSpace(payload.Password))
                {
                    await SendResponse(session, "Error: Invalid payload");
                    return;
                }

                // Kullanıcı adı kontrolü
                // (Senkron Any kullandık şimdilik, DbContext thread-safety için)
                if (_context.Users.Any(u => u.Username == payload.Username))
                {
                    await SendResponse(session, "Error: Username taken");
                    return;
                }

                // 1. Kullanıcıyı oluştur (User)
                var newUser = new User
                {
                    Username = payload.Username,
                    PasswordHash = _passwordHasher.HashPassword(payload.Password),
                    RoleId = 2, // User rolü
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // 2. Oyuncu profilini oluştur (Player)
                var appearanceJson = JsonSerializer.Serialize(payload.Appearance);

                var newPlayer = new Player
                {
                    UserId = newUser.Id,
                    DisplayName = string.IsNullOrWhiteSpace(payload.DisplayName) ? payload.Username : payload.DisplayName,
                    JobTitle = "Newly Joined",
                    StatusMessage = "Welcome to the team!",
                    AppearanceData = appearanceJson
                };

                _context.Players.Add(newPlayer);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[Register] New user registered: {payload.Username}");
                await SendResponse(session, "Register OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Register] Error: {ex.Message}");
                await SendResponse(session, "Error: Internal server error");
            }
        }

        private async Task SendResponse(PlayerSession session, string message)
        {
            var responsePayload = new
            {
                Message = message // "Register OK" veya "Error: ..."
            };

            string json = JsonSerializer.Serialize(responsePayload);
            var response = new TCPPacket((byte)PacketId, session.SessionId, Encoding.UTF8.GetBytes(json));

            await session.SendTcpAsync(response);
        }
    }
}