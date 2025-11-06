using System.Runtime.InteropServices;
using WolvenKit.RED4.ShaderCache.Common;
using WolvenKit.RED4.Types;

namespace WolvenKit.RED4.ShaderCache.Dynamic;

[StructLayout(LayoutKind.Explicit, Size = 0x70)]
internal struct FooterBlock
{
    [FieldOffset(0x00)]
    public uint ShaderCount;
    [FieldOffset(0x04)]
    public uint TechniqueCount;
    [FieldOffset(0x08)]
    public uint ParamCount;
    // After the timestamp
    [FieldOffset(0x1C)]
    public uint IncludesCount;

    // Some kind of hash or checksum, unused by the game
    [FieldOffset(0x0C)]
    public ulong UnusedHash;

    [FieldOffset(0x14)]
    public uint TimestampTime;
    [FieldOffset(0x18)]
    public uint TimestampDate;

    [FieldOffset(0x20)]
    public ulong ShaderBlockSize;
    [FieldOffset(0x28)]
    public ulong TechniqueBlockSize;
    [FieldOffset(0x30)]
    public ulong ParamBlockSize;
    [FieldOffset(0x38)]
    public ulong IncludesBlockSize;
    [FieldOffset(0x40)]
    public ulong TimestampBlockSize;

    [FieldOffset(0x48)]
    public long OffsetToTechniques;
    [FieldOffset(0x50)]
    public long OffsetToParams;
    // These two are stored in reverse order of the block sizes
    [FieldOffset(0x58)]
    public long OffsetToTimestamps;
    [FieldOffset(0x60)]
    public long OffsetToIncludes;

    [FieldOffset(0x68)]
    public uint Magic;
    [FieldOffset(0x6C)]
    public uint Version;
}


public struct CompiledShader
{
    public ulong Hash;
    public ulong ParamsHash;
    public uint Size;
    public long Address;
}


internal struct CompiledTechnique
{
    public ulong Hash;
    public string Name;
    public Timestamp Timestamp;
    public ulong VSHash;
    public ulong PSHash;
    public uint TimestampHash;

    public List<SamplerStateInfo> VSSamplers;
    public List<SamplerStateInfo> PSSamplers;
}


public struct ShaderParam
{
    public CString Name;
    public byte Value;
    public byte Size;
}

internal struct ShaderParams
{
    public ulong Hash;
    public uint MatModMask;
    public List<ShaderParam> Params;
}

[StructLayout(LayoutKind.Explicit, Size = 0x10)]
internal struct MaterialTimestamp
{
    [FieldOffset(0x0)]
    public ulong Hash;
    [FieldOffset(0x8)]
    public uint TimestampTime;
    [FieldOffset(0xC)]
    public uint TimestampDate;
}

[StructLayout(LayoutKind.Explicit, Size = 0x08)]
internal struct SamplerState
{
    [FieldOffset(0x0)]
    public byte FilteringMin;
    [FieldOffset(0x1)]
    public byte FilteringMag;
    [FieldOffset(0x2)]
    public byte FilteringMip;
    [FieldOffset(0x3)]
    public byte AddressU;
    [FieldOffset(0x4)]
    public byte AddressV;
    [FieldOffset(0x5)]
    public byte AddressW;
    [FieldOffset(0x6)]
    public byte ComparisonFunc;
    [FieldOffset(0x7)]
    public byte Register;

    public SamplerStateInfo ToREDClass()
    {
        return new SamplerStateInfo
        {
            FilteringMin = (Enums.ETextureFilteringMin)FilteringMin,
            FilteringMag = (Enums.ETextureFilteringMag)FilteringMag,
            FilteringMip = (Enums.ETextureFilteringMip)FilteringMip,
            AddressU = (Enums.ETextureAddressing)AddressU,
            AddressV = (Enums.ETextureAddressing)AddressV,
            AddressW = (Enums.ETextureAddressing)AddressW,
            ComparisonFunc = (Enums.ETextureComparisonFunction)ComparisonFunc,
            Register = Register
        };
    }
}
