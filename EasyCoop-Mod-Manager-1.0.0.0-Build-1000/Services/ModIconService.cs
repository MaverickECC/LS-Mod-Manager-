using System.IO.Compression;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EasyCoop.ModManager.Services;

public static class ModIconService
{
    public static ImageSource? Read(ZipArchive archive, string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName)) return null;
        var normalized = iconName.Replace('\\', '/').TrimStart('/');
        var entry = archive.Entries.FirstOrDefault(e => string.Equals(e.FullName, normalized, StringComparison.OrdinalIgnoreCase))
                    ?? archive.Entries.FirstOrDefault(e => string.Equals(Path.GetFileName(e.FullName), Path.GetFileName(normalized), StringComparison.OrdinalIgnoreCase));
        if (entry is null) return null;

        using var stream = entry.Open();
        if (string.Equals(Path.GetExtension(entry.Name), ".dds", StringComparison.OrdinalIgnoreCase))
            return DecodeDds(stream);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static BitmapSource DecodeDds(Stream stream)
    {
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var data = memory.ToArray();
        if (data.Length < 128 || data[0] != 'D' || data[1] != 'D' || data[2] != 'S' || data[3] != ' ')
            throw new InvalidDataException("Ungültiges DDS-Modbild.");

        var height = ReadInt(data, 12);
        var width = ReadInt(data, 16);
        if (width <= 0 || height <= 0 || width > 4096 || height > 4096) throw new InvalidDataException("Ungültige DDS-Größe.");
        var fourCc = System.Text.Encoding.ASCII.GetString(data, 84, 4);
        var pixels = new byte[width * height * 4];
        if (fourCc is "DXT1" or "DXT3" or "DXT5") DecodeBlocks(data, 128, width, height, fourCc, pixels);
        else DecodeRaw(data, 128, width, height, pixels, ReadInt(data, 88));
        var image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
        image.Freeze();
        return image;
    }

    private static void DecodeBlocks(byte[] source, int offset, int width, int height, string format, byte[] target)
    {
        var blockSize = format == "DXT1" ? 8 : 16;
        for (var by = 0; by < (height + 3) / 4; by++)
        for (var bx = 0; bx < (width + 3) / 4; bx++)
        {
            if (offset + blockSize > source.Length) throw new InvalidDataException("DDS-Modbild ist unvollständig.");
            var alpha = new byte[16];
            Array.Fill(alpha, (byte)255);
            var colorOffset = offset;
            if (format == "DXT3")
            {
                for (var i = 0; i < 8; i++) { alpha[i * 2] = (byte)((source[offset + i] & 15) * 17); alpha[i * 2 + 1] = (byte)((source[offset + i] >> 4) * 17); }
                colorOffset += 8;
            }
            else if (format == "DXT5")
            {
                DecodeDxt5Alpha(source, offset, alpha);
                colorOffset += 8;
            }
            DecodeColors(source, colorOffset, target, width, height, bx, by, alpha, format == "DXT1");
            offset += blockSize;
        }
    }

    private static void DecodeDxt5Alpha(byte[] source, int offset, byte[] alpha)
    {
        var table = new byte[8];
        table[0] = source[offset]; table[1] = source[offset + 1];
        if (table[0] > table[1])
            for (var i = 1; i <= 6; i++) table[i + 1] = (byte)(((7 - i) * table[0] + i * table[1]) / 7);
        else
        {
            for (var i = 1; i <= 4; i++) table[i + 1] = (byte)(((5 - i) * table[0] + i * table[1]) / 5);
            table[6] = 0; table[7] = 255;
        }
        ulong bits = 0;
        for (var i = 0; i < 6; i++) bits |= (ulong)source[offset + 2 + i] << (8 * i);
        for (var i = 0; i < 16; i++) alpha[i] = table[(int)((bits >> (3 * i)) & 7)];
    }

    private static void DecodeColors(byte[] source, int offset, byte[] target, int width, int height, int bx, int by, byte[] alpha, bool dxt1)
    {
        var c0 = BitConverter.ToUInt16(source, offset);
        var c1 = BitConverter.ToUInt16(source, offset + 2);
        var colors = new byte[4, 4];
        Set565(colors, 0, c0); Set565(colors, 1, c1);
        if (c0 > c1 || !dxt1)
        {
            for (var channel = 0; channel < 3; channel++) { colors[2, channel] = (byte)((2 * colors[0, channel] + colors[1, channel]) / 3); colors[3, channel] = (byte)((colors[0, channel] + 2 * colors[1, channel]) / 3); }
            colors[2, 3] = colors[3, 3] = 255;
        }
        else
        {
            for (var channel = 0; channel < 3; channel++) colors[2, channel] = (byte)((colors[0, channel] + colors[1, channel]) / 2);
            colors[2, 3] = 255; colors[3, 3] = 0;
        }
        var indices = BitConverter.ToUInt32(source, offset + 4);
        for (var i = 0; i < 16; i++)
        {
            var x = bx * 4 + i % 4; var y = by * 4 + i / 4;
            if (x >= width || y >= height) continue;
            var color = (int)((indices >> (i * 2)) & 3); var destination = (y * width + x) * 4;
            target[destination] = colors[color, 2]; target[destination + 1] = colors[color, 1]; target[destination + 2] = colors[color, 0];
            target[destination + 3] = dxt1 && colors[color, 3] == 0 ? (byte)0 : alpha[i];
        }
    }

    private static void Set565(byte[,] colors, int index, ushort value)
    {
        colors[index, 0] = (byte)(((value >> 11) & 31) * 255 / 31);
        colors[index, 1] = (byte)(((value >> 5) & 63) * 255 / 63);
        colors[index, 2] = (byte)((value & 31) * 255 / 31);
        colors[index, 3] = 255;
    }

    private static void DecodeRaw(byte[] source, int offset, int width, int height, byte[] target, int bitsPerPixel)
    {
        if (bitsPerPixel != 32 || source.Length < offset + target.Length) throw new InvalidDataException("Dieses DDS-Format wird nicht unterstützt.");
        Buffer.BlockCopy(source, offset, target, 0, target.Length);
    }

    private static int ReadInt(byte[] source, int offset) => BitConverter.ToInt32(source, offset);
}
