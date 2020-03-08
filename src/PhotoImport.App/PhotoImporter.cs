using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhotoImport.App.Utilities;

namespace PhotoImport.App
{
   public class PhotoImporter : IPhotoImporter
   {
      private readonly ProcessingDirectories _directories;
      private readonly ILogger<PhotoImporter> _logger;

      public PhotoImporter(ProcessingDirectories directories, ILogger<PhotoImporter> logger)
      {
         _directories = directories;
         _logger = logger;
      }

      public async Task ImportAsync(CancellationToken cancellationToken)
      {
         await MoveUniqueFilesAsync(cancellationToken);

         await ProcessDuplicatesAsync(cancellationToken);
      }

      private async Task ProcessDuplicatesAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation($"Finding all potential duplicates...");
         var potentialDuplicates = await FindFilesAsync(_directories.SourceDirectory, cancellationToken);

         _logger.LogInformation($"Found {potentialDuplicates.Count} files.");

         foreach (var potentialDuplicate in potentialDuplicates)
         {
            _logger.LogInformation($"Processing {potentialDuplicate.Filename}...");

            var targetDirectory = potentialDuplicate.GetTargetDirectory(_directories.OutputDirectory.FullName);

            var targetFile = new FileInfo(Path.Combine(targetDirectory.FullName, potentialDuplicate.Filename));

            if (!targetFile.Exists)
            {
               _logger.LogInformation($"Matching file does not exist. This should not happen!");
               Debugger.Break();
            }

            var isDuplicate = await CheckFilesMatchAsync(potentialDuplicate.File, targetFile, cancellationToken);

            if (isDuplicate)
            {
               _logger.LogInformation($"The file {potentialDuplicate.File.FullName} is a duplicate and will be ignored.");
            }
            else
            {
               _logger.LogInformation($"The file {potentialDuplicate.File.FullName} is not a duplicate!");

               var newFilename = await GenerateFilenameForDuplicateAsync(targetDirectory, potentialDuplicate.File);
               _logger.LogInformation($"File will be called {newFilename}");
               var operation = FileOperation.From(_directories, potentialDuplicate, targetDirectory, newFilename);

               _logger.LogInformation("Running file operation...");
               var result = await operation.RunAsync();

               if (!result.Success)
               {
                  _logger.LogError(result.Message);
               }
            }
         }
      }

      private async Task<string> GenerateFilenameForDuplicateAsync(DirectoryInfo targetDirectory, FileInfo sourceFile)
      {
         var extension = Path.GetExtension(sourceFile.FullName);
         var name = Path.GetFileNameWithoutExtension(sourceFile.FullName);

         int suffix = 1;

         while (true)
         {
            var newFilename = new FileInfo(Path.Combine(targetDirectory.FullName, $"{name} - {suffix:D2}{extension}"));

            if (!newFilename.Exists)
            {
               return newFilename.FullName;
            }

            ++suffix;
         }
      }

      private async Task<bool> CheckFilesMatchAsync(FileInfo file1, FileInfo file2, CancellationToken cancellationToken)
      {
         if (file1.Length != file2.Length)
         {
            _logger.LogInformation("Files are different lengths.");
            return false;
         }

         var file1Data = await File.ReadAllBytesAsync(file1.FullName, cancellationToken);
         var file2Data = await File.ReadAllBytesAsync(file2.FullName, cancellationToken);

         if (file1Data.Length != file2Data.Length)
         {
            _logger.LogInformation("File contents are different lengths.");
            return false;
         }

         for (long index = 0; index < file1Data.Length; ++index)
         {
            if (file1Data[index] != file2Data[index])
            {
               _logger.LogInformation("File contents does not match");
               return false;
            }
         }

         return true;
      }

      private async Task MoveUniqueFilesAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Finding files...");
         var records = await FindFilesAsync(_directories.SourceDirectory, cancellationToken);

         _logger.LogInformation($"Found {records.Count} files.");

         _logger.LogInformation("Processing files...");
         var targets = await ProcessFilesAsync(records, cancellationToken);

         _logger.LogInformation($"Found {targets.Count} directory targets");

         var operations = new List<FileOperation>();

         foreach (var target in targets)
         {
            _logger.LogInformation($"Generating file operations for target {target.Key}");
            var targetOperations = await target.GenerateActionPlanAsync(_directories.DuplicateDirectory);

            _logger.LogInformation($"Found {targetOperations.FileOperations.Count} file operations.");

            operations.AddRange(targetOperations.FileOperations);
         }

         _logger.LogInformation("Running all operations:");
         foreach (var operation in operations)
         {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(operation.ToString());
            await operation.RunAsync();
         }
      }


      private async Task<IReadOnlyList<TargetDirectory>> ProcessFilesAsync(IReadOnlyList<FileRecord> records,
                                                                           CancellationToken cancellationToken)
      {
         var outputs = new Dictionary<string, TargetDirectory>();

         foreach (var record in records)
         {
            var key = record.GetTargetKey();

            if (!outputs.TryGetValue(key, out var targetDirectory))
            {
               targetDirectory = await TargetDirectory.FromRecordAsync(_directories, record);
               outputs[key] = targetDirectory;
            }
            else
            {
               targetDirectory.SourceRecords.Add(record);
            }
         }

         return outputs.Values.ToArray();
      }

      private async Task<IReadOnlyList<FileRecord>> FindFilesAsync(DirectoryInfo sourceDirectory, CancellationToken cancellation = default)
      {
         var records = new List<FileRecord>();

         await ProcessDirectoryAsync(sourceDirectory, sourceDirectory, records, cancellation);

         return records;
      }

      private static readonly string[] BlacklistedPrefixes = { "." };
      private static readonly string[] BlacklistedExtensions = {".bin", ".exe", ".dll"};

      private async Task ProcessDirectoryAsync(DirectoryInfo sourceRoot, DirectoryInfo root, List<FileRecord> records, CancellationToken cancellationToken)
      {
         cancellationToken.ThrowIfCancellationRequested();

         if (!root.Exists)
            return;

         if (BlacklistedPrefixes.Any(prefix => root.Name.StartsWith(prefix)))
         {
            _logger.LogInformation($"Ignoring {root.FullName} as it starts with a period.");
            return;
         }

         _logger.LogInformation($"Searching in {root.FullName} for files...");

         foreach (var file in root.EnumerateFiles())
         {
            var record = await FileRecord.FromFileAsync(sourceRoot, file, cancellationToken);

            if (BlacklistedExtensions.Any(ext => record.File.Extension.ToLower() == ext))
            {
               _logger.LogInformation($"Ignoring {record.File.FullName} as its extension is {record.File.Extension}");
               return;
            }

            _logger.LogInformation($"Found {record.Filename}");

            records.Add(record);
         }

         foreach (var subdirectory in root.EnumerateDirectories())
         {
            await ProcessDirectoryAsync(sourceRoot, subdirectory, records, cancellationToken);
         }
      }
   }
}