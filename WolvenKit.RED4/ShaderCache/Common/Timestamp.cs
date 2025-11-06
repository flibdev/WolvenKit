using System;

namespace WolvenKit.RED4.ShaderCache.Common;

/// <summary>
/// Bitpacked Timestamp format used by shader cache files.
/// Some files store this [date][time], other use [time][date]
/// </summary>
public class Timestamp
{
    public uint Date { get; private set; }
    public uint Time { get; private set; }

    public Timestamp(uint date, uint time)
    {
        Date = date;
        Time = time;
    }

    public Timestamp(DateTime timestamp)
    {
        Date  = (uint)timestamp.Year << 20;
        Date |= (uint)(timestamp.Month-1) << 15;
        Date |= (uint)(timestamp.Day-1) << 10;

        Time  = (uint)timestamp.Hour << 22;
        Time |= (uint)timestamp.Minute << 16;
        Time |= (uint)timestamp.Second << 10;
        Time |= (uint)timestamp.Millisecond;
    }

    public DateTime ToDateTime()
    {
        var year  = (int)(Date & 0xFFF00000) >> 20;
        var month = (int)(Date & 0x000F8000) >> 15;
        var day   = (int)(Date & 0x00007C00) >> 10;

        var hour  = (int)(Time & 0x07C00000) >> 22;
        var mins  = (int)(Time & 0x003F0000) >> 16;
        var secs  = (int)(Time & 0x0000FC00) >> 10;
        var milli = (int)(Time & 0x000003FF);

        return new DateTime(year, month + 1, day + 1, hour, mins, secs, milli, DateTimeKind.Local);
    }
}
