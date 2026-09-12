using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

// Wraps the stdin stream and watches for client disconnection.
//
// Issue #21e: when the client closes stdin, this process used to die via
// Environment.Exit(0) without any log flush, losing the final entries. Each
// EOF path now performs a best-effort synchronous flush (Log.CloseAndFlush())
// before exiting. The hard kill itself is DELIBERATE and stays: on Linux the
// OmniSharp stream layer busy-loops at 100% CPU when Read returns 0 without
// a process termination.
public class DisconnectAwareStream : Stream
{
    private readonly Stream _baseStream;

    public DisconnectAwareStream(Stream baseStream)
    {
        _baseStream = baseStream;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        // Read from the real stdin.
        int bytesRead = _baseStream.Read(buffer, offset, count);

        // ON LINUX: a Read returning 0 means the client (parent) closed the
        // pipe. If we do not exit NOW, OmniSharp enters an infinite loop
        // (100% CPU).
        if (bytesRead == 0)
        {
            FlushLogsThenExit();
        }

        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        int bytesRead = _baseStream.Read(buffer);
        if (bytesRead == 0) FlushLogsThenExit();
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int bytesRead = await _baseStream.ReadAsync(buffer, cancellationToken);
        if (bytesRead == 0) FlushLogsThenExit();
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        if (bytesRead == 0) FlushLogsThenExit();
        return bytesRead;
    }

    /// <summary>
    /// Best-effort synchronous log flush right before the hard process exit
    /// (issue #21e). Environment.Exit skips finally blocks, so the async
    /// <c>Log.CloseAndFlushAsync()</c> in Program.Main never runs on the
    /// stdin-EOF path and the last log entries were lost. This must stay
    /// synchronous: we are inside a stream Read and exiting anyway, so
    /// awaiting async teardown is pointless and unsafe here. Failures are
    /// swallowed — the process is about to die regardless.
    /// </summary>
    private static void FlushLogsThenExit()
    {
        try
        {
            Log.CloseAndFlush();
        }
        catch
        {
            // Never let a flush failure prevent the exit.
        }

        System.Environment.Exit(0);
    }

    // Delegate the remaining mandatory members unchanged.
    public override void Flush() => _baseStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
    public override void SetLength(long value) => _baseStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);
    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => _baseStream.CanSeek;
    public override bool CanWrite => _baseStream.CanWrite;
    public override long Length => _baseStream.Length;
    public override long Position { get => _baseStream.Position; set => _baseStream.Position = value; }
}