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

      public DirectoryInfo DestinationRoot { get; }
      public DirectoryInfo DestinationDirectory { get; }
      public string TargetFilename { get; }

      public static FileOperation From(FileRecord sourceRecord, TargetDirectory targetDirectory)
      {
         return new FileOperation(sourceRecord, targetDirectory.ProcessingDirectories.OutputDirectory, targetDirectory.DestinationDirectory, sourceRecord.Filename);
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
   }
}