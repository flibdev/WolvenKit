using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WolvenKit.Common.FNV1A;
using WolvenKit.Common.Services;
using WolvenKit.Core.Compression;
using WolvenKit.Core.CRC;
using WolvenKit.Core.Extensions;
using WolvenKit.Modkit.RED4;
using WolvenKit.Modkit.RED4.Sounds;
using WolvenKit.RED4.Archive;
using WolvenKit.RED4.Archive.IO;
using WolvenKit.RED4.CR2W;
using WolvenKit.RED4.TweakDB;
using WolvenKit.RED4.TweakDB.Helper;
using WolvenKit.RED4.Types;
using EFileReadErrorCodes = WolvenKit.RED4.Archive.IO.EFileReadErrorCodes;

namespace WolvenKit.Utility
{
    [TestClass]
    public class DumpInfo : GameUnitTest
    {
        [ClassInitialize]
        public static void SetupClass(TestContext context) => Setup(context);

        [TestMethod]
        public void DumpSoundEvents()
        {
            var parser = _host.Services.GetRequiredService<Red4ParserService>();
            var eventsInfo = s_groupedFiles[".json"].FirstOrDefault(x => x.Name.Contains("eventsmetadata.json"));
            if (eventsInfo != null)
            {
                var hash = eventsInfo.Key;
                var archive = eventsInfo.GetArchive<Archive>();

                using var originalMemoryStream = new MemoryStream();
                ModTools.ExtractSingleToStream(archive, hash, originalMemoryStream);
                if (!parser.TryReadRed4File(originalMemoryStream, out var originalFile))
                {
                    Assert.Fail("failed to find the file");
                    return;
                }
                if (originalFile.RootChunk is not JsonResource jsonRes)
                {
                    Assert.Fail("not a JsonResource");
                    return;
                }
                if (jsonRes.Root.GetValue() is not audioAudioEventArray eventArray)
                {
                    Assert.Fail("not an audioAudioEventArray");
                    return;
                }

                var md = new SoundEventMetadata();
                var events = eventArray.Events;
                foreach (var e in events)
                {
                    ArgumentNullException.ThrowIfNull(e);
                    var item = new SoundEvent()
                    {
                        Name = e.RedId.ToString(),
                        Tags = e.Tags.Select(x => x.ToString().NotNull()).ToList(),
                    };
                    md.Events.Add(item);
                }

                var resultDir = Path.Combine(Environment.CurrentDirectory, s_testResultsDirectory);
                Directory.CreateDirectory(resultDir);
                var path = Path.Combine(resultDir, "soundEvents.json");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var jsonString = JsonSerializer.Serialize(md, options);
                File.WriteAllText(path, jsonString);

                // test
                var _metadata = JsonSerializer.Deserialize<SoundEventMetadata>(File.ReadAllText(path), options);
            }
        }

        [TestMethod]
        public void DumpExtensionInfo()
        {
            var parser = _host.Services.GetRequiredService<Red4ParserService>();

            var results = new Dictionary<string, ConcurrentDictionary<string, ulong>>();
            foreach (var e in s_groupedFiles)
            {
                results.Add(e.Key, new ConcurrentDictionary<string, ulong>());
            }

            var excludedExtensions = new List<string>() { ".wem", ".bin", ".bnk", ".opuspak", ".opusinfo", ".bk2", ".dat" };

            // Run Test
            foreach (var item in s_groupedFiles.Where(_ => !excludedExtensions.Contains(_.Key)))
            {
                var ext = item.Key;
                var files = item.Value;

                Parallel.ForEach(files, file =>
                {
                    var hash = file.Key;
                    var archive = file.GetArchive<Archive>();

                    try
                    {
                        using var originalMemoryStream = new MemoryStream();
                        ModTools.ExtractSingleToStream(archive, hash, originalMemoryStream);
                        if (parser.TryReadRed4FileHeaders(originalMemoryStream, out var originalFile))
                        {
                            results[ext].TryAdd(originalFile.StringDict[1], 0);
                        }
                        else
                        {

                        }
                    }
                    catch (Exception)
                    {

                    }

                });

            }

            var resultDir = Path.Combine(Environment.CurrentDirectory, s_testResultsDirectory);
            Directory.CreateDirectory(resultDir);

            using var sw = new StringWriter();
            foreach (var (key, classes) in results)
            {
                sw.WriteLine($"{key}: {string.Join(",", classes.Keys.ToList())}");
            }
            File.WriteAllText(Path.Combine(resultDir, "classes.txt"), sw.ToString());
        }

