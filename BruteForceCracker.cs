﻿using System;
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

      int batchSize = maxLength; // Adjust the batch size as needed

      for (int i = 0; i < combinations.Count; i += batchSize)
      {
        var batch = combinations.GetRange(i, Math.Min(batchSize, combinations.Count - i));
        ProcessBatch(batch, hashedPassword);
      }
    }
    private void ProcessBatch(List<string> batch, string targetHash)
    {
      string[] keys = batch.ToArray();
      uint keyLength = (uint)keys[0].Length;

      kernel.ExecuteSha256Kernel(keys, keyLength, out byte[] outputData);

     // Console.WriteLine($"Output data length: {outputData.Length}");

      for (int i = 0; i < keys.Length; i++)
      {
      //  Console.WriteLine($"Value in key array {i} {keys[i]}");
        // Adjust to read 64 bytes for each hash
        byte[] hashBytes = new byte[64];
        Array.Copy(outputData, i * 64, hashBytes, 0, 64);

        // Convert to string and take only the first 64 characters
        string hashString = Encoding.UTF8.GetString(hashBytes, 0, 64);

       // Console.WriteLine($"Hash for {keys[i]}: {hashString}");

        if (hashString.Equals(targetHash, StringComparison.OrdinalIgnoreCase))
        {
          Console.WriteLine($"Password found by BruteForce: {keys[i]}");
          return;
        }
      }
     // Console.WriteLine($"targethash: {targetHash}");
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
      //  Console.WriteLine($"Generated Password Guess: {current}");
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