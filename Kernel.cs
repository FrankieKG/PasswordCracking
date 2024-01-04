using System.Text;
using OpenCL.Net;

namespace PasswordCracking
{
  class Kernel
  {
    private Context context;
    private CommandQueue commandQueue;
    private Program program;
    private Device device;

    public void Initialize()
    {
      ErrorCode error;

      uint numPlatforms;
      Cl.GetPlatformIDs(0, null, out numPlatforms);

      Platform[] platforms = new Platform[numPlatforms];
      Cl.GetPlatformIDs(numPlatforms, platforms, out numPlatforms);
      Platform platform = platforms[0];

      Device[] devices = Cl.GetDeviceIDs(platform, DeviceType.Gpu, out error);
      CheckError(error);
      device = devices[0];

      context = Cl.CreateContext(null, 1, new[] { device }, null, IntPtr.Zero, out error);
      CheckError(error);


      commandQueue = Cl.CreateCommandQueue(context, device, (CommandQueueProperties)0, out error);
      CheckError(error);

      string programSource = File.ReadAllText(@"../../../sha256.cl");

  
      program = Cl.CreateProgramWithSource(context, 1, new[] { programSource }, null, out error);
      CheckError(error);

      error = Cl.BuildProgram(program, 1, new[] { device }, string.Empty, null, IntPtr.Zero);
      CheckError(error);
    }
  
    public void ExecuteSha256Kernel(string[] keys, uint batchSize, out byte[] outputData)
    {
      ErrorCode error;

      byte[] formattedInputData = PrepareInputData(keys, batchSize);

      IMem inputBuffer = Cl.CreateBuffer(context, MemFlags.ReadOnly | MemFlags.CopyHostPtr, formattedInputData, out error);
      CheckError(error);

      
      outputData = new byte[keys.Length * 64];
      IMem outputBuffer = Cl.CreateBuffer(context, MemFlags.WriteOnly, outputData.Length, out error);
      CheckError(error);


      var kernel = Cl.CreateKernel(program, "sha256hash_multiple_kernel", out error);
      CheckError(error);

 
      Cl.SetKernelArg(kernel, 0, batchSize); 
      Cl.SetKernelArg(kernel, 1, inputBuffer); 
      Cl.SetKernelArg(kernel, 2, outputBuffer);

      Cl.EnqueueNDRangeKernel(commandQueue, kernel, 1, null, new[] { (IntPtr)keys.Length }, null, 0, null, out var _);
      var readEvent = new Event();
      Cl.EnqueueReadBuffer(commandQueue, outputBuffer, Bool.False, IntPtr.Zero, outputData.Length, outputData, 0, null, out readEvent);

      Cl.WaitForEvents(1, new[] { readEvent });
      Cl.ReleaseEvent(readEvent);

      Cl.ReleaseKernel(kernel);
      Cl.ReleaseMemObject(inputBuffer);
      Cl.ReleaseMemObject(outputBuffer);
    }
    

    private void CheckError(ErrorCode err)
    {
      if (err != ErrorCode.Success)
      {
        InfoBuffer buildLog = Cl.GetProgramBuildInfo(program, device, ProgramBuildInfo.Log, out ErrorCode buildErr);
        if (buildErr != ErrorCode.Success)
        {
          throw new Exception($"Failed to get build log: {buildErr}");
        }

        string buildLogStr = buildLog.ToString();
        Console.WriteLine(buildLogStr);

        throw new Exception($"OpenCL Error: {err}");
      }
    }
    
    public static byte[] PrepareInputData(string[] keys, uint batchSize)
    {
      List<byte> formattedData = new List<byte>();
      foreach (var key in keys)
      {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length < batchSize)
        {
          formattedData.AddRange(keyBytes);
          formattedData.AddRange(new byte[batchSize - keyBytes.Length]);
        }
        else
        {
          formattedData.AddRange(keyBytes.Take((int)batchSize));
        }
      }

      return formattedData.ToArray();
    }

    public void ExecutePasswordGenerationKernel(char[] charset, uint maxLength, ulong totalCombinations, out string[] generatedPasswords)
    {
      ErrorCode error;


      byte[] charsetBytes = Encoding.UTF8.GetBytes(charset);
      IMem charsetBuffer = Cl.CreateBuffer(context, MemFlags.ReadOnly | MemFlags.CopyHostPtr, charsetBytes, out error);
      CheckError(error);


      byte[] outputData = new byte[totalCombinations * maxLength];
      IMem outputBuffer = Cl.CreateBuffer(context, MemFlags.WriteOnly, outputData.Length, out error);
      CheckError(error);


      var kernel = Cl.CreateKernel(program, "generate_passwords", out error);
      CheckError(error);
      Cl.SetKernelArg(kernel, 0, outputBuffer);
      Cl.SetKernelArg(kernel, 1, charsetBuffer);
      Cl.SetKernelArg(kernel, 2, (uint)charset.Length);
      Cl.SetKernelArg(kernel, 3, maxLength);
      Cl.SetKernelArg(kernel, 4, totalCombinations);

      Cl.EnqueueNDRangeKernel(commandQueue, kernel, 1, null, new[] { (IntPtr)totalCombinations }, null, 0, null, out var _);

      Cl.EnqueueReadBuffer(commandQueue, outputBuffer, Bool.True, IntPtr.Zero, outputData.Length, outputData, 0, null, out var _);

      generatedPasswords = ConvertOutputToStringArray(outputData, maxLength, totalCombinations);

      Cl.ReleaseKernel(kernel);
      Cl.ReleaseMemObject(charsetBuffer);
      Cl.ReleaseMemObject(outputBuffer);
    }

    private static string[] ConvertOutputToStringArray(byte[] outputData, uint maxLength, ulong totalCombinations)
    {
      string[] passwords = new string[totalCombinations];
      for (ulong i = 0; i < totalCombinations; i++)
      {
        passwords[i] = Encoding.UTF8.GetString(outputData, (int)(i * maxLength), (int)maxLength).TrimEnd('\0');
      }
      return passwords;
    }
  }
}
