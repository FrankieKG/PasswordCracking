using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordCracking
{
  class DictionaryAttackCracker
  {
    public static void CrackPassword(string hashedPassword, string[] dictionary)
    {
      /*foreach(var word in dictionary) 
      {
        if(PasswordHasher.HashPassword(word) == hashedPassword) 
        {
          Console.WriteLine($"Password found by DictionaryAttack: {word}");
          return;
        }
      }*/

      Parallel.ForEach(dictionary, (word) =>
      {
        if (PasswordHasher.HashPassword(word) == hashedPassword)
        {
          Console.WriteLine($"Password found by DictionaryAttack: {word}");
          // Add logic to stop further processing if needed
        }
      });
    }
  }
}
