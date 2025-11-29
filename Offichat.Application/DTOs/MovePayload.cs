using Offichat.Application.Enums;

namespace Offichat.Application.DTOs
{
    public class MovePayload
    {
        public int PlayerId { get; set; }

        // Koordinatlar
        public float X { get; set; }
        public float Y { get; set; }

        // Görünüm Durumu
        public AnimationState Anim { get; set; } = AnimationState.Idle;
        public Direction Direction { get; set; } = Direction.Right;
    }
}
