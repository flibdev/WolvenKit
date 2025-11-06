using System.Collections.Generic;
using System.Text;
using WolvenKit.Core.Extensions;
using WolvenKit.RED4.IO;
using WolvenKit.RED4.ShaderCache.Dynamic;

namespace WolvenKit.RED4.ShaderCache;
public class MaterialCacheReader : Red4Reader, ICacheReader
{
    public MaterialCacheReader(Stream input) : base(input) { }
    public MaterialCacheReader(Stream input, Encoding encoding) : base(input, encoding) { }
    public MaterialCacheReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }
    public MaterialCacheReader(BinaryReader reader) : base(reader) { }

    // 53 48 44 52 = 'SHDR'
    private static readonly uint s_magic = 0x53484452;
    private static readonly uint s_version = 0x0A;
    
    public static bool IsSupportedFile(BinaryReader reader)
    {
        reader.BaseStream.Seek(-0x08, SeekOrigin.End);
        return reader.ReadUInt32() == s_magic && reader.ReadUInt32() == s_version;
    }


    public ICacheReader.ReadState TryReadFile(out ICache? cache)
    {
        BaseStream.Seek(-0x70, SeekOrigin.End);
        var footer = BaseStream.ReadStruct<FooterBlock>();
        cache = null;

        if (footer.Magic != s_magic)
        {   
            return ICacheReader.ReadState.UnknownMagic;
        }

        if (footer.Version != s_version)
        {
            return ICacheReader.ReadState.UnsupportedVersion;
        }

        var cShaders = ReadCompiledShaders(footer.ShaderCount, 0, footer.ShaderBlockSize);
        var cTechs = ReadCompiledTechniques(footer.TechniqueCount, footer.OffsetToTechniques, footer.TechniqueBlockSize);
        var paramMap = ReadShaderParamsMap(footer.ParamCount, footer.OffsetToParams, footer.ParamBlockSize);
        // Current don't care about the timestamp or includes blocks


        // Merge CompiledShaders and ShaderParams
        var shaders = new Dictionary<ulong, Shader>();
        foreach (var cs in cShaders)
        {
            var shader = new Shader
            {
                Hash = cs.Hash,
                Size = cs.Size,
                Address = cs.Address
            };
            if (paramMap.TryGetValue(cs.ParamsHash, out var sp))
            {
                shader.MatModMask = sp.MatModMask;
                shader.Params = sp.Params;
            }

            shaders.Add(cs.Hash, shader);
        }

        // Generate material list from compiled techniques
        var materials = new Dictionary<uint, Material>();
        foreach (var tech in cTechs)
        {
            var matHash = (uint)(tech.Hash >> 32);
            var firstSpace = tech.Name.IndexOf(' ');
            var matName = tech.Name.Substring(0, firstSpace);
            var techStr = tech.Name.Substring(firstSpace + 1);

            Material? mat;
            if (materials.ContainsKey(matHash))
            {
                mat = materials[matHash];
            }
            else
            {
                mat = new Material(matName);
                materials.Add(matHash, mat);
            }

            if (!TechniqueDescExt.TryParse(techStr, out TechniqueDesc techDesc))
            {
                throw new Exception($"MaterialCacheReader: Unable to parse TechniqueDesc '{techStr}'");
            }

            mat.AddTechnique(new MaterialTechnique
            {
                Desc = techDesc,
                VertexShader = tech.VSHash == 0 ? null : shaders[tech.VSHash],
                PixelShader = tech.PSHash == 0 ? null : shaders[tech.PSHash],
                VSSamplers = tech.VSSamplers,
                PSSamplers = tech.PSSamplers
            });
        }

        cache = new MaterialCache()
        {
            Shaders = shaders,
            Materials = materials
        };

        return ICacheReader.ReadState.Success;
    }

    //--------------------------------------------------------------------------
    // Common

    private static void ThrowIfMismatch(string section, long position, long offset, ulong size)
    {
        var addr = offset + (long)size;
        if (position != addr)
        {
            throw new Exception($"MaterialCacheReader: {section} block size mismatch - expected {addr}, found {position}");
        }
    }

    //--------------------------------------------------------------------------
    // Shaders

    private List<CompiledShader> ReadCompiledShaders(uint count, long offset, ulong size)
    {
        BaseStream.Seek(offset, SeekOrigin.Begin);
        var shaders = new List<CompiledShader>();

        for (var i = 0; i < count; i++)
        {
            shaders.Add(ReadCompiledShader());
        }

        ThrowIfMismatch("CompiledShader", Position, offset, size);

        return shaders;
    }

    private CompiledShader ReadCompiledShader()
    {
        CompiledShader cs = new();
        cs.Hash = _reader.ReadUInt64();
        cs.ParamsHash = _reader.ReadUInt64();
        cs.Size = _reader.ReadUInt32();
        // No point storing the actual compiled data, will just cache the file address and seek past
        cs.Address = Position;
        _reader.BaseStream.Seek(cs.Size, SeekOrigin.Current);

        return cs;
    }

    //--------------------------------------------------------------------------
    // Compiled Techniques

    private List<CompiledTechnique> ReadCompiledTechniques(uint count, long offset, ulong size)
    {
        BaseStream.Seek(offset, SeekOrigin.Begin);
        var techs = new List<CompiledTechnique>();

        for (var i = 0; i < count; i++)
        {
            techs.Add(ReadCompiledTechnique());
        }

        ThrowIfMismatch("CompiledTechnique", Position, offset, size);

        return techs;
    }

    private CompiledTechnique ReadCompiledTechnique()
    {
        var ct = new CompiledTechnique();
        ct.Hash = _reader.ReadUInt64();
        ct.Name = _reader.ReadLengthPrefixedString();

        // Some hash/checksum, ignored by the game
        _reader.ReadUInt32();

        ct.VSHash = _reader.ReadUInt64();
        ct.PSHash = _reader.ReadUInt64();

        // More hash/checksums, ignored by the game
        _reader.ReadUInt64();
        _reader.ReadUInt64();

        var time = _reader.ReadUInt32();
        var date = _reader.ReadUInt32();
        ct.Timestamp = new Common.Timestamp(date, time);

        // A different timestamp stored in another table
        // Is ignored by the game but I'm keeping it
        ct.TimestampHash = _reader.ReadUInt32();

        ct.VSSamplers = [];
        var vsCount = _reader.ReadUInt32();
        for (var v = 0; v < vsCount; v++)
        {
            var vss = BaseStream.ReadStruct<SamplerState>();
            ct.VSSamplers.Add(vss.ToREDClass());
        }

        ct.PSSamplers = [];
        var psCount = _reader.ReadUInt32();
        for (var p = 0; p < psCount; p++)
        {
            var pss = BaseStream.ReadStruct<SamplerState>();
            ct.VSSamplers.Add(pss.ToREDClass());
        }

        return ct;
    }

    //--------------------------------------------------------------------------
    // Shader Params

    private Dictionary<ulong, ShaderParams> ReadShaderParamsMap(uint count, long offset, ulong size)
    {
        BaseStream.Seek(offset, SeekOrigin.Begin);
        var spm = new Dictionary<ulong, ShaderParams>();

        for (var i = 0; i < count; i++)
        {
            var sp = ReadShaderParams();
            spm.Add(sp.Hash, sp);
        }

        ThrowIfMismatch("ShaderParams", Position, offset, size);

        return spm;
    }

    private ShaderParams ReadShaderParams()
    {
        var sp = new ShaderParams();

        sp.Hash = _reader.ReadUInt64();
        sp.MatModMask = _reader.ReadUInt32();

        var count = _reader.ReadUInt32();
        sp.Params = [];
        for (var p = 0; p < count; p++)
        {
            sp.Params.Add(new ShaderParam
            {
                Name  = _reader.ReadLengthPrefixedString(),
                Value = _reader.ReadByte(),
                Size  = _reader.ReadByte(),
            });
        }

        return sp;
    }
}
