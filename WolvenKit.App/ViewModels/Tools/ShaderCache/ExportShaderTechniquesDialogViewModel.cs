using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using WolvenKit.App.Helpers;
using WolvenKit.App.ViewModels.Dialogs;


namespace WolvenKit.App.ViewModels.Tools.ShaderCache;
public partial class ExportShaderTechniquesDialogViewModel : DialogViewModel
{
    public enum ExportFormat
    {
        [Description("Raw DXIL bitcode")]
        Raw_DXIL = 0,
        [Description("Disassembled DXIL")]
        Dis_DXIL,
        [Description("Converted SPIR-V bitcode")]
        Raw_SPIRV,
        [Description("Decompiled HLSL via SPIR-V")]
        Dec_HLSL
    }

    [Flags]
    public enum ShaderType
    {
        [Description("Vertex Shader")]
        Vertex = 1,
        [Description("Pixel Shader")]
        Pixel = 2,
        [Description("Both Shaders")]
        Both = 3
    }
    

    [ObservableProperty] private ExportFormat _format = ExportFormat.Raw_DXIL;
    [ObservableProperty] private ShaderType _type = ShaderType.Both;

    [NotifyPropertyChangedFor(nameof(CanExport))]
    [ObservableProperty] private string? _folder;

    // Dropdown selections
    [ObservableProperty] private IEnumerable<EnumMember<ExportFormat>>? _exportFormats;
    [ObservableProperty] private IEnumerable<EnumMember<ShaderType>>? _shaderTypes;

    //[ObservableProperty] private ObservableCollection

    public bool CanExport => !string.IsNullOrEmpty(Folder);

    public ExportShaderTechniquesDialogViewModel()
    {
        ExportFormats = EnumHelpers.EnumToItemSource<ExportFormat>();
        ShaderTypes = EnumHelpers.EnumToItemSource<ShaderType>();
    }
}
