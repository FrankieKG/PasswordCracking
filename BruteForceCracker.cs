using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PasswordCracking
{
  class BruteForceCracker
  {
    private Kernel kernel;
    private string characterSet;
    private int maxLength;

    public BruteForceCracker(string characterSet, int maxLength)
    {
      this.characterSet = characterSet;
      this.maxLength = maxLength;
      kernel = new Kernel();
      kernel.Initialize();
    }

    // GPU
    public void CrackPasswordGPU(string hashedPassword, int maxLength)
    {
      var combinations = GenerateCombinations(characterSet, maxLength);

      int batchSize = 1000; // Adjust the batch size as needed

      for (int i = 0; i < combinations.Count; i += batchSize)
      {
        var batch = combinations.GetRange(i, Math.Min(batchSize, combinations.Count - i));
        ProcessBatch(batch, hashedPassword);
      }
    }
    // vad gör processbatch egentligen? är dessa stränglistor fragment av gissningen, eller fragment av hashen?
    private void ProcessBatch(List<string> batch, string targetHash)
    {
      // Convert batch of strings to array of strings
      string[] keys = batch.ToArray();

      // Execute kernel with batch data
      uint keyLength = 1; // Adjust this based on your säger hur långa gissningarna ska vara kernel's expected input length

      kernel.ExecuteSha256Kernel(keys, keyLength, out byte[] outputData);

      // Process output data
      for (int i = 0; i < keys.Length; i++)
      {
        // Extract hash for each password in batch
        byte[] hashBytes = new byte[32]; // SHA-256 hash size
        // Array.Copy(outputData, i * 32, hashBytes, 0, 32); // Correct index based on hash size
        Console.WriteLine($"Raw Hash Bytes for {keys[i]}: {BitConverter.ToString(hashBytes).Replace("-", "").ToLower()}");
        Console.WriteLine($"output: {outputData[4]}");
        Console.WriteLine($"Length of Hash Bytes: {hashBytes.Length}");
        string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        if (hashString.Equals(targetHash, StringComparison.OrdinalIgnoreCase))
        {
          Console.WriteLine($"Password found by BruteForce: {keys[i]}");
          return; // Match found, exit loop
        }
      }
    }




    private byte[] PrepareBatchInputData(List<string> batch)
    {
      List<byte> formattedData = new List<byte>();
      foreach (var password in batch)
      {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        formattedData.AddRange(passwordBytes);

        // Pad or truncate to match the expected input length
        if (passwordBytes.Length < 32) // Assuming 32 bytes input
        {
          formattedData.AddRange(new byte[32 - passwordBytes.Length]);
        }
        else if (passwordBytes.Length > 32)
        {
          formattedData.RemoveRange(formattedData.Count - (passwordBytes.Length - 32), passwordBytes.Length - 32);
        }
      }

      return formattedData.ToArray();
    }




    private List<string> GenerateCombinations(string chars, int maxLen)
    {
      var list = new List<string>();
      GenerateCombinationsRecursive(list, "", chars, maxLen);
      return list;
    }

    private void GenerateCombinationsRecursive(List<string> list, string current, string chars, int maxLen)
    {
      if (current.Length == maxLen)
      {
        list.Add(current);

        // Print out the current password guess
        Console.WriteLine($"Generated Password Guess: {current}");
        return;
      }

      foreach (var c in chars)
      {
        var next = current + c;
        GenerateCombinationsRecursive(list, next, chars, maxLen);
      }
    }
 

    // CPU Parallel 
    public static void CrackPassword(string hashedPassword, int maxLength)
    {

      var characterSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()";
      Parallel.ForEach(characterSet, (c) =>
      {
        RecurseCrack(c.ToString(), characterSet, maxLength - 1, hashedPassword);
      });

      static void RecurseCrack(string current, string characterSet, int length, string hashedPassword)
      {
        if (length == 0)
        {
          if (PasswordHasher.HashPassword(current) == hashedPassword)
          {
            Console.WriteLine($"Password found by BruteForce: {current}");
            return;
          }
        }
        else
        {
          foreach (char c in characterSet)
          {
            RecurseCrack(current + c, characterSet, length - 1, hashedPassword);
          }
        }
      }
    }
  }
}