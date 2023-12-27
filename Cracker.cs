using System;
using System.Text;

namespace PasswordCracking
{
  class Cracker
  {
    public static void TestHashConsistency(string password)
    {
      string hashedPasswordCSharp = PasswordHasher.HashPassword(password);
      Console.WriteLine($"C# Hashed Password: {hashedPasswordCSharp}");

      // Initialize the kernel
      Kernel kernel = new Kernel();
      kernel.Initialize();

      // Execute the kernel with the password
      string[] keys = { password };
      uint keyLength = (uint)password.Length;
      kernel.ExecuteSha256Kernel(keys, keyLength, out byte[] outputData);

      // The outputData contains the hash in hex format
       string hashedPasswordOpenCL = Encoding.UTF8.GetString(outputData);
      //string hashedPasswordOpenCL = ByteArrayToHexString(outputData); //does not work, the result is completely wrong

      Console.WriteLine($"CL Hashed Password: {hashedPasswordOpenCL}");
      Console.WriteLine($"C# Hash Length: {hashedPasswordCSharp.Length}");
      Console.WriteLine($"CL Hash Length: {hashedPasswordOpenCL.Length}");

      // Compare the hashes
      if (hashedPasswordCSharp.Equals(hashedPasswordOpenCL, StringComparison.OrdinalIgnoreCase))
      {
        Console.WriteLine("Success: Hashes match.");
      }
      else
      {
        Console.WriteLine("Error: Hashes do not match.");
      }
    }

    static void Main(string[] args) 
    {
      TestHashConsistency("b");
      /* Console.WriteLine("Testing Password Generation and Hashing");

       string simplePassword = PasswordGenerator.GenerateSimplePassword(10);
       Console.WriteLine($"Simple Password: {simplePassword}");
       Console.WriteLine($"Hashed: {PasswordHasher.HashPassword(simplePassword)}");

       string complexPassword = PasswordGenerator.GenerateComplexPassword(10);
       Console.WriteLine($"Complex Password: {complexPassword}");
       Console.WriteLine($"Hashed: {PasswordHasher.HashPassword(complexPassword)}");
      */

      List<string> hashedPasswords = new List<string>();

      int numberOfPasswords = 3;
      int passWordLength =1;

      for (int i = 0; i < numberOfPasswords; i++)
      {
        string password = PasswordGenerator.GenerateComplexPassword(passWordLength);
        //string password = "Abca";
        Console.WriteLine($"Password unhashed: {password}");
        string hashed = PasswordHasher.HashPassword(password);
        Console.WriteLine($"Password hashed: {hashed}");
        hashedPasswords.Add(hashed);
      }

      string[] commonPasswords = { "password", "123456", "qwerty", "abc123", "admin" };

      
      // GPU cracking
      // Create an instance of BruteForceCracker
      int maxLength = passWordLength; // Maximum length of password to attempt
      BruteForceCracker bruteForceCracker = new BruteForceCracker(characterSet: "abc", maxLength);

      foreach (var hashedPassword in hashedPasswords)
      {
        bruteForceCracker.CrackPasswordGPU(hashedPassword, maxLength);
        // Assuming DictionaryAttackCracker.CrackPassword still takes 2 arguments
       // DictionaryAttackCracker.CrackPassword(hashedPassword, commonPasswords);
      }
      
    }
    public static string ByteArrayToHexString(byte[] byteArray)
    {
      StringBuilder hex = new StringBuilder(byteArray.Length * 2);
      foreach (byte b in byteArray)
        hex.AppendFormat("{0:x2}", b);
      return hex.ToString();
    }
  }
}

