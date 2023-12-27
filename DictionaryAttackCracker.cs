
namespace PasswordCracking
{
  class DictionaryAttackCracker
  {
    public static void CrackPassword(string hashedPassword, string[] dictionary)
    {
      Parallel.ForEach(dictionary, (word) =>
      {
        if (PasswordHasher.HashPassword(word) == hashedPassword)
        {
          Console.WriteLine($"Password found by DictionaryAttack: {word}");

        }
      });
    }
  }
}
