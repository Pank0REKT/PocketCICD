using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using PocketCICD.Interfaces;
using PocketCICD.Services;

namespace PocketCICD
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);

            Services = services.BuildServiceProvider();

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<MainWindow>();
        }
    }
}