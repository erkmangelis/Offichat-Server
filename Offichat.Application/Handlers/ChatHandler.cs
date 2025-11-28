using Offichat.Application.DTOs;
using Offichat.Application.PacketRouting;
using Offichat.Application.Session;
using Offichat.Domain.Entities;
using Offichat.Infrastructure.Persistence;
using Offichat.Network;
using System.Text;
using System.Text.Json;

namespace Offichat.Application.Handlers
{
    public class ChatHandler : IPacketHandler
    {
        public ushort PacketId => 5; // Chat Packet ID

        private readonly SessionManager _sessionManager;
        private readonly OffichatDbContext _context;

        public ChatHandler(SessionManager sessionManager, OffichatDbContext context)
        {
            _sessionManager = sessionManager;
            _context = context;
        }

        public async Task HandleAsync(PacketBase packet, PlayerSession session)
        {
            try
            {
                var jsonString = packet.GetPayloadAsString();
                var request = JsonSerializer.Deserialize<ChatRequest>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (request == null || string.IsNullOrWhiteSpace(request.Message))
                    return;

                // Gönderen bilgileri
                int senderId = session.PlayerId ?? 0;
                string senderName = session.Username ?? "Unknown"; // Sadece LOG kaydı için kullanacağız

                // Log nesnesini hazırlayalım (Henüz kaydetmiyoruz)
                var chatLog = new ChatLog
                {
                    SenderPlayerId = senderId,
                    SenderName = senderName,
                    Message = request.Message,
                    Timestamp = DateTime.UtcNow,
                    Type = request.TargetPlayerId.HasValue && request.TargetPlayerId.Value > 0 ? "Private" : "Global"
                };

                // --- SENARYO 1: ÖZEL MESAJ (PM) ---
                if (request.TargetPlayerId.HasValue && request.TargetPlayerId.Value > 0)
                {
                    // Hedefi ID ile bul
                    var targetSession = _sessionManager.GetSessionByPlayerId(request.TargetPlayerId.Value);

                    if (targetSession != null)
                    {
                        // Log için alıcı bilgilerini doldur
                        chatLog.ReceiverPlayerId = request.TargetPlayerId.Value;
                        chatLog.ReceiverName = targetSession.Username; // Logda isim görünmesi iyidir

                        // A) Hedefe Gönder: (SenderPlayerId = Gönderen)
                        // Client bunu "[Private] GönderenAdı: Mesaj" olarak yorumlar.
                        await SendChatPacket(targetSession, senderId, request.Message, "Private");

                        // B) Kendine Gönder: (SenderPlayerId = ALICI)
                        // Type = "PrivateSent" yapıyoruz. 
                        // Client bunu "[Private] To AlıcıAdı: Mesaj" olarak yorumlar.
                        await SendChatPacket(session, request.TargetPlayerId.Value, request.Message, "PrivateSent");
                    }
                    else
                    {
                        // Hedef bulunamadı veya offline -> Sistem mesajı dön (ID: 0)
                        await SendChatPacket(session, 0, "Player not found or offline.", "System");

                        // Hata durumunda logu kaydetmek istemeyebilirsin, return edebiliriz.
                        // Veya "Failed PM" olarak loglayabilirsin. Şimdilik kaydediyoruz.
                    }
                }
                // --- SENARYO 2: GLOBAL MESAJ ---
                else
                {
                    // Herkese Gönder
                    var response = new ChatResponse
                    {
                        SenderPlayerId = senderId, // İsim yok, client kendi listesinden bulacak
                        Message = request.Message,
                        Type = "Global"
                    };

                    var responsePacket = CreatePacket(response);

                    // Broadcast
                    await _sessionManager.BroadcastTcpAsync(responsePacket);

                    Console.WriteLine($"[Chat] {senderName}: {request.Message}");
                }

                // --- VERİTABANINA KAYIT (LOGGING) ---
                try
                {
                    _context.ChatLogs.Add(chatLog);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChatLog] Failed to save log: {ex.Message}");
                    // Hata olsa bile client'a hissettirme, oyun devam etsin.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Chat] Error: {ex.Message}");
            }
        }

        // Yardımcı Metod: Paketi Byte Dizisine Çevir
        private TCPPacket CreatePacket(ChatResponse response)
        {
            string json = JsonSerializer.Serialize(response);
            return new TCPPacket((byte)PacketId, 0, Encoding.UTF8.GetBytes(json));
        }

        // Yardımcı Metod: Tek Kişiye Paket Gönder
        private async Task SendChatPacket(PlayerSession session, int senderId, string message, string type)
        {
            var response = new ChatResponse
            {
                SenderPlayerId = senderId,
                Message = message,
                Type = type
            };
            var packet = CreatePacket(response);
            await session.SendTcpAsync(packet);
        }
    }
}