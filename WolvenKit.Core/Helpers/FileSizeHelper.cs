using System;
using System.Globalization;

namespace WolvenKit.Core.Helpers;
public class FileSizeHelper
{
    public static string Humanize(long size)
    {
        string suffix;
        double readable;
        switch (Math.Abs(size))
        {
            case >= 0x1000000000000000:
                suffix = "EiB";
                readable = size >> 50;
                break;
            case >= 0x4000000000000:
                suffix = "PiB";
                readable = size >> 40;
                break;
            case >= 0x10000000000:
                suffix = "TiB";
                readable = size >> 30;
                break;
            case >= 0x40000000:
                suffix = "GiB";
                readable = size >> 20;
                break;
            case >= 0x100000:
                suffix = "MiB";
                readable = size >> 10;
                break;
            case >= 0x400:
                suffix = "KiB";
                readable = size;
                break;
            default:
                return size.ToString("0 Bytes");
        }

        return (readable / 1024.0).ToString("0.## ", CultureInfo.InvariantCulture) + suffix;
    }

}
