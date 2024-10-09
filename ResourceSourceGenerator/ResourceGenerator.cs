using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CSharpPoet;
using Microsoft.CodeAnalysis;

namespace ResourceSourceGenerator;

[Generator(LanguageNames.CSharp)]
public class ResourceGenerator : IIncrementalGenerator
{
    private readonly record struct Options(string ProjectDirectory)
    {
        public bool TryGetRelativePath(string path, [NotNullWhen(true)] out string? relativePath)
        {
            if (
                path.NormalizePath().TryTrimStart(ProjectDirectory, out relativePath) &&
                relativePath.TryTrimStart("Resource/", out relativePath)
            )
            {
                return true;
            }

            relativePath = null;
            return false;
        }
    }

    public Dictionary<string, string> Configs = new();
    public List<string> Sprites = [];
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var optionsProvider = context.AnalyzerConfigOptionsProvider
            .Select((analyzerConfigOptions, _) =>
            {
                if (!analyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDirectory) )
                {
                    throw new Exception("Couldn't get project directory");
                }
                return new Options(projectDirectory.NormalizePath());
            });

        var fileProvider = context
            .AdditionalTextsProvider
            .Combine(optionsProvider)
            .Select((pair, cancellationToken) =>
            {
                var (file, options) = pair;

                if (!options.TryGetRelativePath(file.Path, out var relativePath))
                {
                    throw new InvalidOperationException();
                }

                var text = relativePath.EndsWith(".json") ? file.GetText(cancellationToken)!.ToString() : null;
                return (
                    RelativePath: relativePath,
                    Content: text
                );
            });
        
        var configProvider = fileProvider.Where(n => n.RelativePath.EndsWith(".json")).Collect();
        context.RegisterSourceOutput(configProvider, (productionContext, configs) =>
        {
            foreach (var file in configs)
            {
                Configs[file.RelativePath.Split('/').Last()] = file.Content!;
            }
        });
        
        var spriteProvider = fileProvider.Where(n => n.RelativePath.StartsWith("Sprites") && n.RelativePath.EndsWith(".png")).Collect();
        context.RegisterSourceOutput(spriteProvider, (productionContext, sprites) =>
        {
            Dictionary<string, float>? spriteConfig = null;
            if (Configs.TryGetValue("SpriteInfos.json", out var configText))
                spriteConfig = JsonSerializer.Deserialize<Dictionary<string, float>>(configText);
            var spriteFields = new List<CSharpType.IMember>();
            foreach (var file in sprites)
            {
                var filedName = file.RelativePath.GetFileName(false);
                string? pixel = null;
                if (spriteConfig != null && spriteConfig.TryGetValue(file.RelativePath.GetFileName(true), out var pixelValue))
                    pixel = $"{pixelValue}f";

                var spritePath = file.RelativePath.GetResourcePath();
                var value = "new (";
                value += "\"";
                value += spritePath;
                value += "\"";
                if (pixel != null)
                    value += $", {pixel}";
                
                value += ")";
                spriteFields.Add(new CSharpField(Visibility.Public, "ResourceSprite", filedName)
                {
                    IsStatic = true,
                    IsReadonly = true,
                    DefaultValue = value
                });
                Sprites.Add($"{filedName}_{pixel ?? "115f"}");
            }
            
            var sourceClass = new CSharpClass(Visibility.Public, "Sprites")
            {
                IsStatic = true,
                Members = spriteFields
            };
            
            var spriteSourceText = new CSharpFile("NextResources")
            {
                Usings =
                {
                    new CSharpUsing("Next_Chat.Core")
                },
                Members = { sourceClass } 
            }.ToString();
            
            productionContext.AddSource("Sprites", spriteSourceText);
        });
        
        var libProvider = fileProvider.Where(n => n.RelativePath.StartsWith("Lib") && n.RelativePath.EndsWith(".dll")).Collect();
        context.RegisterSourceOutput(libProvider, (productionContext, libs) =>
        {
            Dictionary<string, bool>? libConfig = null;
            if (Configs.TryGetValue("LibConfig.json", out var configText))
                libConfig = JsonSerializer.Deserialize<Dictionary<string, bool>>(configText);
            
            var libsValue = "";
            foreach (var file in libs)
            {
                var write = false;
                if (libConfig != null && libConfig.TryGetValue(file.RelativePath.GetFileName(true), out var _write))
                    write = _write;

                var writeValue = write ? ", true" : ""; 
                libsValue += $"new (\"{file.RelativePath.GetResourcePath()}\"{writeValue})";
                if (libs.Last() != file)
                    libsValue += ",";
                libsValue += "\n";
            }
            var sourceClass = new CSharpClass(Visibility.Public, "Libs")
            {
                IsStatic = true,
                Members =
                {
                    new CSharpField(Visibility.Internal, "ResourceLib[]", "ResourceLibs")
                    {
                        IsStatic = true,
                        IsReadonly = true,
                        DefaultValue = $"[\n{libsValue}]"
                    }
                }
            };
            
            var spriteSourceText = new CSharpFile("NextResources")
            {
                Usings =
                {
                    new CSharpUsing("Next_Chat.Core")
                },
                Members = { sourceClass } 
            }.ToString();
            
            productionContext.AddSource("Libs", spriteSourceText);
        });
        
        context.RegisterSourceOutput(context.CompilationProvider,(Context, Compilation)=>
        {
            var configs = string.Join(",", Configs.Keys);
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sprites = string.Join(",", Sprites);

            var infoSourceText = new CSharpFile("NextResources")
            {
                new CSharpClass("ResourceInfo")
                {
                    new CSharpField(Visibility.Public, "string", "BuildTime")
                    {
                        IsStatic = true,
                        IsReadonly = true,
                        DefaultValue = $"\"{time}\""
                    },
                    new CSharpField(Visibility.Public, "string", "Configs")
                    {
                        IsStatic = true,
                        IsReadonly = true,
                        DefaultValue = $"\"{configs}\""
                    },
                    new CSharpField(Visibility.Public, "string", "Sprites")
                    {
                        IsStatic = true,
                        IsReadonly = true,
                        DefaultValue = $"\"{sprites}\""
                    },
                    new CSharpField(Visibility.Public, "string", "AssemblyName")
                    {
                        IsStatic = true,
                        IsReadonly = true,
                        DefaultValue = $"\"{Compilation.AssemblyName}\""
                    }
                }
            }.ToString();
            Context.AddSource("ResourceInfo", infoSourceText);
        } );
    }
}

public static class Extension
{
    public static string NormalizePath(this string path)
    {
        return path.Replace("\\", "/");
    }

    public static bool TryTrimStart(this string text, string value, [NotNullWhen(true)] out string? result)
    {
        if (text.StartsWith(value))
        {
            result = text[value.Length..];
            return true;
        }

        result = null;
        return false;
    }

    public static string GetResourcePath(this string path) => path.Replace("/", ".");

    public static string GetFileName(this string path, bool hasExtension)
    {
        var paths = path.Split('/').ToList();
        var fileName = paths.Last();
        if (!hasExtension)
        {
            fileName = fileName[..fileName.LastIndexOf('.')];    
        }
        
        return fileName;
    }
}