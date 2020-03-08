using System.IO;
using System.Threading.Tasks;
using Exception = System.Exception;

namespace PhotoImport.App.Utilities
{
   public sealed class FileOperation
   {
      public FileRecord SourceRecord { get; set; }
      public DirectoryInfo DestinationRoot { get; set; }
      public DirectoryInfo DestinationDirectory { get; set; }
      public string TargetFilename { get; set; }

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

      public async Task<(bool Success,string Message)> RunAsync()
      {
         var file = SourceRecord.File;

         file.Refresh();

         if (!file.Exists)
         {
            return (false, $"File '{file.FullName}' no longer exists...");
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
            return (true, "");
         }
         catch (Exception ex)
         {
            return (false, $"Failed to move file: {ex}");
         }
      }
   }
}