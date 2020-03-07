using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhotoImport.App.Utilities;

namespace PhotoImport.App
{
   public static class PhotoImporter
   {
      public static async Task ImportAsync(ProcessingDirectories directories, CancellationToken cancellationToken = default)
      {
         await ApplyStage1Async(directories, cancellationToken);
      }

      private static async Task ApplyStage1Async(ProcessingDirectories directories, CancellationToken cancellationToken)
      {
         Console.WriteLine("Finding files...");
         var records = await FindFilesAsync(directories.SourceDirectory, cancellationToken);

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
      }


      private static async Task<IReadOnlyList<TargetDirectory>> ProcessFilesAsync(IReadOnlyList<FileRecord> records,
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

      private static async Task<IReadOnlyList<FileRecord>> FindFilesAsync(DirectoryInfo sourceDirectory, CancellationToken cancellation = default)
      {
         var records = new List<FileRecord>();

         await ProcessDirectoryAsync(sourceDirectory, sourceDirectory, records, cancellation);

         return records;
      }

      private static readonly string[] BlacklistedPrefixes = { "." };

      private static async Task ProcessDirectoryAsync(DirectoryInfo sourceRoot, DirectoryInfo root, List<FileRecord> records, CancellationToken cancellationToken)
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