using System.IO;
using System.Text;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

public unsafe struct Texture
{
    public Image<Gray8> Data;
    public fixed byte Name[16];
}

public class TextureWad
{
    public IList<Texture> Textures { get; }

    public IReadOnlyList<Rgb24> Palette { get; set; }

    public int LumpCount
    {
        get
        {
            if (Palette is null)
                return Textures.Count;

            return Textures.Count + 1;
        }
    }

    public TextureWad()
    {
        Textures = new List<Texture>();
        Palette  = null;
    }

    public unsafe void Write(string path)
    {
        using var fs = File.OpenWrite(path);
        using var bw = new BinaryWriter(fs);
        fs.SetLength(0);

        bw.Write(new[] { (byte)'W', (byte)'A', (byte)'D', (byte)'2' });
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

            for (int j = 0; j < 16; j++)
                bw.Write(texture.Name[j]);

            bw.Write(texture.Data.Width);
            bw.Write(texture.Data.Height);

            // The offset to where the mipmap offsets are stored.
            var mipOffsets = (int)fs.Position;
            for (int mip = 0; mip < 4; mip++) bw.Write(0);

            // Write the mipmap texture data.
            for (int mip = 0; mip < 4; mip++)
            {
                mips[mip] = (int)fs.Position - offsets[i];
                var image = texture.Data;

                if (mip > 0)
                {
                    // We need to calculate the mipmap textures ourself.
                    image      = image.Clone();
                    var width  = image.Width  / (2 * mip);
                    var height = image.Height / (2 * mip);

                    image.Mutate(context =>
                    {
                        var resampler = new NearestNeighborResampler();
                        context.Resize(width, height, resampler);
                    });
                }

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        bw.Write(image[x, y].PackedValue);
                    }
                }
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

        // The offset to the palette colour data.
        var paletteOffset = (int)fs.Position;
        
        if (Palette is {})
        {
            foreach (var entry in Palette)
            {
                bw.Write(entry.R);
                bw.Write(entry.G);
                bw.Write(entry.B);
            }
        }

        // Write the lump directory offset into the header.
        var directoryOffset = (int)fs.Position;
        using (bw.ScopedSeek(8, SeekOrigin.Begin))
            bw.Write(directoryOffset);

        for (int i = 0; i < Textures.Count; i++)
        {
            var texture = Textures[i];

            bw.Write(offsets[i]);
            bw.Write(offsets[i + 1] - offsets[i]);
            bw.Write(offsets[i + 1] - offsets[i]);
            bw.Write((byte)'D');
            bw.Write((byte)0);
            bw.Write((short)0);

            for (int j = 0; j < 16; j++)
                bw.Write(texture.Name[j]);
        }

        if (Palette is {})
        {
            bw.Write(paletteOffset);
            bw.Write(256 * 3);
            bw.Write(256 * 3);
            bw.Write((byte)'@');
            bw.Write((byte)0);
            bw.Write((short)0);
            bw.Write(Encoding.ASCII.GetBytes("PALETTE\0\0\0\0\0\0\0\0\0"));
        }
    }
}
