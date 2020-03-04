using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhotoImport.App
{
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
}