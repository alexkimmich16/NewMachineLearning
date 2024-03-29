using UnityEngine;
using System.Diagnostics;
using System.IO;
public static class RunPythonScript
{
    public static void ExecutePythonScript(string scriptPath)
    {
        return;
        
        ProcessStartInfo start = new ProcessStartInfo();
        start.FileName = "python";
        start.Arguments = string.Format("\"{0}\"", scriptPath);
        start.UseShellExecute = false; // Do not use OS shell
        start.CreateNoWindow = true; // Do not create a window
        start.RedirectStandardOutput = true; // Any output, generated by the script, will be redirected back
        start.RedirectStandardError = true;


        using (Process process = Process.Start(start))
        {
            using (StreamReader reader = process.StandardOutput)
            {
                string stderr = process.StandardError.ReadToEnd(); // Here are the exceptions from our Python script
                string result = reader.ReadToEnd(); // Here is the result of StdOut(for example: print "test")
                UnityEngine.Debug.Log(result);
                UnityEngine.Debug.LogError(stderr);
            }
        }

        /*


        Process process = new Process();
        process.StartInfo = start;
        process.Start();

        string output = process.StandardOutput.ReadToEnd(); // Read the output from the script
        string error = process.StandardError.ReadToEnd();   // Read any error message

        if (!string.IsNullOrEmpty(output))
            UnityEngine.Debug.Log(output);

        if (!string.IsNullOrEmpty(error))
            UnityEngine.Debug.LogError(error);

        process.WaitForExit();
        */


    }
}
