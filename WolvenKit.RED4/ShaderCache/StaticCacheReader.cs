using System.Collections.Generic;
using System.Text;
using WolvenKit.Core.Extensions;
using WolvenKit.Core.Helpers;
using WolvenKit.RED4.IO;
using WolvenKit.RED4.Types;
using WolvenKit.RED4.ShaderCache.Common;
using WolvenKit.RED4.ShaderCache.Static;

namespace WolvenKit.RED4.ShaderCache;
public class StaticCacheReader : ICacheReader
{
    private readonly BinaryReader _reader;

    public StaticCacheReader(BinaryReader reader)
    {
        _reader = reader;
    }

    // 53 48 44 52 = 'SHDR'
    private static readonly uint s_magic = 0x53484452;
    private static readonly uint s_version = 0x08;
    
    public static bool IsSupportedFile(BinaryReader reader)
    {
        reader.BaseStream.Seek(-0x10, SeekOrigin.End);
        return reader.ReadUInt32() == s_magic && reader.ReadUInt32() == s_version;
    }

    public ICache ReadFile()
    {
        _reader.BaseStream.Seek(-FooterBlock.Size, SeekOrigin.End);
        var footer = _reader.BaseStream.ReadStruct<FooterBlock>();

        if (footer.Magic != s_magic)
        {
            throw new InvalidDataException("Unknown magic number");
        }

        if (footer.Version != s_version)
        {
            throw new InvalidDataException("Unsupported version number");
        }

        _reader.BaseStream.Seek(0, SeekOrigin.Begin);

        var passes = ReadPasses(footer.PassCount);
        var passSize = _reader.BaseStream.Position;

        var shaders = ReadShaders(footer.ShaderCount);
        var shaderSize = _reader.BaseStream.Position - passSize;

        var metadata = new CacheMetadata
        {
            FileSize = _reader.BaseStream.Length,
            Chunks =
            [
                new MetadataChunk { Type = "Passes", Count = footer.PassCount, Size = (ulong)passSize },
                new MetadataChunk { Type = "Shaders", Count = footer.ShaderCount, Size = (ulong)shaderSize }
            ]
        };

        return new StaticCache(
            shaders.Select(s => (s.Hash, s)).ToDictionary(),
            passes.Select(p => (p.Hash, p)).ToDictionary(),
            metadata
        );
    }

    //--------------------------------------------------------------------------
    // Passes

    private List<Pass> ReadPasses(uint count)
    {
        var passes = new List<Pass>();

        for (var i = 0; i < count; i++)
        {
            passes.Add(ReadPass());
        }

        return passes;
    }

    private Pass ReadPass()
    {
        var ph = _reader.BaseStream.ReadStruct<PassHeader>();
        var somState = ReadSOMState();

        var pass = new Pass
        {
            Name = string.Empty,
            Hash = ph.Hash,
            HashVertex = ph.HashVertex,
            HashPixel = ph.HashPixel,
            HashCompute = ph.HashCompute,
            HashRaytrace = ph.HashRaytrace,
            SOMState = somState,
            RenderTargets = [],
            InputLayouts = []
        };

        // now for the awful encoding
        var rtCount = _reader.ReadUInt32();
        for (var i = 0; i < rtCount; i++)
        {
            // unused byte, flag maybe? always zero
            _ = _reader.ReadByte();
            
            var rtFormats = new List<string>();
            var dsFormat = string.Empty;

            var fieldName = _reader.ReadLengthPrefixedString();

            while (fieldName != "None" && !string.IsNullOrEmpty(fieldName))
            {
                // FieldType: always "static:8,GpuWrapApieTextureFormat"
                _ = _reader.ReadLengthPrefixedString();
                // Size: unneeded since we're only reading length-prefixed strings
                _ = _reader.ReadUInt32();

                switch (fieldName)
                {
                    case "rtFormats":
                        var count = _reader.ReadUInt32();
                        for (var c = 0; c < count; c++)
                        {
                            rtFormats.Add(_reader.ReadLengthPrefixedString());
                        }
                        break;
                    case "dsFormat":
                        dsFormat = _reader.ReadLengthPrefixedString();
                        break;
                    default:
                        throw new InvalidDataException($"Found invalid field name: '{fieldName}'");
                }

                fieldName = _reader.ReadLengthPrefixedString();
            }

            // Convert this string nonsense into enums
            pass.RenderTargets.Add(new RenderTargetSetup
            {
                RTFormats = [.. rtFormats.Select(r => Enum.Parse<Enums.GpuWrapApieTextureFormat>(r))],
                DSFormat = string.IsNullOrEmpty(dsFormat)
                    ? Enums.GpuWrapApieTextureFormat.SomeMagicBullshit
                    : Enum.Parse<Enums.GpuWrapApieTextureFormat>(dsFormat)
            });
        }

        var ilCount = _reader.ReadUInt32();
        for (var i = 0; i < ilCount; i++)
        {
            pass.InputLayouts.Add(_reader.ReadLengthPrefixedString());
        }

        pass.Name = _reader.ReadLengthPrefixedString();

        return pass;
    }

    private SOMState ReadSOMState()
    {
        var state = new Packed_SOMState();

        state.DepthStencilModeDesc = _reader.BaseStream.ReadStruct<Packed_PSODescDepthStencilModeDesc>();
        state.RasterizerModeDesc = _reader.BaseStream.ReadStruct<Packed_PSODescRasterizerModeDesc>();
        state.BlendModeDesc = new Packed_PSODescBlendModeDesc
        {
            NumTargets = _reader.ReadByte(),
            Independent = _reader.ReadByte(),
            AlphaToCoverage = _reader.ReadByte(),
            RenderTargets = new Packed_PSODescRenderTarget[8]
        };

        for (var rt = 0; rt < 8; rt++ )
        {
            state.BlendModeDesc.RenderTargets[rt] = _reader.BaseStream.ReadStruct<Packed_PSODescRenderTarget>();
        }

        state.StencilReadMask = _reader.ReadByte();
        state.StencilWriteMask = _reader.ReadByte();
        state.StencilRef = _reader.ReadByte();

        return state.ToREDClass();
    }

    //--------------------------------------------------------------------------
    // Shaders

    private List<Shader> ReadShaders(uint count)
    {
        var shaders = new List<Shader>();

        for (var i = 0; i < count; i++)
        {
            shaders.Add(ReadCompiledShader());
        }

        return shaders;
    }

    private Shader ReadCompiledShader()
    {
        Shader s = new()
        {
            Hash = _reader.ReadUInt64(),
            Size = _reader.ReadUInt32(),
            // No point storing the actual compiled data, will just cache the file address and seek past
            Address = _reader.BaseStream.Position
        };
        _reader.BaseStream.Seek(s.Size, SeekOrigin.Current);

        return s;
    }
}
