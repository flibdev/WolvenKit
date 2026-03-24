using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WolvenKit.ShaderTools;

internal static unsafe partial class DXD_API
{
    public const string LibraryPath = "lib/DXILDecompiler.dll";

    [LibraryImport(LibraryPath, EntryPoint = "dxd_get_error_string")]
    public static partial void GetErrorString(uint id, Span<byte> buffer, ulong size);

    [LibraryImport(LibraryPath, EntryPoint = "dxd_api_create")]
    public static partial IntPtr CreateHandle();

    [LibraryImport(LibraryPath, EntryPoint = "dxd_api_destroy")]
    public static partial uint ReleaseHandle(IntPtr handle);


    [LibraryImport(LibraryPath, EntryPoint = "dxd_dxc_initialize")]
    public static partial uint DXC_Initialize(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string dllPath);


    [LibraryImport(LibraryPath, EntryPoint = "dxd_dxc_export_disassembled")]
    public static partial uint DXC_ExportDisassembled(IntPtr handle, Span<byte> data, ulong size, [MarshalAs(UnmanagedType.LPStr)] string filename);

    [LibraryImport(LibraryPath, EntryPoint = "dxd_spv_export_spirv")]
    public static partial uint SPV_ExportSPIRV(IntPtr handle, Span<byte> data, ulong size, [MarshalAs(UnmanagedType.LPStr)] string filename);


    [LibraryImport(LibraryPath, EntryPoint = "dxd_spv_export_hlsl")]
    public static partial uint SPV_ExportHLSL(IntPtr handle, Span<byte> data, ulong size, [MarshalAs(UnmanagedType.LPStr)] string filename);
}
