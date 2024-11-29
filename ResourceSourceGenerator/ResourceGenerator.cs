using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CSharpPoet;
using Microsoft.CodeAnalysis;
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

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
            SpriteInfoJson? spriteConfig = null;
            if (Configs.TryGetValue("SpriteInfos.json", out var configText))
                spriteConfig = JsonSerializer.Deserialize<SpriteInfoJson>(configText);
            var spriteFields = new List<CSharpType.IMember>();
            foreach (var file in sprites)
            {
                var fullName = file.RelativePath.GetFileName(true);
                var filedName = file.RelativePath.GetFileName(false);
                var spritePath = file.RelativePath.GetResourcePath();
                var value = "new (";
                var info = filedName;
                
                value += "\"";
                value += spritePath;
                value += "\"";
                if (spriteConfig != null)
                {
                    if (spriteConfig.Pixel.TryGetValue(fullName, out var pixelValue))
                    {
                        value += $", {pixelValue}f";
                        info += $"_{pixelValue}f";
                    }

                    if (spriteConfig.Rect.TryGetValue(fullName, out var rect))
                    {
                        value += $", {rect.x}";
                        value += $", {rect.y}";
                        info += $"_({rect.x},{rect.y})";
                    }
                }
                
                value += ")";
                spriteFields.Add(new CSharpField(Visibility.Public, "ResourceSprite", filedName)
                {
                    IsStatic = true,
                    IsReadonly = true,
                    DefaultValue = value
                });
                Sprites.Add(info);
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
                    new CSharpUsing("NextChat.Core")
                },
                Members = { sourceClass } 
            }.ToString();
            
            productionContext.AddSource("Sprites", spriteSourceText);
        });
        
        var libProvider = fileProvider.Where(n => n.RelativePath.StartsWith("Lib") && n.RelativePath.EndsWith(".dll")).Collect();
        context.RegisterSourceOutput(libProvider, (productionContext, libs) =>
        {
            var libsValue = "";
            foreach (var file in libs)
            {
                libsValue += $"new (\"{file.RelativePath.GetResourcePath()}\")";
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
                    new CSharpUsing("NextChat.Core")
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

public class SpriteInfoJson
{
    public Dictionary<string, float> Pixel { get; set; }

    public Dictionary<string, rect> Rect { get; set; }
    
    // ReSharper disable once ClassNeverInstantiated.Global
    public record rect(int x, int y);
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