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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PhotoImport.App.Utilities;

namespace PhotoImport.App
{
   public class ProcessingDirectories
   {
      public DirectoryInfo SourceDirectory { get; }

      public DirectoryInfo OutputDirectory { get; }

      public DirectoryInfo DuplicateDirectory { get; }

      public ProcessingDirectories(DirectoryInfo sourceDirectory, DirectoryInfo outputDirectory, DirectoryInfo duplicateDirectory)
      {
         SourceDirectory = sourceDirectory;
         OutputDirectory = outputDirectory;
         DuplicateDirectory = duplicateDirectory;
      }

      public static ProcessingDirectories From(string sourceDirectory, string outputDirectory, string duplicateDirectory)
      {
         var sdi = new DirectoryInfo(sourceDirectory);
         var odi = new DirectoryInfo(outputDirectory);
         var ddi = new DirectoryInfo(duplicateDirectory);

         if (!sdi.Exists)
         {
            throw new DirectoryNotFoundException(sourceDirectory);
         }

         if (!odi.Exists)
         {
            odi.Create();
         }

         if (!ddi.Exists)
         {
            ddi.Create();
         }

         return new ProcessingDirectories(sdi, odi, ddi);
      }
   }

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
            Console.WriteLine("Import photos started.");
            var sourceDirectory = @"F:\Test\Source";
            var outputDirectory = @"F:\Test\Output";
            var duplicateDirectory = @"F:\Test\Duplicates";

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
               var targetOperations = await target.GenerateFileOperationsAsync(directories.DuplicateDirectory);

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
