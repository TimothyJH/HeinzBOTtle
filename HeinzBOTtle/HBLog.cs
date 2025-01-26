namespace HeinzBOTtle;

/// <summary>
/// Manages the logging system for this program.
/// </summary>
public sealed class HBLog : IDisposable {

    /// <summary>The interface for writing messages to the full log file.</summary>
    public StreamWriter? FullLogWriter { get; private set; }
    /// <summary>The interface for writing messages to the reduced log file.</summary>
    public StreamWriter? ReducedLogWriter { get; private set; }
    /// <summary>The path indicating the location of the full log file in the filesystem.</summary>
    public string FullLogFilePath { get; }
    /// <summary>The path indicating the location of the reduced log file in the filesystem.</summary>
    public string ReducedLogFilePath { get; }
    /// <summary>The date to be used when renaming the log files before terminating the program.</summary>
    private DateTime LogStart { get; }
    /// <summary>Controls mutual exclusion for access of the log file.</summary>
    private Semaphore FileSemaphore { get; }

    public HBLog(string logFilePath, string reducedLogFilePath) {
        LogStart = DateTime.Now;
        FullLogFilePath = logFilePath;
        ReducedLogFilePath = reducedLogFilePath;
        FileSemaphore = new Semaphore(1, 1);
        
    }

    /// <summary>Opens the file streams to write the logs.</summary>
    /// <returns>Whether the operation was successful.</returns>
    public bool Start() {
        try {
            if (File.Exists(FullLogFilePath))
                File.Delete(FullLogFilePath);
            if (File.Exists(ReducedLogFilePath))
                File.Delete(ReducedLogFilePath);
            FullLogWriter = File.AppendText(FullLogFilePath);
            ReducedLogWriter = File.AppendText(ReducedLogFilePath);
            return true;
        } catch {
            FullLogWriter = null;
            ReducedLogWriter = null;
            return false;
        }
    }

    /// <summary>Writes a line to the log with an immediate timestamp and HeinzBOTtle as the message source.</summary>
    /// <param name="message">The message to write, which will be followed by a line termination</param>
    /// <param name="reduced">Whether the message should be included in the reduced log</param>
    public void Info(string? message, bool reduced = true) {
        WriteLineToLog(message, "HeinzBOTtle", DateTime.Now, reduced);
    }

    /// <summary>Writes a line to the log asynchronously with an immediate timestamp and HeinzBOTtle as the message source.</summary>
    /// <param name="message">The message to write, which will be followed by a line termination</param>
    /// <param name="reduced">Whether the message should be included in the reduced log</param>
    public async Task InfoAsync(string? message, bool reduced = true) {
        await WriteLineToLogAsync(message, "HeinzBOTtle", DateTime.Now, reduced);
    }

    /// <summary>Writes a line to the log based on the provided arguments.</summary>
    /// <param name="message">The message to write, which will be followed by a line termination</param>
    /// <param name="source">The source of the message for inclusion in the message prefix</param>
    /// <param name="ts">The message timestamp for inclusion in the message prefix</param>
    /// <param name="reduced">Whether the message should be included in the reduced log</param>
    public void WriteLineToLog(string? message, string source, DateTime ts, bool reduced = false) {
        if (message == null)
            return;
        ApplyPrefix(ref message, source, ts);
        Console.WriteLine(message);
        if (FileSemaphore.WaitOne(2000)) {
            FullLogWriter!.WriteLine(message);
            if (reduced)
                ReducedLogWriter!.WriteLine(message);
            FileSemaphore.Release();
        } else
            Console.WriteLine($"(!) MESSAGE COULD NOT BE LOGGED TO FILE!");
    }

