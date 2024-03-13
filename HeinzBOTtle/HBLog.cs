namespace HeinzBOTtle;

/// <summary>
/// Manages the logging system for this program.
/// </summary>
public sealed class HBLog : IDisposable {

    /// <summary>The interface for writing messages to the log file.</summary>
    public StreamWriter? LogWriter { get; private set; }
    /// <summary>The path indicating the location of the log file in the filesystem.</summary>
    public string LogFilePath { get; }
    /// <summary>Indicates whether the log file was successfully initialized at the start of the program.</summary>
    public bool SuccessfulSetup { get; }
    /// <summary>Controls mutual exclusion for access of the log file.</summary>
    private Semaphore FileSemaphore { get; }

    public HBLog(string logFilePath) {
        LogFilePath = logFilePath;
        FileSemaphore = new Semaphore(1, 1);
        try {
            if (File.Exists(LogFilePath))
                File.Delete(LogFilePath);
            LogWriter = File.AppendText(LogFilePath);
            SuccessfulSetup = true;
        } catch {
            LogWriter = null;
            SuccessfulSetup = false;
        }
    }

    /// <summary>Writes a line to the log with an immediate timestamp and HeinzBOTtle as the message source.</summary>
    /// <param name="message">The message to write, which will be followed by a line termination</param>
    public void Info(string? message) {
        WriteLineToLog(message, "HeinzBOTtle", DateTime.Now);
    }

    /// <summary>Writes a line to the log asynchronously with an immediate timestamp and HeinzBOTtle as the message source.</summary>
    /// <param name="message">The message to write, which will be followed by a line termination</param>
    public async Task InfoAsync(string? message) {
        await WriteLineToLogAsync(message, "HeinzBOTtle", DateTime.Now);
    }

    /// <summary>Writes a line to the log based on the provided arguments.</summary>
    /// <param name="message">The message to write, which will be followed by a line termination</param>
    /// <param name="source">The source of the message for inclusion in the message prefix</param>
    /// <param name="ts">The message timestamp for inclusion in the message prefix</param>
    public void WriteLineToLog(string? message, string source, DateTime ts) {
        if (message == null)
            return;
        ApplyPrefix(ref message, source, ts);
        Console.WriteLine(message);
        if (FileSemaphore.WaitOne(2000)) {
            LogWriter!.WriteLine(message);
            FileSemaphore.Release();
        } else
            Console.WriteLine($"(!) MESSAGE COULD NOT BE LOGGED TO FILE!");
    }

    /// <summary>Writes a line to the log asynchronously based on the provided arguments.</summary>
    /// <param name="message">The message to write, which will be followed by a line termination</param>
    /// <param name="source">The source of the message for inclusion in the message prefix</param>
    /// <param name="ts">The message timestamp for inclusion in the message prefix</param>
    public async Task WriteLineToLogAsync(string? message, string source, DateTime ts) {
        if (message == null)
            return;
        ApplyPrefix(ref message, source, ts);
        Console.WriteLine(message);
        if (FileSemaphore.WaitOne(2000)) {
            await LogWriter!.WriteLineAsync(message);
            FileSemaphore.Release();
        } else
            Console.WriteLine($"(!) MESSAGE COULD NOT BE LOGGED TO FILE!");
    }


    /// <summary>Applies a prefix to the provided log message and adjusts any additional lines in the message to align properly with the first line.</summary>
    /// <param name="message">The raw message to be modified</param>
    /// <param name="source">The source of the message</param>
    /// <param name="ts">The message timestamp</param>
    public void ApplyPrefix(ref string message, string source, DateTime ts) {
        string prefix = $"[{ts.Month}/{ts.Day} {ts.Hour}:{ts.Minute:D2}:{ts.Second:D2}] <{source}> ";
        if (message.Contains('\n')) {
            int paddingLength = prefix.Length + 1;
            char[] padding = new char[paddingLength];
            padding[0] = '\n';
            for (int i = 1; i < paddingLength; i++)
                padding[i] = ' ';
            message = message.Replace("\n", new string(padding));
        }
        message = prefix + message;
    }

    /// <summary>Fluishes the log file's stream and temporarily releases process control over the log file so that the file may reveal its contents.</summary>
    public async Task ReleaseLogAsync() {
        Console.WriteLine("Attempting to release the log temporarily; the operations associated with this will not be logged to the log file.");
        if (FileSemaphore.WaitOne(2000)) {
            await LogWriter!.FlushAsync();
            LogWriter!.Close();
            LogWriter = File.AppendText(LogFilePath);
            FileSemaphore.Release();
            Console.WriteLine("Done!");
        } else
            Console.WriteLine("Unable to acquire log semaphore; attempt is abandoned.");
    }

    public void Dispose() {
        LogWriter!.Flush();
        LogWriter!.Close();
    }

}
