using System;
using System.IO;

namespace MakeWad;

public readonly struct StreamSeekDisposable : IDisposable
{
    readonly Stream target;
    readonly long reset;

    public StreamSeekDisposable(Stream stream, long offset, SeekOrigin origin)
    {
        target = stream;
        reset  = target.Position;
        target.Seek(offset, origin);
    }

    public void Dispose()
    {
        target.Position = reset;
    }
}

public readonly struct BinaryWriterSeekDisposable : IDisposable
{
    readonly BinaryWriter target;
    readonly int reset;

    public BinaryWriterSeekDisposable(BinaryWriter writer, int offset, SeekOrigin origin)
    {
        target = writer;
        reset  = (int)target.BaseStream.Position;
        target.Seek(offset, origin);
    }

    public void Dispose()
    {
        target.Seek(reset, SeekOrigin.Begin);
    }
}
