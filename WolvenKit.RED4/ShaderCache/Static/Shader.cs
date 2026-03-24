using System.Collections.Generic;
using WolvenKit.RED4.ShaderCache.Common;

namespace WolvenKit.RED4.ShaderCache.Static;

public class Shader : IShader
{
    public ulong Hash { get; set; }
    public uint Size { get; set; }
    public long Address { get; set; }
}
