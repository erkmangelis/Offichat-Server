using System;
using System.Collections.Generic;
using System.Text;

namespace Offichat.Application.DTOs
{
    public class MovePayload
    {
        public int PlayerId { get; set; }

        // Koordinatlar
        public float X { get; set; }
        public float Y { get; set; }

        // Görünüm Durumu
        public string Anim { get; set; } = "idle"; // Örn: "idle", "walk", "sit"
        public string Direction { get; set; } = "right"; // Örn: "right, "left", "up", "down"
    }
}
