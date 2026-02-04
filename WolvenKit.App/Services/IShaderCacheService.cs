using System;
using System.Threading.Tasks;
using WolvenKit.Core.Interfaces;
using WolvenKit.RED4.ShaderCache;
using WolvenKit.RED4.ShaderCache.Common;

namespace WolvenKit.App.Services;

public interface IShaderCacheService : IDisposable
{
    event EventHandler? OnLoad;

    bool IsLoaded { get; }

    Task LoadCache(string path);

    ICache? Cache { get; }

    byte[] GetShaderBytecode(IShader shader);

    public IGameFile? GetMaterialByName(string name);
}
