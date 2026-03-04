using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using WolvenKit.RED4.ShaderCache.Dynamic;

namespace WolvenKit.App.ViewModels.Tools.ShaderCache
{
    public class MaterialViewModel : ObservableObject
    {
        public required string Name { get; set; }
        public required string Hash { get; set; }
        public string? FilePath { get; set; }
        public int TechniqueCount { get; set; }

        public List<RED4.Types.Enums.EMaterialVertexFactory> VertexFactories { get; set; } = [];

        public List<MaterialTechniqueViewModel> Techniques { get; set; } = [];
    }

    public class MaterialTechniqueViewModel : ObservableObject
    {
        public uint CompositeSort { get; set; }
        public required string MatName { get; set; }
        public uint Index { get; set; }
        public uint PassIndex { get; set; }
        public string Pass { get; set; }
        public RED4.Types.Enums.EMaterialVertexFactory VertexFactory { get; set; }
        public bool IsDiscarded { get; set; }
        public bool IsDismembered { get; set; }
        public bool IsPreskinned { get; set; }
        public ulong? VSHash { get; set; }
        public ulong? PSHash { get; set; }

        [SetsRequiredMembers]
        public MaterialTechniqueViewModel(string matName, MaterialTechnique tech)
        {
            MatName = matName;
            Index = tech.Desc.Index;
            PassIndex = tech.Desc.PassIndex;
            Pass = tech.Desc.Pass;
            VertexFactory = tech.Desc.VertexFactory;
            IsDiscarded = tech.Desc.IsDiscarded;
            IsDismembered = tech.Desc.IsDismembered;
            IsPreskinned = tech.Desc.IsPreskinned;
            VSHash = tech.VertexShader?.Hash ?? null;
            PSHash = tech.PixelShader?.Hash ?? null;

            // Similar to how the VFID is used in the cache file itself,
            // but ordered in a way that enables useful sorting.
            // 6: Vertex Factory
            // 8: Index
            // 8: PassIndex
            // 1: IsDismembered
            // 1: IsPreskinned
            // 1: IsDiscarded
            CompositeSort = (uint)VertexFactory << 19;
            CompositeSort |= Index << 11;
            CompositeSort |= PassIndex << 3;
            if (IsDismembered)
            {
                CompositeSort |= 4;
            }
            if (IsPreskinned)
            {
                CompositeSort |= 2;
            }
            if (IsDiscarded)
            {
                CompositeSort |= 1;
            }
        }
    }
}
