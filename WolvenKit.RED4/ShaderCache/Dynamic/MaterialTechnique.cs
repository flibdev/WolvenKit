using System.Text.RegularExpressions;
using WolvenKit.RED4.Types;

namespace WolvenKit.RED4.ShaderCache.Dynamic;

public struct TechniqueDesc
{
    public uint Index;
    public string Pass;
    public byte PassIndex;
    public byte FallbackIndex;
    public Enums.EMaterialVertexFactory VertexFactory;
    public bool IsDiscarded;
    public bool IsDismembered;
    public bool IsPreskinned;
}


public struct MaterialTechnique
{
    public TechniqueDesc Desc;
    public Shader? VertexShader;
    public Shader? PixelShader;

    public List<SamplerStateInfo> VSSamplers;
    public List<SamplerStateInfo> PSSamplers;
}


public partial class TechniqueDescExt
{
    public const uint Flag_Dismembered = 0x04;
    public const uint Flag_Discarded   = 0x02;
    public const uint Flag_Preskinned  = 0x01;
    public static bool HasFlag(uint value, uint flag) => (value & flag) == flag;

    /// <summary>
    /// Attempts to parse the CompiledTechnique string and extract the description
    /// </summary>
    public static bool TryParse(string input, out TechniqueDesc techDesc)
    {
        techDesc = new TechniqueDesc();
        var match = CompiledRegex().Match(input);
        if (match.Success == false)
        {
            return false;
        }

        try
        {
            techDesc.Index         = uint.Parse(match.Groups["index"].Value);
            techDesc.Pass          = match.Groups["pass"].Value;
            techDesc.PassIndex     = byte.Parse(match.Groups["pass_idx"].Value);
            techDesc.FallbackIndex = byte.Parse(match.Groups["fallback"].Value);

            // The VertexFactory is stored within the VFID value
            // with the lower 3 bits storing the 3 bit flags
            var vfid = uint.Parse(match.Groups["vfid"].Value);
            techDesc.VertexFactory = (Enums.EMaterialVertexFactory)(vfid >> 3);
            techDesc.IsDiscarded   = HasFlag(vfid, Flag_Discarded);
            techDesc.IsDismembered = HasFlag(vfid, Flag_Dismembered);
            techDesc.IsPreskinned  = HasFlag(vfid, Flag_Preskinned);

            return true;
        }
        catch
        {
            return false;
        }        
    }

    /* The details of each compiled technique are thankfully stored in plain-text
     * but unused by the game since the techniques are found by a hash lookup
     *
     * Stored strings look like:
     * CompiledTechnique [Index: 0, Pass 'renderstage_', PassIndex: 0, Fallback: 0, RenderStageContext: [ID: 18, VF: MeshStatic; Flags]
     * 
     * The used Vertex Factory and flags are encoded in the ID value as well as
     * plain-text, but decoding them from the ID is faster than string parsing
    */ 
    [GeneratedRegex(
        @"CompiledTechnique \[Index: (?<index>\d+), Pass '(?<pass>[^']+)', PassIndex: (?<pass_idx>\d+), Fallback: (?<fallback>\d+), RenderStageContext: \[ID: (?<vfid>\d+)",
        RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant
    )]
    private static partial Regex CompiledRegex();
}
