using System.Text;

namespace PasswordCracking
{
  class PasswordGenerator
  {
    static Random Random = new Random();

    public static string GenerateSimplePassword(int length)
    {
      string[] commonWords = { "password", "hello", "welcome", "abc123", "12345" };
      StringBuilder sb = new StringBuilder();

      while(sb.Length < length) 
      {
        sb.Append(commonWords[Random.Next(commonWords.Length)]);
      }

      return sb.ToString().Substring(0, length);
    }

    public static string GenerateComplexPassword(int length) 
    {

      const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()";
      return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray() );
      
    }

  }
}
