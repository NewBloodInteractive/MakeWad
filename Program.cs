using System;
using System.Numerics;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class Program
{
    static unsafe void Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: makewad <in folder> <in palette> <out wad>");
            return;
        }

        var palette = LoadPalette(args[1]);
        var wad     = new TextureWad { Palette = palette };

        foreach (var path in Directory.EnumerateFiles(args[0], "*.png", SearchOption.AllDirectories))
        {
            Console.WriteLine(path);
            var image   = Image.Load<Rgba32>(File.ReadAllBytes(path));
            var texture = new Texture { Data = new Image<Gray8>(image.Width, image.Height) };

            // Set the texture name
            var name = Path.GetFileNameWithoutExtension(path);

            if (name.Length > 16)
                Console.WriteLine("Warning: '{0}' truncated to 16 characters.", name);

            for (int i = 0, n = Math.Min(16, name.Length); i < n; i++)
                texture.Name[i] = (byte)name[i];

            Console.WriteLine("Palettizing...");
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    texture.Data[x, y] = NearestPaletteEntry(palette, image[x, y]);
                }
            }

            wad.Textures.Add(texture);
        }

        Console.WriteLine("Saving...");
        wad.Write(args[2]);
    }

    static Gray8 NearestPaletteEntry(Rgb24[] palette, Rgba32 pixel)
    {
        if (pixel.A == 0)
            return new Gray8(0);

        var dist  = float.PositiveInfinity;
        var color = new Vector3(pixel.R, pixel.G, pixel.B);
        var min   = new Gray8(0);

        for (var i = 0; i < palette.Length; i++)
        {
            var entry = new Vector3(palette[i].R, palette[i].G, palette[i].B);
            var cDist = Vector3.DistanceSquared(color, entry);

            if (cDist < dist)
            {
                dist = cDist;
                min  = new Gray8((byte)i);
            }
        }

        return min;
    }

    static Rgb24[] LoadPalette(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);
        var palette  = new Rgb24[256];
        
        for (int i = 0; i < 256; i++)
        {
            palette[i] = new Rgb24
            {
                R = br.ReadByte(),
                G = br.ReadByte(),
                B = br.ReadByte()
            };
        }

        return palette;
    }
}