    /// <summary>Writes a line to the log asynchronously based on the provided arguments.</summary>
    /// <param name="message">The message to write, which will be followed by a line termination</param>
    /// <param name="source">The source of the message for inclusion in the message prefix</param>
    /// <param name="ts">The message timestamp for inclusion in the message prefix</param>
    /// <param name="reduced">Whether the message should be included in the reduced log</param>
    public async Task WriteLineToLogAsync(string? message, string source, DateTime ts, bool reduced = false) {
        if (message == null)
            return;
        ApplyPrefix(ref message, source, ts);
        Console.WriteLine(message);
        if (FileSemaphore.WaitOne(2000)) {
            Task fullTask = FullLogWriter!.WriteLineAsync(message);
            if (reduced) {
                Task reducedTask = ReducedLogWriter!.WriteLineAsync(message);
                await reducedTask;
            }
            await fullTask;
            FileSemaphore.Release();
        } else
            Console.WriteLine($"(!) MESSAGE COULD NOT BE LOGGED TO FILE!");
    }

    /// <summary>Fluishes the log file's stream and temporarily releases process control over the log file so that the file may reveal its contents.</summary>
    public async Task ReleaseLogsAsync() {
        Console.WriteLine("Attempting to release the log temporarily; the operations associated with this will not be logged to the log file.");
        if (FileSemaphore.WaitOne(2000)) {
            Task full = FullLogWriter!.FlushAsync();
            Task reduced = ReducedLogWriter!.FlushAsync();
            await full;
            await reduced;
            FullLogWriter!.Close();
            FullLogWriter = File.AppendText(FullLogFilePath);
            ReducedLogWriter!.Close();
            ReducedLogWriter = File.AppendText(ReducedLogFilePath);
            FileSemaphore.Release();
            Console.WriteLine("Done!");
        } else
            Console.WriteLine("Unable to acquire log semaphore; attempt is abandoned.");
    }

    /// <summary>Moves the log files to the provided destination if that configuration exists. This assumes that the files have already been flushed and closed.</summary>
    /// <param name="logDestination">The directory to which the log files should be moved</param>
    public bool Retire(DirectoryInfo logDestination) {
        try {
            if (!logDestination.Exists)
                logDestination.Create();
            string dir = logDestination.FullName;
            string fullStem = Path.Combine(dir, $"log-{LogStart.Year}-{LogStart.Month}-{LogStart.Day}-full");
            string reducedStem = Path.Combine(dir, $"log-{LogStart.Year}-{LogStart.Month}-{LogStart.Day}-reduced");
            if (File.Exists(fullStem + ".txt") || File.Exists(reducedStem + ".txt")) {
                for (int i = 1; i < 1234567890; i++) {
                    if (!File.Exists($"{fullStem}-{i}.txt") && !File.Exists($"{reducedStem}-{i}.txt")) {
                        File.Move(FullLogFilePath, $"{fullStem}-{i}.txt");
                        File.Move(ReducedLogFilePath, $"{reducedStem}-{i}.txt");
                        Console.WriteLine($"Logs were successfully moved to \"log-{LogStart.Year}-{LogStart.Month}-{LogStart.Day}-*-{i}.txt\".");
                        return true;
                    }
                }
                Console.WriteLine("Unable to move logs because SOMEHOW there are already over a billion logs from today! I wonder how that happened...");
                return false;
            } else {
                File.Move(FullLogFilePath, fullStem + ".txt");
                File.Move(ReducedLogFilePath, reducedStem + ".txt");
                Console.WriteLine($"Logs were successfully moved to \"log-{LogStart.Year}-{LogStart.Month}-{LogStart.Day}-*.txt\".");
                return true;
            }
        } catch (Exception e) {
            Console.WriteLine($"Unable to move logs due to an exception ({e.GetType()}): {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    public void Dispose() {
        FullLogWriter!.Flush();
        FullLogWriter!.Close();
        ReducedLogWriter!.Flush();
        ReducedLogWriter!.Dispose();
    }

    /// <summary>Applies a prefix to the provided log message and adjusts any additional lines in the message to align properly with the first line.</summary>
    /// <param name="message">The raw message to be modified</param>
    /// <param name="source">The source of the message</param>
    /// <param name="ts">The message timestamp</param>
    private static void ApplyPrefix(ref string message, string source, DateTime ts) {
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

}
