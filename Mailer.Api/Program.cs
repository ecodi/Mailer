using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Mailer.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureAppConfiguration((context, confBuilder) =>
                    {
                        confBuilder
                            .AddJsonFile("users.json")
                            .AddEnvironmentVariables();
                    });
                    webBuilder.UseStartup<Startup>();
                });
    }
}
