using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoImport.App.Utilities
{
   public sealed class TargetDirectory
   {
      public string Key { get; }

      public List<FileRecord> SourceRecords { get; }

      public ProcessingDirectories ProcessingDirectories { get; }
      public DirectoryInfo DestinationDirectory { get; }

      public IReadOnlyList<string> GetExistingFilenames()
      {
         this.DestinationDirectory.Refresh();

         if (!DestinationDirectory.Exists)
            return Array.Empty<string>();

         return this.DestinationDirectory.GetFiles().Select(x => x.Name).ToArray();
      }

      public async Task<IReadOnlyList<FileOperation>> GenerateFileOperationsAsync(DirectoryInfo duplicateRoot)
      {
         var operations = new List<FileOperation>();

         var existingFiles = GetExistingFilenames().ToHashSet();

         foreach (var record in SourceRecords)
         {
            if (existingFiles.Contains(record.Filename))
            {
               // Duplicate found
               Console.WriteLine($"DUPLICATE: {record.Filename}");
            }
            else
            {
               var operation = FileOperation.From(record, this);
               operations.Add(operation);
            }

            existingFiles.Add(record.Filename);
         }

         return operations;
      }

      private TargetDirectory(string key, List<FileRecord> sourceRecords, ProcessingDirectories processingDirectories, DirectoryInfo destinationDirectory)
      {
         Key = key;
         SourceRecords = sourceRecords;
         ProcessingDirectories = processingDirectories;
         DestinationDirectory = destinationDirectory;
      }

      public static async Task<TargetDirectory> FromRecordAsync(ProcessingDirectories processingDirectories, FileRecord record)
      {
         var key = record.GetTargetKey();

         if (key == null)
            return null;

         var destination = record.GetTargetDirectory(processingDirectories.OutputDirectory.FullName);

         return new TargetDirectory(key, new List<FileRecord>() { record }, processingDirectories, destination);
      }
   }
}