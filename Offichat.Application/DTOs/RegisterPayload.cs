using System.ComponentModel.DataAnnotations;

namespace Offichat.Application.DTOs
{
    public class RegisterPayload
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string DisplayName { get; set; }

        // Karakter görünüm verisi (Godot'tan ne gelirse veritabanına aynen basacağız)
        public object Appearance { get; set; }
    }
}