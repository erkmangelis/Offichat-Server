using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Offichat.Network
{
    public class TCPListenerManager
    {
        private readonly TcpListener _listener;

        public event Action<TcpClient, TCPPacket>? OnPacketReceived;
        public event Action<TcpClient>? OnClientConnected;
        public event Action<TcpClient>? OnClientDisconnected;

        public TCPListenerManager(string ip, int port)
        {
            _listener = new TcpListener(IPAddress.Parse(ip), port);
        }

        public void Start(CancellationToken cancellationToken)
        {
            _listener.Start();
            Task.Run(() => AcceptLoop(cancellationToken), cancellationToken);
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private async Task AcceptLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    OnClientConnected?.Invoke(client);
                    _ = Task.Run(() => HandleClient(client, cancellationToken), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Token iptal edildiğinde loop'tan çık
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TCP] AcceptLoop error: {ex.Message}");
                }
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
        {
            using var stream = client.GetStream();
            var buffer = new byte[4096];

            try
            {
                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) break; // disconnect

                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, 0, data, 0, bytesRead);

                    try
                    {
                        var packet = TCPPacket.FromBytes(data);
                        OnPacketReceived?.Invoke(client, packet);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TCP] Failed to parse packet: {ex.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Token iptal edildiğinde client loop'tan çık
            }
            finally
            {
                OnClientDisconnected?.Invoke(client);
                client.Close();
            }
        }
    }
}
