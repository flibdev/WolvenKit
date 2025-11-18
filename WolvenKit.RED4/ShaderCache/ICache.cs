using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.RED4.ShaderCache.Common;

namespace WolvenKit.RED4.ShaderCache;
public interface ICache
{
    enum CacheType
    {
        Static,
        Dynamic
    }

    CacheType Type { get; }

    CacheMetadata Metadata { get; }
}
