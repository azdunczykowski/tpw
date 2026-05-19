//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//_____________________________________________________________________________________________________________________________________

using System.Collections.Concurrent;
using System.Text;

namespace TP.ConcurrentProgramming.Data
{
  internal class DiagnosticLogger : IDisposable
  {
    internal static DiagnosticLogger CreateDefaultLogger()
    {
      string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diagnostic_log.txt");
      return new DiagnosticLogger(path);
    }

    internal DiagnosticLogger(string filePath)
    {
      _filePath = filePath;
      _queue = new BlockingCollection<string>(BoundedCapacity);
      _writer = new Thread(WriterLoop) { IsBackground = true, Name = "DiagLogger" };
      _writer.Start();
    }

    // Non-blocking: jeśli kolejka pełna (brak przepustowości I/O), wpis pomijany.
    internal void Log(int ballId, double posX, double posY, double velX, double velY, long timestampMs)
    {
      if (_disposed) return;
      _queue.TryAdd(Serialize(ballId, posX, posY, velX, velY, timestampMs));
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

    private readonly string _filePath;
    private readonly BlockingCollection<string> _queue;
    private readonly Thread _writer;
    private volatile bool _disposed;

    // Ręczna serializacja do ASCII – bez zewnętrznych pakietów.
    // Format: timestamp_ms;ball_id;pos_x;pos_y;vel_x;vel_y
    private static string Serialize(int id, double px, double py, double vx, double vy, long ts)
    {
      var sb = new StringBuilder(80);
      sb.Append(ts); sb.Append(';');
      sb.Append(id); sb.Append(';');
      sb.Append(px.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)); sb.Append(';');
      sb.Append(py.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)); sb.Append(';');
      sb.Append(vx.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)); sb.Append(';');
      sb.Append(vy.ToString("F4", System.Globalization.CultureInfo.InvariantCulture));
      return sb.ToString();
    }

    // Wątek konsumenta – opóźnienia I/O nie blokują wątków kul.
    private void WriterLoop()
    {
      try
      {
        using StreamWriter sw = new StreamWriter(_filePath, append: false, encoding: Encoding.ASCII);
        sw.WriteLine("timestamp_ms;ball_id;pos_x;pos_y;vel_x;vel_y");
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
