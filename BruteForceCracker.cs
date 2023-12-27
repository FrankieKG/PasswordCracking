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

      int batchSize = 1000; 

      for (int i = 0; i < combinations.Count; i += batchSize)
      {
        var batch = combinations.GetRange(i, Math.Min(batchSize, combinations.Count - i));
        ProcessBatch(batch, hashedPassword, (uint)batchSize);
      }

    }

    private void ProcessBatch(List<string> batch, string targetHash, uint batchSize)
    {
      string[] keys = batch.ToArray();

      kernel.ExecuteSha256Kernel(keys, batchSize, out byte[] outputData);

     
      for (int i = 0; i < keys.Length; i++)
      {

        byte[] hashBytes = new byte[64];
        Array.Copy(outputData, i * 64, hashBytes, 0, 64);


        string hashString = Encoding.UTF8.GetString(hashBytes, 0, 64);


        if (hashString.Equals(targetHash, StringComparison.OrdinalIgnoreCase))
        {
          Console.WriteLine($"Password found by bruteforcecracker: {keys[i]}");
          Console.WriteLine($"Hash: {hashString}");

        }
      }

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
       // Console.WriteLine($"Generated Password Guess: {current}");
        return;
      }

      foreach (var c in chars)
      {
        var next = current + c;
        GenerateCombinationsRecursive(list, next, chars, maxLen);
      }
    }
  }
}