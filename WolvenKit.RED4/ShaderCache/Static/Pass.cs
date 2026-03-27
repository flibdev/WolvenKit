using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.RED4.Types;

namespace WolvenKit.RED4.ShaderCache.Static;

public class Pass
{
    public required string Name;
    public uint Hash;

    public ulong HashVertex;
    public ulong HashPixel;
    public ulong HashCompute;
    public ulong HashRaytrace;

    public required SOMState SOMState;
    public List<RenderTargetSetup> RenderTargets = [];
    public List<Enums.StaticShaderInputLayout> InputLayouts = [];
}
