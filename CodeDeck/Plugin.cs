using CodeDeck.Models;
using CodeDeck.Models.Configuration;
using CodeDeck.PluginAbstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CodeDeck
{
    public class Plugin : AssemblyLoadContext
    {
        private readonly ILogger _logger;
        private readonly PluginConfiguration? _configuration;

        /// <summary>
        /// The location of the plugin directory on disk.
        /// </summary>
        public string PluginPath { get; set; }

        /// <summary>
        /// The name of the plugin. The plugin name is the same as the plugin directory name.
        /// </summary>
        new public string Name { get; set; }

        public string BuildPath => Path.Combine(PluginPath, "bin");
        public string LibrariesPath => Path.Combine(PluginPath, "lib");
        //public string NugetPath => Path.Combine(PluginPath, "nuget");
        public string AssemblyFileName => Path.Combine(BuildPath, $"{Name}.dll");
        public string PdbFileName => Path.Combine(BuildPath, $"{Name}.pdb");

        public List<FileInfo>? SourceFiles { get; set; }

        public FileInfo? AssemblyFileInfo => File.Exists(AssemblyFileName) ? new FileInfo(AssemblyFileName) : null;

        public Assembly? Assembly { get; set; }

        public Type? PluginType { get; set; }


        public Plugin(ILogger logger, string pluginDirectory, PluginConfiguration? configuration)
        {
            _logger = logger;
            _configuration = configuration;

            PluginPath = pluginDirectory;
            Name = Path.GetFileName(pluginDirectory);

            Init();
        }

        public void Init()
        {
            // Create default directories
            Directory.CreateDirectory(BuildPath);

            SourceFiles = GetAllSourceFiles();

            // Recompile plugin if a compiled assembly doesn't exist or
            // if a source files has been changed since last compilation
            if (AssemblyFileInfo == null || !IsAssemblyUpToDate())
            {
                _logger.LogInformation($"<{nameof(Plugin)}.{nameof(Init)}> Compiling plugin: '{Name}'");
                if (!CompilePlugin())
                {
                    _logger.LogError($"<{nameof(Plugin)}.{nameof(Init)}> Encountered errors while compiling plugin: '{Name}'");
                    return;
                }
            }

            _logger.LogInformation($"<{nameof(Plugin)}.{nameof(Init)}> Loading plugin: '{AssemblyFileName}'");

            // Load the plugin and libraries used by the plugin into the AssemblyLoadContext
            LoadAllLibraries();
            Assembly = LoadFromAssemblyPath(AssemblyFileName);

            // Get the plugin type inside the loaded assembly
            PluginType = GetPluginType();
            if (PluginType == null)
            {
                _logger.LogWarning($"<{nameof(Plugin)}.{nameof(Init)}> Assembly; '{Name}' does not contain a valid plugin!");
                return;
            }

            // Map Plugin settings if available in the configuration
            if (_configuration?.Settings != null)
            {
                MapSettings(_configuration.Settings, PluginType);
            }
        }

        public void LoadAllLibraries()
        {
            if (!Directory.Exists(LibrariesPath)) return;

            var assemblies = Directory.GetFiles(LibrariesPath, "*.dll", SearchOption.AllDirectories);
            foreach (var assembly in assemblies)
            {
                try
                {
                    LoadFromAssemblyPath(Path.GetFullPath(assembly));
                }
                catch (Exception e)
                {
                    _logger.LogWarning($"<{nameof(Plugin)}.{nameof(LoadAllLibraries)}> Error while loading library: '{Path.GetFullPath(assembly)}'. Exception: {e.Message}");
                }
            }
        }

        public List<FileInfo> GetAllSourceFiles()
        {
            var sourceFiles = Directory.GetFiles(PluginPath, "*.cs", SearchOption.AllDirectories);
            var sourceFilesInfo = sourceFiles.Select(x => new FileInfo(x));

            return sourceFilesInfo.ToList();
        }

        public bool IsAssemblyUpToDate()
        {
            if (AssemblyFileInfo is null || SourceFiles is null) return false;

            var assemblyWriteTime = AssemblyFileInfo.LastWriteTimeUtc;
            var latestSourceFileWriteTime = SourceFiles
                .OrderByDescending(x => x.LastWriteTimeUtc)
                .First().LastWriteTimeUtc;

            return assemblyWriteTime > latestSourceFileWriteTime;
        }

        public bool CompilePlugin()
        {
            if (SourceFiles is null || !SourceFiles.Any()) return false;

            // Read all source files and parse into syntax trees
            var syntaxTrees = new List<SyntaxTree>();
            foreach (var sf in SourceFiles)
            {
                var data = File.ReadAllBytes(sf.FullName);
                var sourceText = SourceText.From(data, data.Length, Encoding.UTF8, SourceHashAlgorithm.Sha1, false, true);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, null, sf.FullName);

                syntaxTrees.Add(syntaxTree);
            }

            // Get trusted assemblies
            var trustedAssembliesPaths = ((string)(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? ""))
                .Split(Path.PathSeparator).ToList();

            // Get assemblies in the libraries directory
            if (Directory.Exists(LibrariesPath))
            {
                trustedAssembliesPaths.AddRange(Directory.GetFiles(LibrariesPath, "*.dll", SearchOption.AllDirectories));
            }

            // Convert all assembly paths into references
            var references = trustedAssembliesPaths
                .Select(p => MetadataReference.CreateFromFile(p))
                .ToList();

            // Create the compilation
            var compilation = CSharpCompilation.Create(
                Name,
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, platform: Platform.AnyCpu));

            // Try to compile and emit assembly and PDB 
            using var assemblyStream = File.Create(AssemblyFileName);
            using var pdbStream = File.Create(PdbFileName);
            var emitResult = compilation.Emit(assemblyStream, pdbStream,
                options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));

            if (!emitResult.Success)
            {
                // When the compilation fails, it leaves behind zero byte files, remove them
                assemblyStream.Close();
                pdbStream.Close();
                if (File.Exists(AssemblyFileName)) File.Delete(AssemblyFileName);
                if (File.Exists(PdbFileName)) File.Delete(PdbFileName);

                foreach (var d in emitResult.Diagnostics)
                {
                    _logger.LogError($"<{nameof(Plugin)}.{nameof(CompilePlugin)}> Error: {d.Location}: {d.GetMessage()}");
                }
                return false;
            }

            return true;
        }

        private Type? GetPluginType()
        {
            if (Assembly is null) return null;

            var pluginType = Assembly
                .GetTypes()
                .FirstOrDefault(x => x.BaseType != null && x.BaseType.Equals(typeof(CodeDeckPlugin)));

            return pluginType;
        }

        public Tile? CreateTileInstance(string tileTypeName, JsonObject? settings)
        {
            if (PluginType is null)
            {
                return null;
            }

            var tileType = PluginType
                .GetNestedTypes()
                .Where(x => x.BaseType == typeof(Tile))
                .Where(x => x.Name == tileTypeName)
                .FirstOrDefault();

            if (tileType is null)
            {
                return null;
            }

            if (Activator.CreateInstance(tileType) is not Tile tileInstance)
            {
                return null;
            }

            if (settings != null)
            {
                MapSettings(settings, tileType, tileInstance);
            }

            return tileInstance;
        }


        /// <summary>
        /// This method maps a Dictionary of settings to the relevant properties of a class.
        /// The destination object might be a static class or a class instance.
        /// Note to self: This method is not generic because we're working with types loaded at runtime.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="type"></param>
        /// <param name="instance"></param>
        private void MapSettings(JsonObject? settings, Type type, object? instance = null)
        {
            // Assign the raw key settings dictionary to the instance or static class
            type.BaseType?.GetProperty("Settings")?.SetValue(instance, settings);

            // Get all properties with the SettingAttribute
            var settingProperties = type.GetProperties()
                .Where(x => x.CustomAttributes.Any(ca => ca.AttributeType.Name == nameof(SettingAttribute)))
                .ToList();

            // Try to parse the setting into the correct type and assign the value to the property
            foreach (var p in settingProperties)
            {
                if (settings is not null && settings.ContainsKey(p.Name) && p is not null)
                {
                    JsonNode? value = settings[p.Name];
                    // Console.WriteLine($"{p.Name} = {value?.ToJsonString()}");
                    JsonSerializerOptions jsOpts = new();
                    jsOpts.Converters.Add(new ColorParser());
                    jsOpts.Converters.Add(new KeyIdentifierConverter());
                    jsOpts.Converters.Add(new ImageParser());
                    foreach (CustomAttributeData v in p.CustomAttributes)
                    {
                        if (v.AttributeType.Name == nameof(JsonConverterAttribute))
                        {
                            // Kill me, it works
                            Type? jv = v.ConstructorArguments[0].Value as Type;
                            if (jv is not null)
                            {
                                JsonConverter? cv = Activator.CreateInstance(jv) as JsonConverter;
                                if (cv is not null)
                                {
                                    jsOpts.Converters.Add(cv);
                                }
                            }
                        }
                    }

                    object? valObj = JsonSerializer.Deserialize(value, p.PropertyType, jsOpts);
                    if (valObj is not null)
                    {
                        p.SetValue(instance, valObj);
                    }
                }
                // if (settings?.TryGetValue(p.Name, out var value) ?? false)
                // {
                //     // Parse string
                //     if (p.PropertyType.Name == typeof(string).Name)
                //     {
                //         p.SetValue(instance, value);
                //     }
                //     // Parse bool
                //     else if (p.PropertyType == typeof(bool?) || p.PropertyType == typeof(bool))
                //     {
                //         if (bool.TryParse(value, out var parsedValue))
                //         {
                //             p.SetValue(instance, parsedValue);
                //         }
                //     }
                //     // Parse byte
                //     else if (p.PropertyType == typeof(byte?) || p.PropertyType == typeof(byte))
                //     {
                //         if (byte.TryParse(value, out var parsedValue))
                //         {
                //             p.SetValue(instance, parsedValue);
                //         }
                //     }
                //     // Parse int
                //     else if (p.PropertyType == typeof(int?) || p.PropertyType == typeof(int))
                //     {
                //         if (int.TryParse(value, out var parsedValue))
                //         {
                //             p.SetValue(instance, parsedValue);
                //         }
                //     }
                //     else if (p.PropertyType == typeof(Image))
                //     {
                //         if (File.Exists(value))
                //         {
                //             Image parsedValue;
                //             try
                //             {
                //                 parsedValue = Image.Load<Rgba32>(value);
                //                 p.SetValue(instance, parsedValue);
                //             }
                //             finally
                //             {
                //                 _logger.LogError("Error loading image");
                //             }
                //         }
                //     }
                //     else if (p.PropertyType == typeof(Color?) || p.PropertyType == typeof(Color))
                //     {
                //         if (Color.TryParse(value, out var parsedValue))
                //         {
                //             p.SetValue(instance, parsedValue);
                //         }
                //     }
                //     // Parse double
                //     else if (p.PropertyType == typeof(double?) || p.PropertyType == typeof(double))
                //     {
                //         if (double.TryParse(value, out var parsedValue))
                //         {
                //             p.SetValue(instance, parsedValue);
                //         }
                //     }
                //     else
                //     {
                //         _logger.LogWarning($"<{nameof(Plugin)}.{nameof(MapSettings)}> Can not map setting '{p.Name}' because data type '{p.PropertyType.Name}' is not supported.");
                //     }
                // }
            }
        }
    }
}
