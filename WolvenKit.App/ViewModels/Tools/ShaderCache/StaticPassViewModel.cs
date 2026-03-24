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
    public class StaticPassViewModel : ObservableObject
    {
        public required string Name { get; set; }
        public required string Hash { get; set; }

        public required string HashVertex { get; set; }
        public required string HashPixel { get; set; }
        public required string HashCompute { get; set; }
        public required string HashRaytrace { get; set; }
    }
}
