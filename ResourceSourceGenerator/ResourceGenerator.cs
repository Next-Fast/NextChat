using CSharpPoet;
using Microsoft.CodeAnalysis;

namespace ResourceSourceGenerator;

[Generator(LanguageNames.CSharp)]
public class ResourceGenerator : IIncrementalGenerator
{
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var fileProvider = context
            .AdditionalTextsProvider
            .Where(n => n.Path.EndsWith(".png"))
            .Collect();
        
        context.RegisterSourceOutput(fileProvider, (productionContext, text) =>
        {
            var spriteFields = new List<CSharpType.IMember>();
            foreach (var file in text)
            {
                var paths = file.Path.Replace("\\", ".").Replace("/", ".").Split('.').ToList();
                var root = paths.IndexOf("Resource");
                var name = paths[paths.Count - 2];
                var filedName = name;
                string? pixel = null;
                if (name.Contains('_'))
                {
                    var split = name.Split('_');
                    filedName = split[0];
                    pixel = split[1];
                }
                    
                var value = "new (";
                value += "\"";
                for (var i = root + 1; i < paths.Count - 3; i++)
                {
                    value += $"{paths[i]}.";
                }
                
                value += $"{filedName}.png";
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

            
            var infoSourceText = new CSharpFile("NextResources")
            {
                new CSharpClass("ResourceInfo")
                {
                    new CSharpField(Visibility.Public, "string", "BuildTime")
                    {
                        IsStatic = true,
                        IsReadonly = true,
                        DefaultValue = $"\"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\""
                    }
                }
            }.ToString();
            
            productionContext.AddSource("Sprites", spriteSourceText);
            productionContext.AddSource("ResourceInfo", infoSourceText);
        } );
    }
}