using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace WolvenKit.ShaderTools;


public class DXILDecompiler : IDisposable
{
    private const string s_dxCompilerPath = "lib/dxcompiler.dll";
    private const uint s_success = 0;

    private IntPtr _handle = IntPtr.Zero;


    public static string GetErrorString(uint id)
    {
        // impl requires a minimum of 64 byte buffer
        Span<byte> buffer = stackalloc byte[64];
        DXD_API.GetErrorString(id, buffer, (ulong)buffer.Length);
        return Encoding.UTF8.GetString(buffer);
    }

    private static void ThrowIfError(uint id)
    {
        if (id != s_success)
        {
            throw new Exception($"DXILDecompiler Error: {GetErrorString(id)}");
        }
    }

    public DXILDecompiler()
    {
        _handle = DXD_API.CreateHandle();

        ThrowIfError(DXD_API.DXC_Initialize(_handle, s_dxCompilerPath));
    }

    public void ExportDisassembled(Span<byte> buffer, string filepath)
    {
        ThrowIfError(DXD_API.DXC_ExportDisassembled(_handle, buffer, (ulong)buffer.Length, filepath));
    }

    public void ExportSPIRV(Span<byte> buffer, string filepath)
    {
        ThrowIfError(DXD_API.SPV_ExportSPIRV(_handle, buffer, (ulong)buffer.Length, filepath));
    }

    public void ExportHLSL(Span<byte> buffer, string filepath)
    {
        ThrowIfError(DXD_API.SPV_ExportHLSL(_handle, buffer, (ulong)buffer.Length, filepath));
    }


    #region IDisposable

    private bool _isDisposed = false;

    ~DXILDecompiler()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                // dispose managed state (managed objects)
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            if (_handle != IntPtr.Zero)
            {
                var res = DXD_API.ReleaseHandle(_handle);
                if (res != s_success)
                {
                    // This shouldn't ever happen
                    throw new Exception($"Error releasing handle: {GetErrorString(res)}");
                }
            }

            _isDisposed = true;
        }
    }

    #endregion
}
