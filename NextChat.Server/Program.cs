namespace NextChat.Server;

public static class Program
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
