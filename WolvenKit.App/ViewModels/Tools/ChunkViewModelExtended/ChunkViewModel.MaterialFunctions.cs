using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using WolvenKit.RED4.Types;

// ReSharper disable once CheckNamespace
namespace WolvenKit.App.ViewModels.Shell;

public partial class ChunkViewModel
{
    #region Properties

    #endregion

    public bool CalculateParameterBlockSizes(CMaterialTemplate mt)
    {
        // Should only ever be 3
        CArrayFixedSize<CUInt32> blockSize = new(mt.Parameters.Count);
        var dirty = false;

        for (var i = 0; i < mt.Parameters.Count; i++)
        {
            ulong size = 0;

            foreach (var p in mt.Parameters[i])
            {
                size += p.Chunk switch
                {
                    // Simple values stored fully in block
                    CMaterialParameterColor             => 4,
                    CMaterialParameterScalar            => 4,
                    CMaterialParameterVector            => 16,

                    // More complex values stored as pointers
                    CMaterialParameterCpuNameU64        => 8,
                    CMaterialParameterCube              => 8,
                    CMaterialParameterDynamicTexture    => 8,
                    CMaterialParameterFoliageParameters => 8,
                    CMaterialParameterGradient          => 8,
                    CMaterialParameterHairParameters    => 8,
                    CMaterialParameterMultilayerMask    => 8,
                    CMaterialParameterMultilayerSetup   => 8,
                    CMaterialParameterSkinParameters    => 8,
                    CMaterialParameterStructBuffer      => 8,
                    CMaterialParameterTerrainSetup      => 8,
                    CMaterialParameterTexture           => 8,
                    CMaterialParameterTextureArray      => 8,

                    _ => 0,
                };
            }

            blockSize[i] = (CUInt32)size;
            if (blockSize[i] != mt.ParamBlockSize[i])
            {
                dirty = true;
            }

            _loggerService.Debug($"Parameters[{i}] size = {size}");
        }

        if (dirty)
        {
            mt.ParamBlockSize = blockSize;
        }

        return dirty;
    }

}

