using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.RED4.IO;
using WolvenKit.RED4.ShaderCache;
using WolvenKit.RED4.ShaderCache.Common;

namespace WolvenKit.Common.Services
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


        public ShaderCacheService() { } 

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
                else
                {
                    // Static shader reader


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
