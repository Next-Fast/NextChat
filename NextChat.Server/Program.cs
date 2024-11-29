using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NextChat.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder()
            .UseConsoleLifetime()
            .ConfigureWebHost(
                hostBuilder =>
                {
                    hostBuilder.ConfigureServices((context, collection) =>
                    {

                    });

                    hostBuilder.Configure((context, applicationBuilder) =>
                    {
                    });
                },
                options =>
                {
                })
            .ConfigureServices((builder, services) =>
            {

            });

        var serverApp = builder.Build();
        try
        {
            await serverApp.RunAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
