using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolvenKit.RED4.ShaderCache;
public interface IShader
{
    ulong Hash { get; }
    uint Size { get; }
    long Address { get; }
}
