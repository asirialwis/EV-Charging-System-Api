using System;
using System.Linq;
using System.Text;

namespace EVChargingSystem.WebAPI.Utils
{
    public static class PasswordGenerator
    {
        private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*";
        
        public static string GenerateTemporaryPassword(int length = 12)
        {
            var random = new Random();
            var password = new string(Enumerable.Repeat(Chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
            
            return password;
        }
    }
}