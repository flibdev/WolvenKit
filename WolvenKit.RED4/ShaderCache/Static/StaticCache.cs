using System.Collections.Generic;
using WolvenKit.RED4.ShaderCache.Common;

namespace WolvenKit.RED4.ShaderCache.Static;

public class StaticCache : ICache
{
    public ICache.CacheType Type => ICache.CacheType.Static;
    public CacheMetadata Metadata { get; }

    public Dictionary<ulong, Shader> Shaders { get; private set; }
    public Dictionary<uint, Pass> Passes { get; private set; }

    public StaticCache(Dictionary<ulong, Shader> shaders, Dictionary<uint, Pass> passes, CacheMetadata metadata)
    {
        Shaders = shaders;
        Passes = passes;
        Metadata = metadata;
    }
}
