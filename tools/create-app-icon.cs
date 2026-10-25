// Draws the app icon (a cash register printing a receipt) and writes it as a multi-size .ico.
// The drawing lives here, as vector shapes on a 256 x 256 grid, so the icon can be changed and regenerated.
//
// Usage, from the repository root:  dotnet run tools/create-app-icon.cs
// Output: src/PosNfce.Desktop/Assets/app.ico (sizes 16 to 256) and app-64.png (for the window header).

#:property TargetFramework=net10.0-windows
#:property UseWPF=true
#:property PublishAot=false

using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

int[] sizes = [16, 20, 24, 32, 40, 48, 64, 256];
var assets = Path.Combine(Directory.GetCurrentDirectory(), "src", "PosNfce.Desktop", "Assets");
Directory.CreateDirectory(assets);

// WPF drawing objects need an STA thread.
var thread = new Thread(() =>
{
    var frames = sizes.Select(size => (size, Render(size))).ToList();
    File.WriteAllBytes(Path.Combine(assets, "app.ico"), BuildIco(frames));
    File.WriteAllBytes(Path.Combine(assets, "app-64.png"), Png(frames.Single(f => f.size == 64).Item2));
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
Console.WriteLine($"Icon written to {assets}");

static BitmapSource Render(int size)
{
    var accent = Brush("#1565C0");
    var dark = Brush("#1F2A37");
    var amber = Brush("#F9A825");
    var white = Brushes.White;

    var visual = new DrawingVisual();
    using (var dc = visual.RenderOpen())
    {
        dc.PushTransform(new ScaleTransform(size / 256.0, size / 256.0));

        // Background: rounded square in the app's accent color, visible on light and dark taskbars.
        dc.DrawRoundedRectangle(accent, null, new Rect(8, 8, 240, 240), 52, 52);

        // Receipt coming out of the printer, with a torn edge on top.
        var receipt = Geometry.Parse("M 148,124 L 148,52 L 156,44 L 164,52 L 172,44 L 180,52 L 188,44 L 196,52 L 196,124 Z");
        dc.DrawGeometry(white, null, receipt);
        if (size >= 32)
        {
            dc.DrawRectangle(accent, null, new Rect(158, 66, 28, 7));
            dc.DrawRectangle(accent, null, new Rect(158, 82, 20, 7));
            dc.DrawRectangle(accent, null, new Rect(158, 98, 28, 7));
        }

        // Customer display on a short pole.
        dc.DrawRoundedRectangle(white, null, new Rect(52, 44, 84, 56), 10, 10);
        dc.DrawRoundedRectangle(dark, null, new Rect(62, 54, 64, 36), 4, 4);
        if (size >= 24)
            dc.DrawRectangle(amber, null, new Rect(72, 66, 36, 10));
        dc.DrawRectangle(white, null, new Rect(84, 98, 20, 24));

        // Body with the keypad.
        dc.DrawGeometry(white, null, Geometry.Parse("M 60,120 L 204,120 L 216,180 L 40,180 Z"));
        if (size >= 32)
        {
            for (var row = 0; row < 2; row++)
                for (var col = 0; col < 4; col++)
                    dc.DrawRoundedRectangle(accent, null, new Rect(72 + col * 30 - row * 4, 132 + row * 22, 20, 12), 3, 3);
        }

        // Cash drawer with its handle.
        dc.DrawRoundedRectangle(white, null, new Rect(32, 186, 192, 36), 8, 8);
        dc.DrawRoundedRectangle(accent, null, new Rect(104, 199, 48, 10), 5, 5);
    }

    var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
    bitmap.Render(visual);
    return new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
}

static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

static byte[] Png(BitmapSource bitmap)
{
    var encoder = new PngBitmapEncoder();
    encoder.Frames.Add(BitmapFrame.Create(bitmap));
    using var stream = new MemoryStream();
    encoder.Save(stream);
    return stream.ToArray();
}

// Small sizes go in as 32-bit DIBs, the format every Windows icon reader understands; 256 goes in as PNG,
// as Windows itself does.
static byte[] Dib(BitmapSource bitmap)
{
    int size = bitmap.PixelWidth, stride = size * 4;
    var pixels = new byte[stride * size];
    bitmap.CopyPixels(pixels, stride, 0);

    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    writer.Write(40);            // BITMAPINFOHEADER size
    writer.Write(size);
    writer.Write(size * 2);      // XOR image + AND mask
    writer.Write((short)1);      // planes
    writer.Write((short)32);     // bits per pixel
    writer.Write(0);             // BI_RGB
    writer.Write(0);
    writer.Write(0);
    writer.Write(0);
    writer.Write(0);
    writer.Write(0);
    for (var y = size - 1; y >= 0; y--) // bottom-up rows
        writer.Write(pixels, y * stride, stride);
    var maskStride = (size + 31) / 32 * 4; // AND mask unused (alpha wins), rows padded to 4 bytes
    writer.Write(new byte[maskStride * size]);
    return stream.ToArray();
}

static byte[] BuildIco(List<(int size, BitmapSource bitmap)> frames)
{
    var images = frames.Select(f => f.size == 256 ? Png(f.bitmap) : Dib(f.bitmap)).ToList();

    using var stream = new MemoryStream();
    using var writer = new BinaryWriter(stream);
    writer.Write((short)0);  // reserved
    writer.Write((short)1);  // type: icon
    writer.Write((short)frames.Count);

    var offset = 6 + 16 * frames.Count;
    for (var i = 0; i < frames.Count; i++)
    {
        var size = frames[i].size;
        writer.Write((byte)(size == 256 ? 0 : size)); // 0 means 256
        writer.Write((byte)(size == 256 ? 0 : size));
        writer.Write((byte)0);   // palette
        writer.Write((byte)0);   // reserved
        writer.Write((short)1);  // planes
        writer.Write((short)32); // bits per pixel
        writer.Write(images[i].Length);
        writer.Write(offset);
        offset += images[i].Length;
    }
    foreach (var image in images)
        writer.Write(image);
    return stream.ToArray();
}
