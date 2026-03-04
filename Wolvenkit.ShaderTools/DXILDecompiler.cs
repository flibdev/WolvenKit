using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace WolvenKit.ShaderTools;


public static unsafe partial class DXILDecompiler
{
    private const string s_library = "lib/DXILDecompiler.dll";

    public const uint SUCCESS = 0;

    [LibraryImport(s_library, EntryPoint = "dxd_get_error_string")]
    private static partial void GetErrorStringImpl(uint id, Span<byte> buffer, ulong size);

    public static string GetErrorString(uint id)
    {
        // impl requires a minimum of 40 byte buffer
        Span<byte> buffer = stackalloc byte[64];
        GetErrorStringImpl(id, buffer, (ulong)buffer.Length);
        return Encoding.UTF8.GetString(buffer);
    }

    [LibraryImport(s_library, EntryPoint = "dxd_export_disassembled")]
    public static partial uint ExportDisassembled(Span<byte> data, ulong size, [MarshalAs(UnmanagedType.LPStr)] string filename);
}
