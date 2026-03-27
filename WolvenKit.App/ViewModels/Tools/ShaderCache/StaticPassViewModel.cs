using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using WolvenKit.App.ViewModels.Shell;
using WolvenKit.RED4.ShaderCache.Dynamic;
using WolvenKit.RED4.ShaderCache.Static;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.ViewModels.Tools.ShaderCache
{
    public class StaticPassViewModel : ObservableObject
    {
        public required string Name { get; set; }
        public required string Hash { get; set; }

        public required string HashVertex { get; set; }
        public required string HashPixel { get; set; }
        public required string HashCompute { get; set; }
        public required string HashRaytrace { get; set; }

        public required string InputLayouts { get; set; }

        public List<RenderTarget>? RenderTargets { get; set; }

        public required List<ChunkViewModel> SOMStateVM { get; set; }
    }

    public class RenderTarget : ObservableObject
    {
        public required string RTFormats { get; set; }
        public required string DSFormat { get; set; }

        public static RenderTarget FromRTSetup(RenderTargetSetup setup)
        {
            return new RenderTarget
            {
                RTFormats = string.Join(", ", setup.RTFormats) ?? string.Empty,
                DSFormat = setup.DSFormat == Enums.GpuWrapApieTextureFormat.SomeMagicBullshit
                    ? "None"
                    : setup.DSFormat.ToString()
            };
        }
    }
}
