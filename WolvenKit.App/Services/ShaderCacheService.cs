using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.Common;
using WolvenKit.Common.FNV1A;
using WolvenKit.Core.Interfaces;
using WolvenKit.RED4.IO;
using WolvenKit.RED4.ShaderCache;
using WolvenKit.RED4.ShaderCache.Common;
using WolvenKit.RED4.Types;

namespace WolvenKit.App.Services
{
    public class ShaderCacheService : IShaderCacheService
    {
        public class OnLoadArgs : EventArgs
        {
            public bool Success { get; set; }
            public string? Reason { get; set; }
            public ICache? Cache { get; set; }
        }

        public event EventHandler? OnLoad;

        public ICache? Cache { get; private set; } = null;
        public bool IsLoaded => !_isLoading && Cache != null;


        private bool _isLoading = false;
        private BinaryReader? _reader = null;


        private readonly IArchiveManager _archiveManager;
        private readonly ILoggerService _loggerService;

        public ShaderCacheService(
            IArchiveManager archiveManager,
            ILoggerService loggerService)
        {
            _archiveManager = archiveManager;
            _loggerService = loggerService;
        }

        private static readonly Dictionary<string, string> s_knownNameMismatches = new()
        {
            // Has duplicate materials
            { "silverhand_overlay", "base\\materials\\silverhand_overlay.mt" },
            // CDPR can't speel
            { "q305_thunderstorm_lighting", "ep1\\fx\\quest\\q305\\thunderstorm\\q305_thunderstorm_lightning.mt" },
            // Or be consistent
            { "water_test", "base\\fx\\shaders\\water_plane.mt" }
        };

        public IGameFile? GetMaterialByName(string name)
        {
            var mat = s_knownNameMismatches.GetValueOrDefault(name);

            mat ??= _archiveManager
                .Search($"\\{name}.mt", ArchiveManagerScope.Basegame)
                .Select(f => f.FileName)
                .Cast<string?>()
                .FirstOrDefault();

            mat ??= _archiveManager
                .Search($"\\{name}.remt", ArchiveManagerScope.Basegame)
                .Select(f => f.FileName)
                .Cast<string?>()
                .FirstOrDefault();

            return mat != null ? _archiveManager.GetGameFile(mat, false, false) : null;
        }

        public async Task LoadCache(string path)
        {
            if (_isLoading)
            {
                return;
            }

            _isLoading = true;

            if (Cache != null)
            {
                _reader?.Close();
                Cache = null;
            }
             
            await Task.Run(() =>
            {
                var file = File.OpenRead(path);

                _reader = new BinaryReader(file, Encoding.UTF8, true);

                ICacheReader? cacheReader = null;

                // Detect format type                
                if (MaterialCacheReader.IsSupportedFile(_reader))
                {
                    cacheReader = new MaterialCacheReader(_reader);
                }
                else if (StaticCacheReader.IsSupportedFile(_reader))
                {
                    cacheReader = new StaticCacheReader(_reader);
                }

                if (cacheReader != null)
                {
                    var args = new OnLoadArgs
                    {
                        Cache = null,
                        Success = false
                    };

                    // cache reader throws on data errors
                    try
                    {
                        Cache = cacheReader.ReadFile();
                        args.Cache = Cache;
                        args.Success = true;
                    }
                    catch (Exception e)
                    {
                        args.Reason = e.ToString();
                    }

                    OnLoad?.Invoke(this, args);
                }
                else
                {
                    OnLoad?.Invoke(this, new OnLoadArgs
                    {
                        Success = false,
                        Reason = "Unsupported file format"
                    });
                }

                _isLoading = false;
            });
        }

        

        public byte[] GetShaderBytecode(IShader shader)
        {
            if (!IsLoaded)
            {
                throw new Exception("ShaderCacheService: Cannot get bytecode before loading");
            }

            if (_reader == null)
            {
                throw new Exception("ShaderCacheService: No cached file reader");
            }

            _reader.BaseStream.Seek(shader.Address, SeekOrigin.Begin);
            return _reader.ReadBytes((int)shader.Size);
        }


        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ShaderCacheService()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _reader?.Close();
                }

                _disposed = true;
            }
        }
        #endregion
    }
}
