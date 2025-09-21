using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Offichat.Network
{
    public class UDPListenerManager
    {
        private readonly UdpClient _udpClient;
        public UdpClient UdpClient => _udpClient;

        public event Action<IPEndPoint, UDPPacket>? OnPacketReceived;

        public UDPListenerManager(string ip, int port)
        {
            _udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(ip), port));
        }

        public void Start(CancellationToken cancellationToken)
        {
            Task.Run(() => ReceiveLoop(cancellationToken), cancellationToken);
        }

        public void Stop()
        {
            _udpClient.Close();
        }

        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(cancellationToken);
                    var data = result.Buffer;

                    try
                    {
                        var packet = UDPPacket.FromBytes(data);
                        OnPacketReceived?.Invoke(result.RemoteEndPoint, packet);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[UDP] Failed to parse packet: {ex.Message}");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Token iptal edildiğinde loop'tan çık
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UDP] ReceiveLoop error: {ex.Message}");
                }
            }
        }

        public void SendPacket(IPEndPoint endPoint, UDPPacket packet)
        {
            var data = packet.ToBytes();
            _udpClient.Send(data, data.Length, endPoint);
        }
    }
}
