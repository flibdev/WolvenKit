using System.ComponentModel.DataAnnotations;

namespace WolvenKit.RED4.ShaderCache.Common;

public class MetadataChunk
{
    public string? Type { get; set; }
    public uint Count { get; set; }
    public ulong Size { get; set; }
}

public class CacheMetadata
{
    public long FileSize { get; set; }

    public List<MetadataChunk> Chunks { get; set; } = [];
}
