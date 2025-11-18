using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WolvenKit.App.Services;
using WolvenKit.Common.Services;
using WolvenKit.Core.Interfaces;
using WolvenKit.RED4.ShaderCache.Common;
using WolvenKit.RED4.ShaderCache.Dynamic;
using static WolvenKit.Common.Services.ShaderCacheService;

namespace WolvenKit.App.ViewModels.Tools;

public class CacheMaterialKey
{
    public string Name { get; set; }
    public ulong Hash { get; set; }

    public CacheMaterialKey(string name, ulong hash) {  Name = name; Hash = hash; }
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
    [ObservableProperty] private CacheMaterialKey? _selectedMaterial = null;

    private readonly ISettingsManager _settingsManager;
    private readonly IShaderCacheService _shaderCacheService;
    private readonly ILoggerService _log;

    public ShaderCacheViewModel(
        ISettingsManager settingsManager,
        IShaderCacheService shaderCacheService,
        ILoggerService loggerService
        ) : base(ToolTitle, ToolContentId)
    {
        _settingsManager = settingsManager;
        _shaderCacheService = shaderCacheService;
        _log = loggerService;

        _log.Warning("ShaderCacheViewModel ctor");

        
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

        if (e is not OnLoadArgs loadArgs)
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
                
        }
        
    }

    partial void OnSelectedMaterialChanged(CacheMaterialKey? value)
    {
        if (value != null)
        {
            _log.Info($"Selected Material = {value.Name}");
        }
    }
}
