using System.Runtime.InteropServices;
using WolvenKit.RED4.ShaderCache.Common;
using WolvenKit.RED4.Types;

namespace WolvenKit.RED4.ShaderCache.Static;

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
internal struct FooterBlock
{
    public static readonly long Size = 0x20;

    [FieldOffset(0x00)] public uint PassCount;
    [FieldOffset(0x04)] public uint ShaderCount;
    // Opposite byte order from material cache
    [FieldOffset(0x08)] public uint TimestampDate;
    [FieldOffset(0x0C)] public uint TimestampTime;
    [FieldOffset(0x10)] public uint Magic;
    [FieldOffset(0x14)] public uint Version;
    // Assumed, need to confirm if checked
    [FieldOffset(0x18)] public ulong Checksum;
}


public struct CompiledShader
{
    public ulong Hash;
    public uint Size;
    public long Address;
}

// Pass info with a fixed size before the awful string-based encoding
[StructLayout(LayoutKind.Explicit, Size = 0x24)]
internal struct PassHeader
{
    [FieldOffset(0x00)] public uint Hash;

    [FieldOffset(0x04)] public ulong HashVertex;
    [FieldOffset(0x0C)] public ulong HashPixel;
    [FieldOffset(0x14)] public ulong HashCompute;
    [FieldOffset(0x1C)] public ulong HashRaytrace;
}


//------------------------------------------------------------------------------
// Packed structs of existing RED Classes

[StructLayout(LayoutKind.Explicit, Size = 0x08)]
internal struct Packed_PSODescRenderTarget : IPackedStruct<PSODescRenderTarget>
{
    [FieldOffset(0x00)] public byte BlendEnable;
    [FieldOffset(0x01)] public byte WriteMask;
    [FieldOffset(0x02)] public byte ColorOp;
    [FieldOffset(0x03)] public byte AlphaOp;
    [FieldOffset(0x04)] public byte DestFactor;
    [FieldOffset(0x05)] public byte DestAlphaFactor;
    [FieldOffset(0x06)] public byte SrcFactor;
    [FieldOffset(0x07)] public byte SrcAlphaFactor;

    public readonly PSODescRenderTarget ToREDClass()
    {
        return new PSODescRenderTarget
        {
            BlendEnable     = BlendEnable > 0,
            WriteMask       = (Enums.PSODescBlendModeWriteMask)WriteMask,
            ColorOp         = (Enums.PSODescBlendModeOp)ColorOp,
            AlphaOp         = (Enums.PSODescBlendModeOp)AlphaOp,
            DestFactor      = (Enums.PSODescBlendModeFactor)DestFactor,
            DestAlphaFactor = (Enums.PSODescBlendModeFactor)DestAlphaFactor,
            SrcFactor       = (Enums.PSODescBlendModeFactor)SrcFactor,
            SrcAlphaFactor  = (Enums.PSODescBlendModeFactor)SrcAlphaFactor,
        };
    }
}


[StructLayout(LayoutKind.Explicit, Size = 0x08)]
internal struct Packed_PSODescRasterizerModeDesc : IPackedStruct<PSODescRasterizerModeDesc>
{
    [FieldOffset(0x00)] public byte Wireframe;
    [FieldOffset(0x01)] public byte FrontWinding;
    [FieldOffset(0x02)] public byte CullMode;
    [FieldOffset(0x03)] public byte AllowMSAA;
    [FieldOffset(0x04)] public byte OffsetMode;
    [FieldOffset(0x05)] public byte Scissors;
    [FieldOffset(0x06)] public byte ConservativeRasterization;
    [FieldOffset(0x07)] public byte Valid;

    public readonly PSODescRasterizerModeDesc ToREDClass()
    {
        return new PSODescRasterizerModeDesc
        {
            Wireframe = Wireframe,
            FrontWinding = (Enums.PSODescRasterizerModeFrontFaceWinding)FrontWinding,
            CullMode = (Enums.PSODescRasterizerModeCullMode)CullMode,
            AllowMSAA = AllowMSAA,
            OffsetMode = (Enums.PSODescRasterizerModeOffsetMode)OffsetMode,
            Scissors = Scissors,
            ConservativeRasterization = ConservativeRasterization,
            Valid = Valid
        };
    }
}


[StructLayout(LayoutKind.Explicit, Size = 0x08)]
internal struct Packed_PSODescDepthStencilModeDesc : IPackedStruct<PSODescDepthStencilModeDesc>
{
    [FieldOffset(0x00)] public byte DepthTestEnable;
    [FieldOffset(0x01)] public byte DepthWriteEnable;
    [FieldOffset(0x02)] public byte DepthFunc;
    [FieldOffset(0x03)] public byte StencilEnable;
    [FieldOffset(0x04)] public byte StencilReadMask;
    [FieldOffset(0x05)] public byte StencilWriteMask;
    [FieldOffset(0x06)] public byte StencilPassOp;
    [FieldOffset(0x07)] public byte StencilFunc;

    public readonly PSODescDepthStencilModeDesc ToREDClass()
    {
        return new PSODescDepthStencilModeDesc
        {
            DepthTestEnable = DepthTestEnable,
            DepthWriteEnable = DepthWriteEnable,
            DepthFunc = (Enums.PSODescDepthStencilModeComparisonMode)DepthFunc,
            StencilEnable = StencilEnable,
            StencilReadMask = StencilReadMask,
            StencilWriteMask = StencilWriteMask,
            FrontFace = new PSODescStencilFuncDesc
            {
                StencilPassOp = (Enums.PSODescDepthStencilModeStencilOpMode)StencilPassOp,
                StencilFunc = (Enums.PSODescDepthStencilModeComparisonMode)StencilFunc
            }
        };
    }
}


internal struct Packed_PSODescBlendModeDesc : IPackedStruct<PSODescBlendModeDesc>
{
    public byte NumTargets;
    public byte Independent;
    public byte AlphaToCoverage;
    public Packed_PSODescRenderTarget[] RenderTargets;

    public readonly PSODescBlendModeDesc ToREDClass()
    {
        var ret = new PSODescBlendModeDesc
        {
            NumTargets = NumTargets,
            Independent = Independent,
            AlphaToCoverage = AlphaToCoverage,
            RenderTarget = new(8)
        };

        for (var i = 0; i < 8; i++)
        {
            ret.RenderTarget[i] = RenderTargets[i].ToREDClass();
        }

        return ret;
    }
}

internal struct Packed_SOMState : IPackedStruct<SOMState>
{
    public Packed_PSODescDepthStencilModeDesc DepthStencilModeDesc;
    public Packed_PSODescRasterizerModeDesc RasterizerModeDesc;
    public Packed_PSODescBlendModeDesc BlendModeDesc;
    public byte StencilReadMask;
    public byte StencilWriteMask;
    public byte StencilRef;

    public readonly SOMState ToREDClass()
    {
        return new SOMState
        {
            DepthStencilModeDesc = DepthStencilModeDesc.ToREDClass(),
            RasterizerModeDesc = RasterizerModeDesc.ToREDClass(),
            BlendModeDesc = BlendModeDesc.ToREDClass(),
            StencilReadMask = StencilReadMask,
            StencilWriteMask = StencilWriteMask,
            StencilRef = StencilRef
        };
    }
}
