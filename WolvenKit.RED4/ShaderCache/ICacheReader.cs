using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.RED4.IO;
using WolvenKit.RED4.ShaderCache.Dynamic;

namespace WolvenKit.RED4.ShaderCache;
public interface ICacheReader
{
    /// <summary>
    /// Determines if a file is supported by matching magic and version number
    /// against their known offsets in the footers.
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    static abstract bool IsSupportedFile(BinaryReader reader);

    /// <summary>
    /// Parses a shader cache file and returns the mapped data.
    /// </summary>
    /// <exception cref="InvalidDataException">Thrown if the file has unexpected data</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the file has hashes that don't map correctly</exception>
    ICache ReadFile();
}
