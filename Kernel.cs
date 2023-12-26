using System;
using System.IO;
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

      // Get the number of platforms
      uint numPlatforms;
      Cl.GetPlatformIDs(0, null, out numPlatforms);

      // Get the platforms
      Platform[] platforms = new Platform[numPlatforms];
      Cl.GetPlatformIDs(numPlatforms, platforms, out numPlatforms);
      Platform platform = platforms[0];

      // Get the first GPU device
      Device[] devices = Cl.GetDeviceIDs(platform, DeviceType.Gpu, out error);
      CheckError(error);
      device = devices[0];

      // Create a context for the GPU device
      context = Cl.CreateContext(null, 1, new[] { device }, null, IntPtr.Zero, out error);
      CheckError(error);

      // Create a command queue for the GPU device
      commandQueue = Cl.CreateCommandQueue(context, device, (CommandQueueProperties)0, out error);
      CheckError(error);

      // Read the OpenCL program source
      string programSource = File.ReadAllText("C:\\Users\\bajsk\\source\\repos\\PasswordCrackingtest3\\PasswordCracking\\sha256Copy.cl");

      // Create and build the OpenCL program
      program = Cl.CreateProgramWithSource(context, 1, new[] { programSource }, null, out error);
      CheckError(error);

      error = Cl.BuildProgram(program, 1, new[] { device }, string.Empty, null, IntPtr.Zero);
      CheckError(error);
    }
  
    public void ExecuteSha256Kernel(string[] keys, uint keyLength, out byte[] outputData)
    {
      ErrorCode error;

      // Prepare input data
      byte[] formattedInputData = PrepareInputData(keys, keyLength);
      foreach (byte inputData in formattedInputData) { Console.WriteLine($"formattedinputdata: {inputData}"); }
      // Create input buffer
      IMem inputBuffer = Cl.CreateBuffer(context, MemFlags.ReadOnly | MemFlags.CopyHostPtr, formattedInputData, out error);
      CheckError(error);

      // Output data for each key (32 bytes for each SHA-256 hash)
      outputData = new byte[keys.Length * 64 ];
      IMem outputBuffer = Cl.CreateBuffer(context, MemFlags.WriteOnly, outputData.Length, out error);
      CheckError(error);

      // Load the kernel from the program
      var kernel = Cl.CreateKernel(program, "sha256hash_multiple_kernel", out error);
      CheckError(error);

      // Set kernel arguments
      Cl.SetKernelArg(kernel, 0, keyLength); // Length of each key
      Cl.SetKernelArg(kernel, 1, inputBuffer); // Input data
      Cl.SetKernelArg(kernel, 2, outputBuffer); // Output data

      // Execute the kernel
      Cl.EnqueueNDRangeKernel(commandQueue, kernel, 1, null, new[] { (IntPtr)keys.Length }, null, 0, null, out var _);

      // Read the results
      Cl.EnqueueReadBuffer(commandQueue, outputBuffer, Bool.True, IntPtr.Zero, outputData.Length, outputData, 0, null, out var _);

      // Release resources
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

    public byte[] PrepareInputData(string[] keys, uint keyLength)
    {
      List<byte> formattedData = new List<byte>();
      keyLength = 2;
      foreach (var key in keys)
      {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key); // Convert the key to bytes

        // Ensure the keyBytes array is exactly keyLength bytes
        if (keyBytes.Length < keyLength)
        {
          // Resize the array to keyLength and fill with zeros
          Array.Resize(ref keyBytes, (int)keyLength);
        }

        // Add the resized keyBytes to the list
        formattedData.AddRange(keyBytes);
      }

      return formattedData.ToArray();
    }




  }
}
