using System.Collections.Generic;
using WolvenKit.RED4.Types;

namespace WolvenKit.RED4.ShaderCache.Dynamic;
public class Material
{
    public string Name { get; set; }
    public List<MaterialTechnique> Techniques { get; } = [];
    public HashSet<Enums.EMaterialVertexFactory> VertexFactories { get; } = [];
    public HashSet<string> Passes { get; } = [];

    public Material(string name)
    {
        Name = name;
    }
    public void AddTechnique(MaterialTechnique tech)
    {
        Techniques.Add(tech);
        VertexFactories.Add(tech.Desc.VertexFactory);
        Passes.Add(tech.Desc.Pass);
    }
}
