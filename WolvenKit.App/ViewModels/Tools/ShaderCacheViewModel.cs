using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.SharpDX.Core;
using SharpDX;
using Splat.ModeDetection;
using WolvenKit.App.Extensions;
using WolvenKit.App.Interaction;
using WolvenKit.App.Models;
using WolvenKit.App.Services;
using WolvenKit.App.ViewModels.Shell;
using WolvenKit.App.ViewModels.Tools.ShaderCache;
using WolvenKit.Common.Services;
using WolvenKit.Core.Extensions;
using WolvenKit.Core.Interfaces;
using WolvenKit.RED4.ShaderCache.Common;
using WolvenKit.RED4.ShaderCache.Dynamic;
using WolvenKit.RED4.Types;
using WolvenKit.ShaderTools;
using static WolvenKit.RED4.Types.Enums;
using Material = WolvenKit.RED4.ShaderCache.Dynamic.Material;

namespace WolvenKit.App.ViewModels.Tools;

public class CacheMaterialKey
{
    public string Name { get; set; }
    public uint Hash { get; set; }

    public CacheMaterialKey(string name, uint hash) {  Name = name; Hash = hash; }
}

public partial class ShaderCacheViewModel : FloatingPaneViewModel
{
    /// <summary>
    /// Identifies the <see ref="ContentId"/> of this tool window.
    /// </summary>
    public const string ToolContentId = "ShaderCache_Tool";

    /// <summary>
    /// Identifies the caption string used for this tool window.
    /// </summary>
    public const string ToolTitle = "Shader Cache Browser";

    /// <summary>
    /// GUID to track file open dialog history separately from main app
    /// </summary>
    private readonly Guid _dialogGuid = new("ff7f9eba-54a9-4bf4-bc7f-a6985dd32caa");


    [ObservableProperty] private bool _isLoaded = false;
    [ObservableProperty] private bool _isDynamicCache = true;
    [ObservableProperty] private string _statusStr = "No Cache File Loaded";

    [ObservableProperty] private CacheMetadata? _cacheMetadata = null;

    [ObservableProperty] private List<CacheMaterialKey> _materials = [];
    [ObservableProperty] private string _materialCount = "Materials";
    [ObservableProperty] private CacheMaterialKey? _selectedMaterialKey = null;
    [ObservableProperty] private Material? _selectedMaterial = null;
    [ObservableProperty] private MaterialViewModel? _selectedMaterialVM = null;

    [ObservableProperty] private List<MaterialTechniqueViewModel> _techniques = [];
    [ObservableProperty] private ObservableCollection<object>? _selectedTechniques = [];

    [ObservableProperty] private string _techniqueCount = "Techniques";


    private readonly ISettingsManager _settingsManager;
    private readonly IShaderCacheService _shaderCacheService;
    private readonly IAppArchiveManager _appArchiveManager;
    private readonly AppViewModel _appViewModel;
    private readonly ILoggerService _log;

    public ShaderCacheViewModel(
        ISettingsManager settingsManager,
        IShaderCacheService shaderCacheService,
        IAppArchiveManager appArchiveManager,
        AppViewModel appViewModel,
        ILoggerService loggerService
        ) : base(ToolTitle, ToolContentId)
    {
        _settingsManager = settingsManager;
        _shaderCacheService = shaderCacheService;
        _appArchiveManager = appArchiveManager;
        _appViewModel = appViewModel;
        _log = loggerService;
    }


    private void ClearLoadedCache()
    {
        CacheMetadata = null;
        Materials?.Clear();
        IsLoaded = false;
        StatusStr = "No Cache File Loaded";
    }

