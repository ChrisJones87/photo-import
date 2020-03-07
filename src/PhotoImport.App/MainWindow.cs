using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoImport.App
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      public MainWindow()
      {
         InitializeComponent();
      }

      private async void ImportPhotos(object sender, RoutedEventArgs e)
      {
         Console.BufferHeight = short.MaxValue - 1;

         await Task.Run(async () =>
         {
            var suffix = "HTC";

            Console.WriteLine("Import photos started.");
            var sourceDirectory = $@"F:\Test\Source\{suffix}";
            var outputDirectory = $@"F:\Test\Output\{suffix}";
            var duplicateDirectory = $@"F:\Test\Duplicates\{suffix}";

            var directories = ProcessingDirectories.From(sourceDirectory, outputDirectory, duplicateDirectory);

            var cancellationToken = CancellationToken.None;

            var importer = new PhotoImporter(directories);

            await importer.ImportAsync(cancellationToken);

            Console.WriteLine("Complete");
         });
      }


   }
}
