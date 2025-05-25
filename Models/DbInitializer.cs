using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Do_An_Tot_Nghiep.Models;

namespace Do_An_Tot_Nghiep.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                var admin = new User
                {
                    Username = "admin",
                    Password = HashPassword("admin123"),
                    Email = "admin@example.com",
                    Role = "Admin",
                    CreatedAt = DateTime.Now
                };

                context.Users.Add(admin);
                context.SaveChanges();
            }
        }

        private static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }
}
