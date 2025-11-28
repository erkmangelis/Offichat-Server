using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Offichat.Infrastructure.Persistence
{
    public class OffichatDbContextFactory : IDesignTimeDbContextFactory<OffichatDbContext>
    {
        public OffichatDbContext CreateDbContext(string[] args)
        {
            // 1. appsettings.json dosyasının yerini bul (Api projesinin altında)
            // Not: Terminali hangi klasörde açtığına göre path değişebilir, en garantisi Api klasörüne gitmektir.
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../Offichat.Api");

            // Eğer doğrudan Api klasöründeysen (ki genelde öyle olur), path'i düzelt.
            if (!Directory.Exists(basePath))
            {
                basePath = Directory.GetCurrentDirectory();
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json")
                .Build();

            // 2. Connection String'i al
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 3. DbContext'i oluştur ve döndür
            var optionsBuilder = new DbContextOptionsBuilder<OffichatDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new OffichatDbContext(optionsBuilder.Options);
        }
    }
}