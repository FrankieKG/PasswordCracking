using System.Text;
using System.Security.Cryptography;

namespace PasswordCracking
{
  class PasswordHasher
  {
    public static string HashPassword(string password)
    {
      using (SHA256 sha256 = SHA256.Create())
      {
        byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
      }
    }

  }
}
