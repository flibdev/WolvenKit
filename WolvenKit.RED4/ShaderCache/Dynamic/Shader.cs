using System.Collections.Generic;
using WolvenKit.RED4.ShaderCache.Common;

namespace WolvenKit.RED4.ShaderCache.Dynamic;
public class Shader : IShader
{
    public ulong Hash { get; set; }
    public uint Size { get; set; }
    public long Address { get; set; }
    public uint MatModMask { get; set; }
    public List<ShaderParam> Params { get; set; } = [];
}
