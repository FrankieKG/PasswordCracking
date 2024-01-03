using System.Text;
using System.Diagnostics;


namespace PasswordCracking
{
  class BruteForceCracker
  {
    private Stopwatch stopwatch = new Stopwatch();
    private int passwordsFound = 0;
    private readonly object lockObject = new object();
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
    public void CrackPasswordGPU(string hashedPassword, int maxLength, int totalPasswords)
    {
      var combinations = GenerateCombinations(characterSet, maxLength);
      /*KERNEL-BASED
      // Calculate total combinations
      ulong totalCombinations = (ulong)Math.Pow(characterSet.Length, maxLength);

      // Generate passwords using GPU
      kernel.ExecutePasswordGenerationKernel(characterSet.ToCharArray(), (uint)maxLength, totalCombinations, out string[] generatedPasswords);
      */
      int batchSize = 500;
      stopwatch.Start();

      //for (int i = 0; i < generatedPasswords.Length; i += batchSize)
      for (int i = 0; i < combinations.Count; i += batchSize)
      {
        lock (lockObject)
        {
          if (passwordsFound >= totalPasswords)
          {
            stopwatch.Stop();
            Console.WriteLine($"Total cracking time: {stopwatch.ElapsedMilliseconds} ms");
            return; 
          }
        }
        int batchCount = Math.Min(batchSize, combinations.Count - i);
        var batch = combinations.GetRange(i, batchCount);
        /* KERNEL-BASED
        int batchCount = Math.Min(batchSize, generatedPasswords.Length - i);
        var batch = generatedPasswords.Skip(i).Take(batchCount).ToList();
        */
        ProcessBatch(batch, hashedPassword, (uint)batchCount, totalPasswords);
      }
      
      Console.WriteLine($"Total cracking time: {stopwatch.ElapsedMilliseconds} ms");

    }

    private void ProcessBatch(List<string> batch, string targetHash, uint batchSize, int totalPasswords)
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

          lock (lockObject)
          {
            passwordsFound++;
            Console.WriteLine($"Password found by bruteforcecracker: {keys[i]}");
            Console.WriteLine($"Hash: {hashString}");

            if (passwordsFound >= totalPasswords)
            {

              return;
            }
          }

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