        [TestMethod]
        public void DumpMissingHashes()
        {
            ArgumentNullException.ThrowIfNull(s_bm);

            var parsers = _host.Services.GetRequiredService<Red4ParserService>();
            var _hashService = _host.Services.GetRequiredService<IHashService>();

            var resultDir = Path.Combine(Environment.CurrentDirectory, s_testResultsDirectory);
            Directory.CreateDirectory(resultDir);

            // load new hashes
            var newHashesPath = Path.Combine(s_testResultsDirectory, "resourcePaths.txt");
            var newHashes = new Dictionary<ulong, string>();
            if (File.Exists(newHashesPath))
            {
                var lines = File.ReadAllLines(newHashesPath);
                foreach (var line in lines)
                {
                    var hash = FNV1A64HashAlgorithm.HashString(line);
                    if (!_hashService.Contains(hash))
                    {
                        if (!newHashes.ContainsKey(hash))
                        {
                            newHashes.Add(hash, line);
                        }
                    }
                }
                Console.WriteLine($"Loaded {newHashes.Count} new hashes from {newHashesPath}");
            }

            {
                List<ulong> newAdded = new();
                List<ulong> used = new();
                List<ulong> missing = new();
                var info_missing = new Dictionary<string, List<ulong>>();

                var archives = s_bm.Archives.KeyValues.Select(_ => _.Value).ToList();

                for (var i = 0; i < archives.Count; i++)
                {
                    var ar = archives[i];
                    var hashes = new List<ulong>();
                    info_missing.Add(ar.Name, new List<ulong>());

                    foreach (var (hash, fileInfoEntry) in ar.Files)
                    {
                        if (hash == 6507781686736237035)
                        {

                        }

                        hashes.Add(hash);
                        if (fileInfoEntry is FileEntry fe && fe.NameOrHash == hash.ToString())
                        {
                            if (newHashes.ContainsKey(hash))
                            {
                                used.Add(hash);
                                newAdded.Add(hash);
                            }
                            else
                            {
                                missing.Add(hash);
                                info_missing[ar.Name].Add(hash);
                            }
                        }
                        else
                        {
                            used.Add(hash);
                        }
                    }

                    using var tw = File.CreateText(Path.Combine(resultDir, $"{ar.Name}_hashes.txt"));
                    foreach (var h in hashes)
                    {
                        tw.WriteLine(h);
                    }
                }

                Console.WriteLine($"Added {newAdded.Count} new hashes");

                // write all used and all missing hashes
                var info = Path.Combine(resultDir, $"_info_hashes.txt");
                var info_json = JsonSerializer.Serialize(info_missing, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                });
                File.WriteAllText(info, info_json);

                var missinghashtxt = Path.Combine(resultDir, "missinghashes.txt");
                missing = missing.OrderBy(x => x).Distinct().ToList();
                using (var missingWriter = File.CreateText(missinghashtxt))
                {
                    for (var i = 0; i < missing.Count; i++)
                    {
                        var mh = missing[i];
                        missingWriter.WriteLine(mh);
                    }
                }

                var usedhashtxt = Path.Combine(resultDir, "usedhashes.txt");
                var usedStrings = new List<string>();
                foreach (var hash in used)
                {
                    var str = "";
                    if (_hashService.Contains(hash))
                    {
                        str = _hashService.Get(hash);
                    }
                    else if (newHashes.ContainsKey(hash))
                    {
                        str = newHashes[hash];
                    }
                    else
                    {
                        continue;
                    }
                    if (!string.IsNullOrEmpty(str))
                    {
                        usedStrings.Add(str);
                    }

                }
                usedStrings = usedStrings.OrderBy(x => x).Distinct().ToList();
                File.WriteAllLines(usedhashtxt, usedStrings);

                //var allhashes = _hashService.GetAllHashes().ToList();
                //var unused = allhashes.Except(used).ToList();

                //var unusedhashtxt = Path.Combine(resultDir, "unusedhashes.txt");
                //var unusedStrings = unused.Select(s => _hashService.Get(s)).OrderBy(x => x);
                //using (var unusedWriter = File.CreateText(usedhashtxt))
                //{
                //    foreach (var s in unusedStrings)
                //    {
                //        unusedWriter.WriteLine(s);
                //    }
                //}

                //compress all used and all missing hashes
                //CompressFile(unusedhashtxt, resultDir);
                CompressFile(usedhashtxt, resultDir);

            }


        }

