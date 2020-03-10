using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PhotoImport.App
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      private readonly ILogger<MainWindow> _logger;
      private readonly IServiceProvider _serviceProvider;
      private readonly ILoggerFactory _loggerFactory;


      public MainWindow(ILogger<MainWindow> logger, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
      {
         _logger = logger;
         _serviceProvider = serviceProvider;
         _loggerFactory = loggerFactory;
         InitializeComponent();
      }

      private async void ImportPhotos(object sender, RoutedEventArgs e)
      {
         Console.BufferHeight = short.MaxValue - 1;

         _logger.LogInformation("Import Photos requested by user");

         try
         {
            ImportPhotosButton.IsEnabled = false;

            var sourceDirectory = OrganiseSourceTextBox.Text;
            var outputDirectory = OrganiseOutputTextBox.Text;

            _logger.LogInformation($"Source Directory: {sourceDirectory}");
            _logger.LogInformation($"Output Directory: {outputDirectory}");

            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
               MessageBox.Show(this, $"The source directory must be supplied.", "Error", MessageBoxButton.OK);
               return;
            }

            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
               MessageBox.Show(this, $"The output directory must be supplied.", "Error", MessageBoxButton.OK);
               return;
            }

            if (!Directory.Exists(sourceDirectory))
            {
               MessageBox.Show(this, $"The source directory {sourceDirectory} does not exist.", "Error", MessageBoxButton.OK);
               return;
            }

            await Task.Run(async () =>
            {
               _logger.LogInformation($"Import photos started from {sourceDirectory} to {outputDirectory}");

               var directories = ProcessingDirectories.From(sourceDirectory, outputDirectory, null);

               var cancellationToken = CancellationToken.None;

               var logger = _serviceProvider.GetService<ILogger<PhotoImporter>>();
               var importer = new PhotoImporter(directories, logger);

               await importer.ImportAsync(cancellationToken);

               _logger.LogInformation("Complete");
            });
         }
         finally
         {
            ImportPhotosButton.IsEnabled = true;
         }
      }
   }
}
