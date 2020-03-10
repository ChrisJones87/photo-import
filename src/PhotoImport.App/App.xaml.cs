using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace PhotoImport.App
{
   /// <summary>
   /// Interaction logic for App.xaml
   /// </summary>
   public partial class App : Application
   {
      private readonly IHost _host;

      public App()
      {
         _host = new HostBuilder()
            .ConfigureServices((context, services) =>
            {
               services.AddSingleton<MainWindow>();
            })
            .ConfigureAppConfiguration((context, config) =>
            {
               config.SetBasePath(context.HostingEnvironment.ContentRootPath);
            })
            .ConfigureLogging(logging =>
            {
               logging.AddSerilog();

               var logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotoImporter", "PhotoImport-{Date}.log");
               logging.AddFile(logFilePath);
               logging.AddConsole();
            })
            .Build();
      }

      private void App_OnStartup(object sender, StartupEventArgs e)
      {
         _host.StartAsync().Wait();

         MainWindow = _host.Services.GetRequiredService<MainWindow>();
         MainWindow.Show();
      }

      private void App_OnExit(object sender, ExitEventArgs e)
      {
         using (_host)
         {
            _host.StopAsync().Wait();
         }
         
      }
   }
}
