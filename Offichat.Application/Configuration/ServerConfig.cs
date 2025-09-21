using System.IO;
using System.Text.Json;

namespace Offichat.Application
{
    public class ServerConfig
    {
        public string IP { get; set; } = "127.0.0.1";
        public int TCPPort { get; set; } = 9000;
        public int UDPPort { get; set; } = 9001;

        public static ServerConfig Load(string path)
        {
            var json = File.ReadAllText(path);
            var doc = JsonSerializer.Deserialize<JsonDocument>(json);
            var server = doc!.RootElement.GetProperty("Server");
            return new ServerConfig
            {
                IP = server.GetProperty("IP").GetString() ?? "127.0.0.1",
                TCPPort = server.GetProperty("TCPPort").GetInt32(),
                UDPPort = server.GetProperty("UDPPort").GetInt32()
            };
        }
    }
}