    [RelayCommand]
    private void LoadShaderCache()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            DefaultExt = "*.cache",
            Filter = "Shader Cache (.cache)|*.cache",
            ClientGuid = _dialogGuid,
            DefaultDirectory = Path.Combine(_settingsManager.GetRED4GameRootDir(), "engine")
        };

        var result = dialog.ShowDialog();
        if (result == true)
        {
            ClearLoadedCache();
            StatusStr = dialog.FileName;
            _log.Info($"Attempting to load shader cache '{dialog.FileName}'");
            _shaderCacheService.OnLoad += ShaderCacheService_OnLoad;
            _shaderCacheService.LoadCache(dialog.FileName);
        }
    }

    private void ShaderCacheService_OnLoad(object? sender, EventArgs e)
    {
        _shaderCacheService.OnLoad -= ShaderCacheService_OnLoad;

        if (e is not ShaderCacheService.OnLoadArgs loadArgs)
        {
            return;
        }

        if (!loadArgs.Success)
        {
            StatusStr = $"Could not load: {loadArgs.Reason}";
            _log.Error($"Could not load shader cache file: {loadArgs.Reason}");
            return;
        }

        _log.Success($"Loaded shader cache.");

        var cache = loadArgs.Cache as MaterialCache;
        if (cache != null)
        {
            IsLoaded = true;
            IsDynamicCache = true;
            CacheMetadata = cache.Metadata;

            // do stuff
            Materials = cache.Materials
                .Select(kvp => new CacheMaterialKey(kvp.Value.Name, kvp.Key))
                .OrderBy(mk => mk.Name)
                .ToList();

            MaterialCount = $"Materials ({Materials.Count})";

            Techniques = cache.Materials
                .SelectMany(kvp =>
                    kvp.Value.Techniques.Select(
                        t => new MaterialTechniqueViewModel(kvp.Value.Name, t)
                    )                    
                )
                .OrderBy(mt => mt.MatName)
                .ThenBy(mt => mt.CompositeSort)
                .ToList();
            TechniqueCount = $"Techniques ({Techniques.Count})";
        }

    }

    public void SelectMaterial(uint hash)
    {
        var cache = _shaderCacheService?.Cache as MaterialCache;
        SelectedMaterial = cache?.Materials[hash];
    }

    partial void OnSelectedMaterialKeyChanged(CacheMaterialKey? value)
    {
        SelectedMaterialVM = null;

        if (value != null)
        {
            var cache = _shaderCacheService.Cache as MaterialCache;
            if (cache != null && cache.Materials.TryGetValue(value.Hash, out var material))
            {
                SelectedTechniques = [];

                SelectedMaterialVM = new MaterialViewModel
                {
                    Hash = value.Hash.ToString("X8"),
                    Name = material.Name,
                    FilePath = _shaderCacheService.GetMaterialByName(material.Name)?.FileName,
                    TechniqueCount = material.Techniques.Count,
                    VertexFactories = material.VertexFactories
                        .OrderBy(vf => vf)
                        .ToList(),
                    Techniques = material.Techniques
                        .Select(t => new MaterialTechniqueViewModel(material.Name, t))
                        .OrderBy(mt => mt.CompositeSort)
                        .ToList()
                };
            }
        }
    }

    [RelayCommand]
    private void ShowMaterialTemplate()
    {
        if (SelectedMaterialVM?.FilePath == null)
        {
            return;
        }

        var gamefile = _appArchiveManager.GetGameFile(SelectedMaterialVM.FilePath);

        if (gamefile != null)
        {
            var assetBrowser = _appViewModel.GetToolViewModel<AssetBrowserViewModel>();
            assetBrowser.IsVisible = true;
            assetBrowser.ShowFileByHash(gamefile.Key);
        }
    }

    private bool HasFeatFlag(FeatureFlagsMask val, EFeatureFlagMask flag) => ((ulong)val.Flags & (ulong)flag) != 0;

    [RelayCommand]
    private void CalculateShaderTechniques()
    {
        if (SelectedMaterialVM?.FilePath == null)
        {
            return;
        }

        var gamefile = _appArchiveManager.GetCR2WFile(SelectedMaterialVM.FilePath);
        var techTotal = 0;

        if (gamefile != null && gamefile.RootChunk is CMaterialTemplate mt)
        {
            var techCount = mt.Techniques
                .Where(t => !HasFeatFlag(t.FeatureFlagsEnabledMask, EFeatureFlagMask.HitProxies))
                .Where(t => !HasFeatFlag(t.FeatureFlagsEnabledMask, EFeatureFlagMask.Overdraw))
                .Select(t => t.Passes.Count)
                .Sum();

            foreach (var vf in mt.VertexFactories)
            {
                var techMult = 0;

                switch ((EMaterialVertexFactory)vf)
                {
                    // No flags
                    case EMaterialVertexFactory.MVF_Debug:
                    case EMaterialVertexFactory.MVF_Fullscreen:
                    case EMaterialVertexFactory.MVF_ParticleSphereAligned:
                        techMult = 1;
                        break;

                    // Regular + Discard
                    case EMaterialVertexFactory.MVF_Terrain:
                    case EMaterialVertexFactory.MVF_MeshSpeedTree:
                    case EMaterialVertexFactory.MVF_Decal:
                    case EMaterialVertexFactory.MVF_MeshProcedural:
                    case EMaterialVertexFactory.MVF_MeshProxy:
                        techMult = 2;
                        break;

                    // Regular + Preskinned
                    case EMaterialVertexFactory.MVF_ParticleBilboard:
                    case EMaterialVertexFactory.MVF_ParticleParallel:
                    case EMaterialVertexFactory.MVF_ParticleMotionBlur:
                    case EMaterialVertexFactory.MVF_ParticleVerticalFixed:
                    case EMaterialVertexFactory.MVF_ParticleTrail:
                    case EMaterialVertexFactory.MVF_ParticleFacingTrail:
                    case EMaterialVertexFactory.MVF_ParticleScreen:
                    case EMaterialVertexFactory.MVF_ParticleBeam:
                    case EMaterialVertexFactory.MVF_ParticleFacingBeam:
                    case EMaterialVertexFactory.MVF_DrawBuffer:
                        techMult = 1;
                        if (mt.CanHaveTangentUpdate)
                        {
                            techMult *= 2;
                        }
                        break;

                    // Regular + Discard + Preskinned                    
                    case EMaterialVertexFactory.MVF_MeshStatic:
                    case EMaterialVertexFactory.MVF_MeshSkinnedVehicle:
                    case EMaterialVertexFactory.MVF_MeshStaticVehicle:
                    case EMaterialVertexFactory.MVF_MeshDestructible:
                    case EMaterialVertexFactory.MVF_MeshDestructibleSkinned:
                    case EMaterialVertexFactory.MVF_MeshWindowProxy:
                        techMult = 2;
                        if (mt.CanHaveTangentUpdate)
                        {
                            techMult *= 2;
                        }                        
                        break;

                    // Regular + Discard + Preskinned + Dismember
                    case EMaterialVertexFactory.MVF_MeshSkinned:
                    case EMaterialVertexFactory.MVF_MeshExtSkinned:
                    case EMaterialVertexFactory.MVF_GarmentMeshSkinned:
                    case EMaterialVertexFactory.MVF_GarmentMeshExtSkinned:
                    case EMaterialVertexFactory.MVF_MeshSkinnedLightBlockers:
                    case EMaterialVertexFactory.MVF_MeshExtSkinnedLightBlockers:
                    case EMaterialVertexFactory.MVF_GarmentMeshSkinnedLightBlockers:
                    case EMaterialVertexFactory.MVF_GarmentMeshExtSkinnedLightBlockers:
                    case EMaterialVertexFactory.MVF_MeshSkinnedSingleBone:
                        techMult = 2;
                        if (mt.CanHaveTangentUpdate)
                        {
                            techMult *= 2;
                        }
                        if (mt.CanHaveDismemberment)
                        {
                            techMult *= 2;
                        }
                        break;
                }

                _log.Info($"{vf} = {techMult}x");

                techTotal += techCount * techMult;
            }

        }
    }

    private static string GetExportExtension(ExportFormats? format)
    {
        return format switch
        {
            ExportFormats.Raw_DXIL => "dxil",
            ExportFormats.Dis_DXIL => "ll",
            ExportFormats.Raw_SPIRV => "spirv",
            ExportFormats.Dec_HLSL => "hlsl",
            _ => "dat",
        };
    }

    private static string TransformFilenameTemplate(string template, MaterialTechniqueViewModel vm, bool isPixelShader)
    {
        template = template.Replace("{SortID}", $"{vm.CompositeSort:X8}");
        template = template.Replace("{Material}", vm.MatName);
        template = template.Replace("{Index}", $"{vm.Index}");
        template = template.Replace("{PassIndex}", $"{vm.PassIndex}");
        template = template.Replace("{Pass}", vm.Pass);
        template = template.Replace("{VF}", vm.VertexFactory.ToString().Replace("MVF_", ""));

        var flagstr = new StringBuilder();
        if (vm.IsDismembered) { flagstr.Append("_DM"); }
        if (vm.IsPreskinned)  { flagstr.Append("_PS"); }
        if (vm.IsDiscarded)   { flagstr.Append("_DC"); }
        template = template.Replace("{Flags}", flagstr.ToString());

        template = template.Replace("{Type}", isPixelShader ? "PS" : "VS");

        return template;
    }

    private static bool IsShaderType(ShaderTypes flags, ShaderTypes value) => (flags & value) == value;

    public /*async Task*/ void SaveSelectTechniques(ExportShaderTechniquesDialogViewModel export)
    {
        var selected = SelectedTechniques.NotNull().OfType<MaterialTechniqueViewModel>().ToList();
        var cache = _shaderCacheService?.Cache as MaterialCache;

        // sanity checks
        if (_shaderCacheService == null || cache == null)
        { return; }
        if (selected.Count == 0 || export.ExportFormat == null || export.ShaderType == null)
        { return; }
        if (string.IsNullOrEmpty(export.FilenameTemplate) || string.IsNullOrEmpty(export.Folder))
        { return; }

        // Ensure folder exists (this 
        var folderInfo = Directory.CreateDirectory(export.Folder);
        if (folderInfo == null)
        {
            _log.Error($"Somehow failed to get directory info for path '{export.Folder}'");
            return;
        }

        _log.Info($"Saving {selected.Count} techs");

        var extension = GetExportExtension(export.ExportFormat);

        foreach (var technique in selected)
        {
            for (var shaderType = ShaderTypes.Vertex; shaderType < ShaderTypes.Both; shaderType++ )
            {
                if (IsShaderType(export.ShaderType.Value, shaderType))
                {
                    var isVS = shaderType == ShaderTypes.Vertex;

                    var filename = TransformFilenameTemplate(export.FilenameTemplate, technique, isVS);
                    var filepath = Path.Combine(folderInfo.FullName, $"{filename}.{extension}");

                    var shaderHash = isVS ? technique.VSHash : technique.PSHash;
                    if (shaderHash == null)
                    {
                        _log.Warning($"No defined {shaderType} for technique '{filename}'");
                        continue;
                    }

                    var shader = cache.Shaders.Get(shaderHash.Value);
                    if (shader == null)
                    {
                        _log.Error($"Cannot retrieve shader with hash {shaderHash:X16}'");
                        continue;
                    }

                    var bytecode = _shaderCacheService.GetShaderBytecode(shader);

                    switch (export.ExportFormat)
                    {
                        case ExportFormats.Raw_DXIL:
                            File.WriteAllBytes(filepath, bytecode);
                            break;
                        case ExportFormats.Dis_DXIL:
                            var err = DXILDecompiler.ExportDisassembled(bytecode, (uint)bytecode.Length, filepath);
                            if (err != DXILDecompiler.SUCCESS)
                            {
                                _log.Error($"Error: {DXILDecompiler.GetErrorString(err)}");
                            }
                            break;
                        default:
                            _log.Error($"Unsupported format {export.ExportFormat} for technique '{filename}'");
                            break;
                    }
                }
            }
        }
    }
}
