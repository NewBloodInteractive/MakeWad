using System.IO;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System.Runtime.CompilerServices;

namespace MakeWad;

public struct Texture
{
    public Image<L8> Data;
    public TextureName Name;
    public TexturePalette Palette;
}

[InlineArray(16)]
public struct TextureName
{
    private byte _element0;
}

[InlineArray(256)]
public struct TexturePalette
{
    private Rgb24 _element0;
}

public sealed class TextureWad
{
    public List<Texture> Textures { get; } = [];

    public int LumpCount => Textures.Count;

    public void Write(string path)
    {
        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        bw.Write("WAD3"u8);
        bw.Write(LumpCount);
        bw.Write(0);

        // We store one more texture offset than necessary so that we
        // can avoid needing to store the sizes of each texture lump.
        var offsets = new int[Textures.Count + 1];
        var mips    = new int[4];

        for (int i = 0; i < Textures.Count; i++)
        {
            var texture = Textures[i];
            offsets[i]  = (int)fs.Position;

            bw.Write(texture.Name);
            bw.Write(texture.Data.Width);
            bw.Write(texture.Data.Height);

            // The offset to where the mipmap offsets are stored.
            var mipOffsets = (int)fs.Position;

            for (int mip = 0; mip < 4; mip++)
                bw.Write(0);

            // Write the mipmap texture data.
            for (int mip = 0; mip < 4; mip++)
            {
                mips[mip] = (int)fs.Position - offsets[i];
                var image = texture.Data;

                if (mip > 0)
                    image = image.Clone(context => context.Resize(image.Width >> mip, image.Height >> mip, new NearestNeighborResampler()));

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        bw.Write(image[x, y].PackedValue);
                    }
                }

                if (mip > 0)
                {
                    image.Dispose();
                }
            }

            // Write the palette data.
            bw.Write((short)256);

            foreach (var entry in texture.Palette)
            {
                bw.Write(entry.R);
                bw.Write(entry.G);
                bw.Write(entry.B);
            }

            using (bw.ScopedSeek(mipOffsets, SeekOrigin.Begin))
            {
                for (int mip = 0; mip < 4; mip++)
                {
                    bw.Write(mips[mip]);
                }
            }
        }

        // Fill in the final texture offset, marking the end.
        offsets[Textures.Count] = (int)fs.Position;

        // Write the lump directory offset into the header.
        var directoryOffset = (int)fs.Position;

        using (bw.ScopedSeek(8, SeekOrigin.Begin))
            bw.Write(directoryOffset);

        for (int i = 0; i < Textures.Count; i++)
        {
            var texture = Textures[i];

            bw.Write(offsets[i]); // offset
            bw.Write(offsets[i + 1] - offsets[i]); // length
            bw.Write(offsets[i + 1] - offsets[i]); // uncompressed length
            bw.Write((byte)'C'); // type
            bw.Write((byte)0); // compression type
            bw.Write((short)0); // unused
            bw.Write(texture.Name);
        }
    }
}
