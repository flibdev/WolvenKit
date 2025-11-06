using System.Collections.Generic;

namespace WolvenKit.RED4.ShaderCache.Dynamic;

public class MaterialCache : ICache
{
    ICache.Type ICache.GetType => ICache.Type.Dynamic;

    public Dictionary<ulong, Shader> Shaders { get; set; } = [];
    public Dictionary<uint, Material> Materials { get; set; } = [];
}
