using Offichat.Application.DTOs;
using Offichat.Application.PacketRouting;
using Offichat.Application.Session;
using Offichat.Network;
using System.Text;
using System.Text.Json;

namespace Offichat.Application.Handlers
{
    public class MoveHandler : IPacketHandler
    {
        public ushort PacketId => 2; // Move Packet ID

        private readonly SessionManager _sessionManager;

        public MoveHandler(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public async Task HandleAsync(PacketBase packet, PlayerSession session)
        {
            try
            {
                // UDP paketleri hızlı gelir, hata olursa loglamaya gerek yok (Fire & Forget)
                var jsonString = packet.GetPayloadAsString();

                // 1. Gelen Veriyi Oku
                // Client sadece { X, Y, Anim, FlipH } gönderir.
                var incomingData = JsonSerializer.Deserialize<MovePayload>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (incomingData == null) return;

                // --- GÜVENLİK VE DOĞRULAMA (Opsiyonel) ---
                // Buraya ileride "Hız Kontrolü" (Speed Hack Check) ekleyebiliriz.
                // Şimdilik gelen veriyi güvenilir kabul ediyoruz.

                // --- 2. CACHE GÜNCELLEME ---
                // Sunucu hafızasındaki konumu güncelle
                session.X = incomingData.X;
                session.Y = incomingData.Y;
                session.Anim = incomingData.Anim;
                session.Direction = incomingData.Direction;

                // 3. Yayınlanacak Veriyi Hazırla
                var broadcastData = new MovePayload
                {
                    // ÖNEMLİ: PlayerId'yi session'dan biz ekliyoruz. 
                    // Client "Ben ID:5'im" dese bile inanmayız, session neyse odur.
                    PlayerId = session.PlayerId ?? 0,

                    X = incomingData.X,
                    Y = incomingData.Y,
                    Anim = incomingData.Anim,
                    Direction = incomingData.Direction
                };

                // PlayerId yoksa (Login olmamışsa) yayına gerek yok
                if (broadcastData.PlayerId == 0) return;

                // 3. Herkese Yay (UDP Broadcast)
                string broadcastJson = JsonSerializer.Serialize(broadcastData);

                // Sequence Number (0) ve Timestamp (0) şimdilik varsayılan.
                // İleride Godot tarafında interpolasyon için Timestamp ekleyebiliriz.
                var udpPacket = new UDPPacket(
                    2, // Move Packet ID
                    session.SessionId,
                    Encoding.UTF8.GetBytes(broadcastJson)
                );

                // Kendim hariç herkese gönder (Çünkü ben zaten oradayım)
                await _sessionManager.BroadcastUdpAsync(udpPacket, session.SessionId);
            }
            catch
            {
                // UDP hatalarını yutuyoruz, oyun akışını kesmesin.
            }
        }
    }
}