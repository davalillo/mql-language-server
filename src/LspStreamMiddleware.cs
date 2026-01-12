using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Esta clase envuelve el stream de entrada y vigila si se corta la conexión
public class DisconnectAwareStream : Stream
{
    private readonly Stream _baseStream;

    public DisconnectAwareStream(Stream baseStream)
    {
        _baseStream = baseStream;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        // Leemos del Stdin real
        int bytesRead = _baseStream.Read(buffer, offset, count);

        // EN LINUX: Si Read devuelve 0, significa que el cliente (padre) cerró el tubo.
        // Si no salimos YA, OmniSharp entrará en bucle infinito (100% CPU).
        if (bytesRead == 0)
        {
            // Matamos el proceso inmediatamente
            System.Environment.Exit(0);
        }

        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        int bytesRead = _baseStream.Read(buffer);
        if (bytesRead == 0) System.Environment.Exit(0);
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int bytesRead = await _baseStream.ReadAsync(buffer, cancellationToken);
        if (bytesRead == 0) System.Environment.Exit(0);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        if (bytesRead == 0) System.Environment.Exit(0);
        return bytesRead;
    }

    // Delegamos el resto de métodos obligatorios sin cambios
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