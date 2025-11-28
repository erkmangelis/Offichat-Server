using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Offichat.Domain.Entities
{
    public class ChatLog
    {
        public long Id { get; set; } // Çok fazla kayıt olacağı için 'long' (bigint)

        public int SenderPlayerId { get; set; }
        public string SenderName { get; set; }

        public int? ReceiverPlayerId { get; set; } // Null ise Global mesajdır
        public string? ReceiverName { get; set; }

        public string Message { get; set; }

        [MaxLength(20)]
        public string Type { get; set; } // "Global", "Private"

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
