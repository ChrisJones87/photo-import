using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Exception = System.Exception;

namespace PhotoImport.App.Utilities
{
   public sealed class FileOperation
   {
      public FileRecord SourceRecord { get; }

      public DirectoryInfo DestinationRoot { get; }
      public DirectoryInfo DestinationDirectory { get; }
      public string TargetFilename { get; }

      public static FileOperation From(FileRecord sourceRecord, TargetDirectory targetDirectory, string filename = null)
      {
         return new FileOperation(sourceRecord, targetDirectory.ProcessingDirectories.OutputDirectory, targetDirectory.DestinationDirectory, filename ?? sourceRecord.Filename);
      }

      public static FileOperation From(ProcessingDirectories directories, FileRecord sourceRecord, DirectoryInfo targetDirectory, string filename = null)
      {
         return new FileOperation(sourceRecord, directories.OutputDirectory, targetDirectory, filename ?? sourceRecord.Filename);
      }

      public FileOperation(FileRecord sourceRecord, DirectoryInfo destinationRoot, DirectoryInfo destinationDirectory, string targetFilename)
      {
         SourceRecord = sourceRecord;
         DestinationRoot = destinationRoot;
         DestinationDirectory = destinationDirectory;
         TargetFilename = targetFilename;
      }

      public override string ToString()
      {
         var source = Path.GetRelativePath(SourceRecord.RootPath.FullName, SourceRecord.File.FullName);
         var target = Path.GetRelativePath(DestinationRoot.FullName, DestinationDirectory.FullName);
         var targetFull = Path.Combine(target, SourceRecord.Filename);

         return $"{source} => {targetFull}";
      }

      public async Task RunAsync()
      {
         var file = SourceRecord.File;

         file.Refresh();

         if (!file.Exists)
         {
            Console.WriteLine($"File '{file.FullName}' no longer exists...");
            return;
         }

         var destinationDirectory = DestinationDirectory;
         destinationDirectory.Refresh();

         if (!destinationDirectory.Exists)
         {
            destinationDirectory.Create();
         }

         var targetFilename = Path.Combine(destinationDirectory.FullName, TargetFilename);

         try
         {
            file.MoveTo(targetFilename, overwrite: false);
         }
         catch (Exception ex)
         {
            Console.WriteLine($"Failed to move file");
            Console.WriteLine(ex);
         }
      }
   }
}