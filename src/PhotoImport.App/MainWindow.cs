using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PhotoImport.App.Utilities;

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
         await Task.Run(async () =>
         {
            Console.WriteLine("Import photos started.");
            var sourceDirectory = $@"F:\Test\Source";
            var outputDirectory = $@"F:\Test\Output";

            var cancellationToken = CancellationToken.None;

            Console.WriteLine("Finding files...");
            var records = await FindFilesAsync(sourceDirectory, cancellationToken);

            Console.WriteLine($"Found {records.Count} files.");


            Console.WriteLine("Processing files...");
            var targets = await ProcessFilesAsync(records, outputDirectory, cancellationToken);

            Console.WriteLine($"Found {targets.Count} directory targets");

            var operations = new List<FileOperation>();

            foreach (var target in targets)
            {
               Console.WriteLine($"Generating file operations for target {target.Key}");
               var targetOperations = await FileOperation.GenerateFileOperationsAsync(target);

               Console.WriteLine($"Found {targetOperations.Count} file operations.");

               operations.AddRange(targetOperations);
            }

            Console.WriteLine("Showing all operations:");
            foreach (var operation in operations)
            {
               Console.WriteLine(operation);
            }
         });
      }


      private async Task<IReadOnlyList<TargetDirectory>> ProcessFilesAsync(IReadOnlyList<FileRecord> records, string output, CancellationToken cancellationToken = default)
      {
         var outputs = new Dictionary<string, TargetDirectory>();

         foreach (var record in records)
         {
            var key = record.GetTargetKey();

            if (!outputs.TryGetValue(key, out var targetDirectory))
            {
               targetDirectory = await TargetDirectory.FromRecordAsync(output, record);
               outputs[key] = targetDirectory;
            }
            else
            {
               targetDirectory.SourceRecords.Add(record);
            }
         }

         return outputs.Values.ToArray();
      }

      private async Task<IReadOnlyList<FileRecord>> FindFilesAsync(string sourceDirectory, CancellationToken cancellation = default)
      {
         var rootDirectory = new DirectoryInfo(sourceDirectory);

         var records = new List<FileRecord>();

         await ProcessDirectoryAsync(rootDirectory, rootDirectory, records, cancellation);

         return records;
      }

      private async Task ProcessDirectoryAsync(DirectoryInfo sourceRoot, DirectoryInfo root, List<FileRecord> records, CancellationToken cancellationToken)
      {
         cancellationToken.ThrowIfCancellationRequested();

         if (!root.Exists)
            return;

         foreach (var file in root.EnumerateFiles())
         {
            var record = await FileRecord.FromFileAsync(sourceRoot, file, cancellationToken);

            records.Add(record);
         }

         foreach (var subdirectory in root.EnumerateDirectories())
         {
            await ProcessDirectoryAsync(sourceRoot, subdirectory, records, cancellationToken);
         }
      }
   }
}
