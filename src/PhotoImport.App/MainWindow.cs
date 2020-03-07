using PhotoImport.App.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
         Console.BufferHeight = Int16.MaxValue - 1;

         await Task.Run(async () =>
         {
            var suffix = "Pixel";

            Console.WriteLine("Import photos started.");
            var sourceDirectory = $@"F:\Test\Source\{suffix}";
            var outputDirectory = $@"F:\Test\Output\{suffix}";
            var duplicateDirectory = $@"F:\Test\Duplicates\{suffix}";

            var directories = ProcessingDirectories.From(sourceDirectory, outputDirectory, duplicateDirectory);

            var cancellationToken = CancellationToken.None;

            Console.WriteLine("Finding files...");
            var records = await FindFilesAsync(sourceDirectory, cancellationToken);

            Console.WriteLine($"Found {records.Count} files.");


            Console.WriteLine("Processing files...");
            var targets = await ProcessFilesAsync(records, directories, cancellationToken);

            Console.WriteLine($"Found {targets.Count} directory targets");

            var operations = new List<FileOperation>();

            foreach (var target in targets)
            {
               Console.WriteLine($"Generating file operations for target {target.Key}");
               var targetOperations = await target.GenerateActionPlanAsync(directories.DuplicateDirectory);

               Console.WriteLine($"Found {targetOperations.FileOperations.Count} file operations.");

               operations.AddRange(targetOperations.FileOperations);
            }

            Console.WriteLine("Showing all operations:");
            foreach (var operation in operations)
            {
               cancellationToken.ThrowIfCancellationRequested();

               Console.WriteLine(operation);
               await operation.RunAsync();
            }

            Console.WriteLine("Complete");
         });
      }


      private async Task<IReadOnlyList<TargetDirectory>> ProcessFilesAsync(IReadOnlyList<FileRecord> records, 
                                                                           ProcessingDirectories directories,
                                                                           CancellationToken cancellationToken = default)
      {
         var outputs = new Dictionary<string, TargetDirectory>();

         foreach (var record in records)
         {
            var key = record.GetTargetKey();

            if (!outputs.TryGetValue(key, out var targetDirectory))
            {
               targetDirectory = await TargetDirectory.FromRecordAsync(directories, record);
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

      private static readonly string[] BlacklistedPrefixes = { "." };

      private async Task ProcessDirectoryAsync(DirectoryInfo sourceRoot, DirectoryInfo root, List<FileRecord> records, CancellationToken cancellationToken)
      {
         cancellationToken.ThrowIfCancellationRequested();

         if (!root.Exists)
            return;

         if (BlacklistedPrefixes.Any(prefix => root.Name.StartsWith(prefix)))
         {
            Console.WriteLine($"Ignoring {root.FullName} as it starts with a period.");
            return;
         }

         Console.WriteLine($"Searching in {root.FullName} for files...");

         foreach (var file in root.EnumerateFiles())
         {
            var record = await FileRecord.FromFileAsync(sourceRoot, file, cancellationToken);

            Console.WriteLine($"Found {record.Filename}");

            records.Add(record);
         }

         foreach (var subdirectory in root.EnumerateDirectories())
         {
            await ProcessDirectoryAsync(sourceRoot, subdirectory, records, cancellationToken);
         }
      }
   }
}
