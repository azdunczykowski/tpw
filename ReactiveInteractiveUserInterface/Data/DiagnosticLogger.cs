//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//_____________________________________________________________________________________________________________________________________

using System.Collections.Concurrent;
using System.Text;

namespace TP.ConcurrentProgramming.Data
{
  internal class DiagnosticLogger : ILogger
  {
    internal static DiagnosticLogger CreateDefaultLogger(string filename = "diagnostic_log.txt")
    {
      string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
      return new DiagnosticLogger(path);
    }

    internal DiagnosticLogger(string filePath)
    {
      _filePath = filePath;
      _queue = new BlockingCollection<string>(BoundedCapacity);
      _writer = new Thread(WriterLoop) { IsBackground = true, Name = "DiagLogger" };
      _writer.Start();
    }

    public void Log(LogLevel level, string message)
    {
      if (_disposed) return;
      string entry = FormatSyslog(level, message);

      lock (_bufferLock)
      {
        if ((int)level <= (int)LogLevel.Error)
          _queue.TryAdd(entry);
        else if (_queue.Count < BoundedCapacity * 3 / 4)
          _queue.TryAdd(entry);
      }
    }

    public void Dispose()
    {
      if (_disposed) return;
      _disposed = true;
      _queue.CompleteAdding();
      _writer.Join(TimeSpan.FromSeconds(3));
      _queue.Dispose();
    }

    private const int BoundedCapacity = 1024;
    private static readonly int _pid = Environment.ProcessId;
    private static readonly string _hostname = Environment.MachineName;

    private readonly string _filePath;
    private readonly BlockingCollection<string> _queue;
    private readonly Thread _writer;
    private readonly object _bufferLock = new object();
    private volatile bool _disposed;

    private static string FormatSyslog(LogLevel level, string message)
    {
      int pri = 8 + (int)level;
      string timestamp = DateTime.UtcNow.ToString("MMM dd HH:mm:ss",
        System.Globalization.CultureInfo.InvariantCulture);
      string levelName = level.ToString().ToUpperInvariant();
      return $"<{pri}>{timestamp} {_hostname} ball-sim[{_pid}]: {levelName} {message}";
    }

    private void WriterLoop()
    {
      try
      {
        using StreamWriter sw = new StreamWriter(_filePath, append: false, encoding: Encoding.ASCII);
        foreach (string entry in _queue.GetConsumingEnumerable())
        {
          sw.WriteLine(entry);
          if (_queue.Count == 0) sw.Flush();
        }
        sw.Flush();
      }
      catch (Exception) { }
    }
  }
}