        private void CompressFile(string path, string resultDir)
        {
            var inbuffer = File.ReadAllBytes(path);
            IEnumerable<byte> outBuffer = new List<byte>();

            var r = Oodle.Compress(inbuffer, ref outBuffer, true);

            var filename = $"{Path.GetFileNameWithoutExtension(path)}.kark";
            File.WriteAllBytes(Path.Combine(resultDir, filename), outBuffer.ToArray());
        }

        public enum Merge
        {
            All,
            Facts,
            NodeRefs
        }

        [TestMethod]
        public void MergeAllStrings() => MergeStrings(Merge.All);

        [TestMethod]
        public void MergeFactStrings() => MergeStrings(Merge.Facts);

        [TestMethod]
        public void MergeNodeRefStrings() => MergeStrings(Merge.NodeRefs);

        private void MergeStrings(Merge merge)
        {
            ArgumentNullException.ThrowIfNull(s_bm);

            var hashService = _host.Services.GetRequiredService<IHashService>();
            ArgumentNullException.ThrowIfNull(hashService);

            var resultDir = Path.Combine(Environment.CurrentDirectory, s_testResultsDirectory, "infodump");

            var dict = new ConcurrentDictionary<string, List<string>>();
            var dict2 = new ConcurrentDictionary<string, byte>();

            Parallel.ForEach(Directory.EnumerateFiles(resultDir, "*.json", SearchOption.AllDirectories), filePath =>
            {
                var dc = JsonSerializer.Deserialize<DataCollection>(File.ReadAllText(filePath))!;

                var lst = new HashSet<string>();

                if (merge == Merge.Facts)
                {
                    var ext = ERedExtensionHelper.FromString(dc.FileName);
                    if (ext == ERedExtension.unknown)
                    {
                        ext = ERedExtensionHelper.FromString(hashService.GetGuessedExtension(dc.Hash));
                    }

                    if (ext is ERedExtension.questphase or ERedExtension.scene)
                    {
                        DumpFileInfo(dc, lst);
                    }

                    if (lst.Count > 0)
                    {
                        var orderedLst = new List<string>(lst);
                        orderedLst.Sort();

                        dict.TryAdd(dc.FileName, orderedLst);
                    }
                }
                else
                {
                    DumpFileInfo(dc, lst);
                    foreach (var str in lst)
                    {
                        if (str == null)
                        {
                            continue;
                        }

                        dict2.TryAdd(str, 0);
                    }
                }


            });

            if (merge == Merge.Facts)
            {
                var nDict = dict.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                File.WriteAllText(Path.Join(resultDir, "facts.json"), JsonSerializer.Serialize(nDict, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                var nList = dict2.Keys.ToList();
                nList.Sort();

                File.WriteAllLines(Path.Join(resultDir, $"merged_{merge}.txt"), nList);
            }

            void DumpFileInfo(DataCollection dc, HashSet<string> lst)
            {
                if (merge == Merge.All)
                {
                    if (dc.UnusedStrings != null)
                    {
                        foreach (var str in dc.UnusedStrings)
                        {
                            lst.Add(str);
                        }
                    }

                    if (dc.Imports != null)
                    {
                        foreach (var str in dc.Imports)
                        {
                            lst.Add(str);
                        }
                    }

                    if (dc.UsedStrings != null)
                    {
                        foreach (var str in dc.UsedStrings)
                        {
                            lst.Add(str);
                        }
                    }

                    if (dc.TweakStrings != null)
                    {
                        foreach (var str in dc.TweakStrings)
                        {
                            lst.Add(str);
                        }
                    }

                    if (dc.NodeRefs != null)
                    {
                        foreach (var str in dc.NodeRefs)
                        {
                            lst.Add(str);
                        }
                    }
                }

                if (merge == Merge.All || merge == Merge.Facts)
                {
                    if (dc.FactStrings != null)
                    {
                        foreach (var str in dc.FactStrings)
                        {
                            lst.Add(str);
                        }
                    }
                }

                if (merge == Merge.All || merge == Merge.NodeRefs)
                {
                    if (dc.NodeRefs != null)
                    {
                        foreach (var str in dc.NodeRefs)
                        {
                            lst.Add(str);
                        }
                    }
                }

                if (dc.Buffers != null)
                {
                    foreach (var buffer in dc.Buffers)
                    {
                        DumpFileInfo(buffer, lst);
                    }
                }
            }
        }

        [TestMethod]
        public void DumpStrings()
        {
            ArgumentNullException.ThrowIfNull(s_bm);
            var resultDir = Path.Combine(Environment.CurrentDirectory, s_testResultsDirectory, "infodump");
            Directory.CreateDirectory(resultDir);

            var existingFiles = new ConcurrentDictionary<string, byte>();
            foreach (var file in Directory.GetFiles(resultDir, "*.json", SearchOption.AllDirectories))
            {
                existingFiles.TryAdd(file, 0);
            }

            var failedFiles = new ConcurrentDictionary<string, byte>();

            var archives = s_bm.Archives.KeyValues.Select(_ => _.Value).ToList();
            foreach (var gameArchive in archives)
            {
                if (gameArchive is not Archive archive)
                {
                    continue;
                }

                string? dirPath = null;
                if (archive.ArchiveAbsolutePath.Contains(@"archive\pc\content"))
                {
                    dirPath = Path.Combine(resultDir, "content");
                }

                if (archive.ArchiveAbsolutePath.Contains(@"archive\pc\ep1"))
                {
                    dirPath = Path.Combine(resultDir, "ep1");
                }

                if (string.IsNullOrEmpty(dirPath))
                {
                    throw new Exception();
                }

                dirPath = Path.Combine(dirPath, archive.Name);
                Directory.CreateDirectory(dirPath);

                Parallel.ForEach(archive.Files, pair =>
                {
                    if (pair.Value is not FileEntry fileEntry)
                    {
                        return;
                    }

                    var resultPath = Path.Combine(dirPath, fileEntry.NameHash64 + ".json");
                    if (existingFiles.TryRemove(resultPath, out _))
                    {
                        return;
                    }

                    try
                    {
                        using var ms = new MemoryStream();
                        archive.ExtractFile(fileEntry, ms);
                        ms.Seek(0, SeekOrigin.Begin);

                        using var reader = new CR2WReader(ms);
                        reader.ParsingError += args => true;

                        reader.CollectData = true;

                        if (reader.ReadFile(out _) == EFileReadErrorCodes.NoError)
                        {
                            reader.DataCollection.CleanUp();

                            reader.DataCollection.FileName = fileEntry.NameOrHash;
                            reader.DataCollection.Hash = fileEntry.NameHash64;

                            var json = JsonSerializer.Serialize(reader.DataCollection, new JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(resultPath, json);
                        }
                    }
                    catch (Exception)
                    {
                        failedFiles.TryAdd(fileEntry.NameOrHash, 0);
                        // ignore
                    }
                });

                archive.ReleaseFileHandle();
            }

            var lst = failedFiles.Keys.ToList();
            lst.Sort();

            File.WriteAllLines(Path.Join(resultDir, "failed.txt"), lst);
        }

        [TestMethod]
        public void RecordGenerator()
        {
            ArgumentNullException.ThrowIfNull(s_tweakDbPath);

            var tweakDbStrPath = Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "..", "..", "WolvenKit.Common", "Resources", "tweakdbstr.kark");
            if (!File.Exists(tweakDbStrPath))
            {
                throw new ArgumentNullException(nameof(tweakDbStrPath));
            }

            var resultDir = Path.Combine(Environment.CurrentDirectory, s_testResultsDirectory, "records");
            Directory.CreateDirectory(resultDir);

            var stringHelper = new TweakDBStringHelper();
            stringHelper.Load(tweakDbStrPath);

            using var fh = File.OpenRead(s_tweakDbPath);
            using var reader2 = new TweakDBReader(fh);

            if (reader2.ReadFile(out var tweakDb) != WolvenKit.RED4.TweakDB.EFileReadErrorCodes.NoError)
            {
                return;
            }
            ArgumentNullException.ThrowIfNull(tweakDb);
            var ass = typeof(gamedataTweakDBRecord).Assembly;

            var rts = new Dictionary<string, Dictionary<string, string>>();
            foreach (var (id, value) in tweakDb.Flats)
            {
                if (stringHelper.GetString(id) is { } resolvedText)
                {
                    if (resolvedText.StartsWith("RTDB"))
                    {
                        var parts = resolvedText.Split('.');

                        var className = $"gamedata{parts[1]}_Record";
                        if (!rts.ContainsKey(className))
                        {
                            rts.Add(className, new Dictionary<string, string>());
                        }

                        var typeName = GetTypeName(value.GetType());

                        rts[className].Add(parts[2], typeName);
                    }
                }
            }

            foreach (var (className, properties) in rts)
            {
                var classType = ass.GetType($"WolvenKit.RED4.Types.{className}");
                if (classType == null)
                {
                    throw new NotImplementedException();
                }

                var baseType = classType.BaseType;
                while (baseType != null && baseType != typeof(gamedataTweakDBRecord))
                {
                    if (rts.ContainsKey(baseType.Name))
                    {
                        foreach (var key in rts[baseType.Name].Keys)
                        {
                            properties.Remove(key);
                        }
                    }

                    baseType = baseType.BaseType;
                }
            }

            foreach (var (className, props) in rts)
            {
                if (props.Count == 0)
                {
                    continue;
                }

                var sortedProps = props.OrderBy(obj => obj.Key).ToDictionary(obj => obj.Key, obj => obj.Value);

                var lines = new List<string>
                {
                    "",
                    "namespace WolvenKit.RED4.Types",
                    "{",
                    $"\tpublic partial class {className}",
                    "\t{"
                };

                var firstProp = true;
                foreach (var (propName, typeName) in sortedProps)
                {
                    if (!firstProp)
                    {
                        lines.Add("\t\t");
                    }

                    var nName = char.ToUpper(propName[0]) + propName[1..];

                    lines.Add($"\t\t[RED(\"{propName}\")]");
                    lines.Add("\t\t[REDProperty(IsIgnored = true)]");
                    lines.Add($"\t\tpublic {typeName} {nName}");
                    lines.Add("\t\t{");
                    lines.Add($"\t\t\tget => GetPropertyValue<{typeName}>();");
                    lines.Add($"\t\t\tset => SetPropertyValue<{typeName}>(value);");
                    lines.Add("\t\t}");

                    firstProp = false;
                }

                lines.Add("\t}");
                lines.Add("}");

                File.WriteAllLines(Path.Combine(resultDir, $"{className}.cs"), lines);
            }

            static string GetTypeName(Type type)
            {
                if (type.IsGenericType)
                {
                    if (type.GetGenericTypeDefinition() == typeof(CArray<>))
                    {
                        return $"CArray<{GetTypeName(type.GetGenericArguments()[0])}>";
                    }

                    if (type.GetGenericTypeDefinition() == typeof(CResourceReference<>))
                    {
                        return $"CResourceReference<{GetTypeName(type.GetGenericArguments()[0])}>";
                    }

                    if (type.GetGenericTypeDefinition() == typeof(CResourceAsyncReference<>))
                    {
                        return $"CResourceAsyncReference<{GetTypeName(type.GetGenericArguments()[0])}>";
                    }
                }
                else
                {
                    return type.Name;
                }

                throw new NotSupportedException();
            }
        }

        public record class TextureInfo(string group, string compression, string rawFormat);

        [TestMethod]
        public void DumpTextureInfo()
        {
            var parser = _host.Services.GetRequiredService<Red4ParserService>();

            var results = new Dictionary<string, ConcurrentDictionary<ulong, TextureInfo>>();
            //foreach (var e in s_groupedFiles)
            {
                results.Add(".xbm", new ConcurrentDictionary<ulong, TextureInfo>());
            }

            // Run Test
            foreach (var item in s_groupedFiles.Where(_ => _.Key == ".xbm"))
            {
                var ext = item.Key;
                var files = item.Value;

                Parallel.ForEach(files, file =>
                {
                    var hash = file.Key;
                    var archive = file.GetArchive<Archive>();

                    try
                    {
                        using var ms = new MemoryStream();
                        ModTools.ExtractSingleToStream(archive, hash, ms);
                        if (parser.TryReadRed4File(ms, out var resource))
                        {
                            if (resource.RootChunk is CBitmapTexture texture)
                            {
                                var group = texture.Setup.Group.ToString();
                                var compression = texture.Setup.Compression.ToString();
                                var rawFormat = texture.Setup.RawFormat.ToString();


                                var info = new TextureInfo(group, compression, rawFormat);
                                results[".xbm"].TryAdd(hash, info);
                            }
                        }
                        else
                        {

                        }
                    }
                    catch (Exception)
                    {

                    }

                });

            }

            var _hashService = _host.Services.GetRequiredService<IHashService>();
            var resultDir = Path.Combine(Environment.CurrentDirectory, s_testResultsDirectory);
            Directory.CreateDirectory(resultDir);

            using var sw = new StringWriter();
            sw.WriteLine($"filename; group; rawFormat; compression");
            foreach (var (key, info) in results[".xbm"])
            {
                var filename = _hashService.Get(key);
                sw.WriteLine($"{filename}; {info.group}; {info.rawFormat}; {info.compression}");
            }
            File.WriteAllText(Path.Combine(resultDir, "texinfo.csv"), sw.ToString());
        }
    }
}