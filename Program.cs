using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using System;
using System.IO;
using MakeWad;

if (args.Length < 2)
{
    Console.WriteLine("Usage: makewad <in folder> <out wad>");
    return;
}

var wad = new TextureWad();

foreach (var path in Directory.EnumerateFiles(args[0], "*.png", SearchOption.AllDirectories))
{
    Console.WriteLine(path);
    var image   = Image.Load<Rgba32>(File.ReadAllBytes(path));
    var texture = new Texture { Data = new Image<L8>(image.Width, image.Height) };

    // Set the texture name
    var name = Path.GetFileNameWithoutExtension(path);

    if (name.Length > 15)
        Console.WriteLine("Warning: '{0}' truncated to 15 characters.", name);

    for (int i = 0, n = Math.Min(15, name.Length); i < n; i++)
        texture.Name[i] = (byte)name[i];

    // Compute palette
    var maxColors   = name.StartsWith('{') ? 255 : 256;
    var quantizer   = new WuQuantizer(new QuantizerOptions { MaxColors = maxColors }).CreatePixelSpecificQuantizer<Rgba32>(Configuration.Default);
    using var frame = QuantizerUtilities.BuildPaletteAndQuantizeFrame(quantizer, image.Frames[0], new Rectangle(0, 0, image.Width, image.Height));
    var palette     = frame.Palette.ToArray();

    for (int y = 0; y < frame.Height; y++)
    {
        var row = frame.DangerousGetRowSpan(y);

        for (int x = 0; x < frame.Width; x++)
        {
            if (name.StartsWith('{') && palette[row[x]].A < 128)
            {
                texture.Data[x, y] = new L8(0xFF);
            }
            else
            {
                texture.Data[x, y] = new L8(row[x]);
            }
        }
    }

    PixelOperations<Rgba32>.Instance.ToRgb24(Configuration.Default, palette, texture.Palette);
    wad.Textures.Add(texture);
}

Console.WriteLine("Saving...");
wad.Write(args[1]);
