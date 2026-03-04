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

public enum ExportFormats
{
    [Display(Description = "Raw DXIL bitcode")]
    Raw_DXIL = 0,
    [Display(Description = "Disassembled DXIL")]
    Dis_DXIL,
    [Display(Description = "Converted SPIR-V bitcode")]
    Raw_SPIRV,
    [Display(Description = "Decompiled HLSL via SPIR-V")]
    Dec_HLSL
}

[Flags]
public enum ShaderTypes : byte
{
    [Display(Description = "Vertex Shader")]
    Vertex = 1,
    [Display(Description = "Pixel Shader")]
    Pixel = 2,
    [Display(Description = "Both Shaders")]
    Both = Vertex | Pixel
}

public partial class ExportShaderTechniquesDialogViewModel : DialogViewModel
{
    [NotifyPropertyChangedFor(nameof(CanExport))]
    [ObservableProperty] private ExportFormats? _exportFormat;

    [NotifyPropertyChangedFor(nameof(CanExport))]
    [ObservableProperty] private ShaderTypes? _shaderType;

    [NotifyPropertyChangedFor(nameof(CanExport))]
    [ObservableProperty] private string? _folder;

    [NotifyPropertyChangedFor(nameof(CanExport))]
    [ObservableProperty] private string? _filenameTemplate;

    // Dropdown selections
    [ObservableProperty] private List<ExportFormats>? _exportFormatList;
    [ObservableProperty] private List<ShaderTypes>? _shaderTypeList;
    [ObservableProperty] private List<string>? _filenameTemplateList;

    //[ObservableProperty] private ObservableCollection

    public bool CanExport => ExportFormat != null
                          && ShaderType != null
                          && !string.IsNullOrEmpty(Folder)
                          && !string.IsNullOrEmpty(FilenameTemplate);

    public ExportShaderTechniquesDialogViewModel()
    {
        ExportFormatList = [.. Enum.GetValues<ExportFormats>()];

        ShaderTypeList = [.. Enum.GetValues<ShaderTypes>()];

        FilenameTemplateList = [
            "{Material}_{SortID}_{VF}_{Type}{Flags}",
            "{VertexFactory}{Flags}_{Material}_{Type}"
        ];
        FilenameTemplate = FilenameTemplateList[0];
    }
}
