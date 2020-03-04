using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoImport.App.Utilities
{
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
               Console.WriteLine($"DUPLICATE: {record.Filename}");
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
}