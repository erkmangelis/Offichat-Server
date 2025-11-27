using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Offichat.Domain.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Required, MaxLength(30)]
        public string DisplayName { get; set; }

        [MaxLength(50)]
        public string JobTitle { get; set; } // "Backend Developer"

        [MaxLength(100)]
        public string StatusMessage { get; set; } // String (Esnek) - "Kod yazıyor..."

        // Görünüm verisi (JSONB)
        // Client tarafında: { "hair": 1, "color": "blue" }
        [Column(TypeName = "jsonb")]
        public string AppearanceData { get; set; }

        public User User { get; set; }
    }
}