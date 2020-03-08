using System;
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


      public MainWindow(ILogger<MainWindow> logger, IServiceProvider serviceProvider)
      {
         _logger = logger;
         _serviceProvider = serviceProvider;
         InitializeComponent();
      }

      private async void ImportPhotos(object sender, RoutedEventArgs e)
      {
         Console.BufferHeight = short.MaxValue - 1;

         await Task.Run(async () =>
         {
            var suffix = "HTC";

            _logger.LogInformation("Import photos started.");
            var sourceDirectory = $@"F:\Test\Source\{suffix}";
            var outputDirectory = $@"F:\Test\Output\{suffix}";
            var duplicateDirectory = $@"F:\Test\Duplicates\{suffix}";

            var directories = ProcessingDirectories.From(sourceDirectory, outputDirectory, duplicateDirectory);

            var cancellationToken = CancellationToken.None;

            var logger = _serviceProvider.GetService<ILogger<PhotoImporter>>();
            var importer = new PhotoImporter(directories, logger);

            await importer.ImportAsync(cancellationToken);

            _logger.LogInformation("Complete");
         });
      }


      private void SelectOrganiseOutputDirectory(object sender, RoutedEventArgs e)
      {
         
      }

      private void SelectOrganiseSourceDirectory(object sender, RoutedEventArgs e)
      {
         throw new NotImplementedException();
      }
   }
}
