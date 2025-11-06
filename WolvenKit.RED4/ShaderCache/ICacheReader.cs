using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.RED4.IO;
using WolvenKit.RED4.ShaderCache.Dynamic;

namespace WolvenKit.RED4.ShaderCache;
public interface ICacheReader
{
    enum ReadState
    {
        Success = 0,
        UnknownMagic,
        UnsupportedVersion
    }

    static abstract bool IsSupportedFile(BinaryReader reader);

    ReadState TryReadFile(out ICache? cache);
}
