using System.IO;
using System.Text.Json;

namespace Offichat.Application
{
    public class ServerConfig
    {
        public string IP { get; set; } = "127.0.0.1";
        public int TCPPort { get; set; } = 9000;
        public int UDPPort { get; set; } = 9001;
        public int AfkTimeoutSeconds { get; set; } = 60;
        public int SessionTimeoutSeconds { get; set; } = 300;
        public string ConnectionString { get; set; }

        public static ServerConfig Load(string path)
        {
            var json = File.ReadAllText(path);
            var doc = JsonSerializer.Deserialize<JsonDocument>(json);

            var root = doc!.RootElement;
            var server = root.GetProperty("Server");

            string connString = "";
            if (root.TryGetProperty("ConnectionStrings", out var connSection))
            {
                if (connSection.TryGetProperty("DefaultConnection", out var defaultConn))
                {
                    connString = defaultConn.GetString() ?? "";
                }
            }

            return new ServerConfig
            {
                IP = server.GetProperty("IP").GetString() ?? "127.0.0.1",
                TCPPort = server.GetProperty("TCPPort").GetInt32(),
                UDPPort = server.GetProperty("UDPPort").GetInt32(),
                AfkTimeoutSeconds = server.GetProperty("AfkTimeoutSeconds").GetInt32(),
                SessionTimeoutSeconds = server.GetProperty("SessionTimeoutSeconds").GetInt32(),
                ConnectionString = connString
            };
        }
    }
}