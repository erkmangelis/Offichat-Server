using System;
using System.Collections.Generic;
using System.Text;

namespace Offichat.Application.DTOs
{
    // İstemciden Gelen (Client -> Server)
    public class ChatRequest
    {
        public string Message { get; set; }

        // Eğer 0 veya null ise Global Chat kabul edilir.
        public int? TargetPlayerId { get; set; }
    }

    // Sunucudan Giden (Server -> Client)
    public class ChatResponse
    {
        public int SenderPlayerId { get; set; } // Gönderen Karakterin ID'si
        public string Message { get; set; }
        public string Type { get; set; } // "Global", "Private", "System"
    }
}
