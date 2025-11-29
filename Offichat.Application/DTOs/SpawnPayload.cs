using Offichat.Application.Enums;

namespace Offichat.Application.DTOs
{
    public class SpawnPayload
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; }
        public object Appearance { get; set; }

        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public AnimationState Anim { get; set; } = AnimationState.Idle;
        public Direction Direction { get; set; } = Direction.Right;
    }
}