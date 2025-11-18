using System.Collections.Generic;
using WolvenKit.RED4.ShaderCache.Common;

namespace WolvenKit.RED4.ShaderCache.Dynamic;

public class MaterialCache : ICache
{
    public ICache.CacheType Type => ICache.CacheType.Dynamic;
    public CacheMetadata Metadata => _metadata;

    public Dictionary<ulong, Shader> Shaders { get; private set; }
    public Dictionary<uint, Material> Materials { get; private set; }

    public MaterialCache(Dictionary<ulong, Shader> shaders, Dictionary<uint, Material> materials, CacheMetadata metadata)
    {
        Shaders = shaders;
        Materials = materials;
        _metadata = metadata;
    }

    private readonly CacheMetadata _metadata;
}
