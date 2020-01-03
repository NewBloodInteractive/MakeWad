using System.IO;

public static class Extensions
{
    public static StreamSeekDisposable ScopedSeek(this Stream stream, long offset, SeekOrigin origin)
    {
        return new StreamSeekDisposable(stream, offset, origin);
    }

    public static BinaryWriterSeekDisposable ScopedSeek(this BinaryWriter writer, int offset, SeekOrigin origin)
    {
        return new BinaryWriterSeekDisposable(writer, offset, origin);
    }
}
