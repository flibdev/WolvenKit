using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WolvenKit.RED4.Types;

namespace WolvenKit.RED4.ShaderCache.Static;
public class RenderTargetSetup
{
    public List<Enums.GpuWrapApieTextureFormat> RTFormats = [];
    public Enums.GpuWrapApieTextureFormat DSFormat = Enums.GpuWrapApieTextureFormat.SomeMagicBullshit;
}
