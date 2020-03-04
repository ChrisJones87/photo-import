using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using ExifLibrary;
using Exception = System.Exception;
using Path = System.IO.Path;

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



   public sealed class FileOperation
   {
      public FileRecord SourceRecord { get; }

      public TargetDirectory TargetDirectory { get; }
      public string TargetFilename { get; }

      public FileOperation(FileRecord sourceRecord, TargetDirectory targetDirectory, string targetFilename)
      {
         SourceRecord = sourceRecord;
         TargetDirectory = targetDirectory;
         TargetFilename = targetFilename;
      }

      public static async Task<IReadOnlyList<FileOperation>> GenerateFileOperationsAsync(TargetDirectory targetDirectory)
      {
         var operations = new List<FileOperation>();

         var existingFiles = targetDirectory.GetExistingFilenames().ToHashSet();

         foreach (var record in targetDirectory.SourceRecords)
         {
            if (existingFiles.Contains(record.Filename))
            {
               // Duplicate found

            }
            else
            {
               var operation = new FileOperation(record, targetDirectory, record.Filename);
               operations.Add(operation);
            }

            existingFiles.Add(record.Filename);
         }

         return operations;
      }

      public override string ToString()
      {
         var source = Path.GetRelativePath(SourceRecord.RootPath.FullName, SourceRecord.File.FullName);
         var target = Path.GetRelativePath(TargetDirectory.DestinationRoot.FullName, TargetDirectory.DestinationDirectory.FullName);
         var targetFull = Path.Combine(target, SourceRecord.Filename);

         return $"{source} => {targetFull}";
      }
   }

   public sealed class TargetDirectory
   {
      public string Key { get; }

      public List<FileRecord> SourceRecords { get; }

      public DirectoryInfo DestinationRoot { get; }
      public DirectoryInfo DestinationDirectory { get; }

      public IReadOnlyList<string> GetExistingFilenames()
      {
         this.DestinationDirectory.Refresh();

         if (!DestinationDirectory.Exists)
            return Array.Empty<string>();

         return this.DestinationDirectory.GetFiles().Select(x => x.Name).ToArray();
      }

      private TargetDirectory(string key, List<FileRecord> sourceRecords, DirectoryInfo destinationRoot, DirectoryInfo destinationDirectory)
      {
         Key = key;
         SourceRecords = sourceRecords;
         DestinationRoot = destinationRoot;
         DestinationDirectory = destinationDirectory;
      }

      public static async Task<TargetDirectory> FromRecordAsync(string outputRoot, FileRecord record)
      {
         var key = record.GetTargetKey();

         if (key == null)
            return null;

         var outputRootInfo = new DirectoryInfo(outputRoot);
         var destination = record.GetTargetDirectory(outputRoot);

         return new TargetDirectory(key, new List<FileRecord>() { record }, outputRootInfo, destination);
      }
   }


   [DebuggerDisplay("{Filename}")]
   public class FileRecord
   {
      private static HashAlgorithm MD5 = new MD5CryptoServiceProvider();

      public DirectoryInfo RootPath { get; }

      public FileInfo File { get; }
      public string Filename => File.Name;
      public long FileSize => File.Length;

      public string FileHash { get; private set; }

      public DateTime? DateTime { get; private set; }

      public string GetTargetKey()
      {
         if (this.DateTime == null)
            return null;

         var dateTime = this.DateTime.Value;

         return $"{dateTime.Year:D4}/{dateTime.Month:D2}/{dateTime.Day:D2}";
      }

      public DirectoryInfo GetTargetDirectory(string root)
      {
         if (this.DateTime == null)
            return null;

         var dateTime = DateTime.Value;

         var path = Path.Combine(root, $"{dateTime.Year:D4}", $"{dateTime.Month:D2}", $"{dateTime.Day:D2}");
         return new DirectoryInfo(path);
      }


      public FileRecord(DirectoryInfo rootPath, FileInfo file, DateTime? dateTime)
      {
         RootPath = rootPath;
         DateTime = dateTime;
         File = file;
      }

      public static async Task<FileRecord> FromFileAsync(DirectoryInfo rootPath, FileInfo file, CancellationToken cancellationToken)
      {
         cancellationToken.ThrowIfCancellationRequested();

         var dateTaken = await TryDetectDate(file);

         var record = new FileRecord(rootPath, file, dateTaken);

         return record;
      }

      private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };

      private static async Task<DateTime?> TryDetectDate(FileInfo file)
      {
         var lastModifiedDate = file.LastWriteTimeUtc;

         try
         {
            // TODO: Blacklist certain folders such as .medres...
            if (ImageExtensions.Contains(file.Extension.ToLower()))
            {
               var imageFile = await ImageFile.FromFileAsync(file.FullName);

               return imageFile.Properties.Get<ExifDateTime>(ExifTag.DateTime);
            }

            return lastModifiedDate;
         }
         catch (Exception ex)
         {
            return lastModifiedDate;
         }
      }

      public async Task<string> CalculateHashAsync(CancellationToken cancellationToken)
      {
         cancellationToken.ThrowIfCancellationRequested();

         try
         {
            await using var stream = File.OpenRead();
            var hash = MD5.ComputeHash(stream);
            FileHash = Convert.ToBase64String(hash);
            //FileHash = BitConverter.ToString(hash);

            return FileHash;
         }
         catch (Exception e)
         {
            return null;
         }
      }
   }
}
