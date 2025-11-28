namespace Offichat.Application.DTOs
{
    public class SpawnPayload
    {
        public int PlayerId { get; set; }
        public string DisplayName { get; set; }
        public object Appearance { get; set; } // Godot'tan gelen JSON

        // Ofis kapı girişi koordinatı
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
    }
}