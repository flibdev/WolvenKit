using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WolvenKit.RED4.ShaderCache;
public interface ICache
{
    enum Type
    {
        Static,
        Dynamic
    }

    Type GetType { get; }
}
