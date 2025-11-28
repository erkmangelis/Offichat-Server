using Offichat.Domain.Interfaces;

namespace Offichat.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            // Şifreyi tuzlayarak (salting) hashle
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            // Girilen şifre hash ile uyuşuyor mu?
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}