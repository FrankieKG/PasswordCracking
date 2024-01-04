using System.Text;

namespace PasswordCracking
{
  class Cracker
  {
    public static void TestHashConsistency(string password)
    {
      string hashedPasswordCSharp = PasswordHasher.HashPassword(password);
      Console.WriteLine($"C# Hashed Password: {hashedPasswordCSharp}");

   
      Kernel kernel = new Kernel();
      kernel.Initialize();

      string[] keys = { password };
      uint keyLength = (uint)password.Length;
      kernel.ExecuteSha256Kernel(keys, keyLength, out byte[] outputData);

       string hashedPasswordOpenCL = Encoding.UTF8.GetString(outputData);
 

      Console.WriteLine($"CL Hashed Password: {hashedPasswordOpenCL}");
      Console.WriteLine($"C# Hash Length: {hashedPasswordCSharp.Length}");
      Console.WriteLine($"CL Hash Length: {hashedPasswordOpenCL.Length}");


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
      bool gpuCrack = false;
     // TestHashConsistency("b");
      List<string> hashedPasswords = new List<string>();

      int numberOfPasswords = 1;
      int passWordLength = 5;

      for (int i = 0; i < numberOfPasswords; i++)
      {
        string password = PasswordGenerator.GenerateComplexPassword(passWordLength);
        Console.WriteLine($"Password unhashed: {password}");
        string hashed = PasswordHasher.HashPassword(password);
        Console.WriteLine($"Password hashed: {hashed}");
        hashedPasswords.Add(hashed);
      }

      
      int maxLength = passWordLength;
      BruteForceCracker bruteForceCracker = new BruteForceCracker(characterSet: "abc123", maxLength);
      int totalPasswords = hashedPasswords.Count;
      if(gpuCrack) 
      { 
        Console.WriteLine("Using GPU-Cracker");
        foreach (var hashedPassword in hashedPasswords)
        {
          bruteForceCracker.CrackPasswordGPU(hashedPassword, maxLength, totalPasswords);
        }

      }
      if(gpuCrack == false) 
      { 
        Console.WriteLine("Using CPU-Cracker");
        foreach (var hashedPassword in hashedPasswords)
        {
          BruteForceCracker.CrackPassword(hashedPassword, maxLength);
        }
      }
      
    }

  }
}

