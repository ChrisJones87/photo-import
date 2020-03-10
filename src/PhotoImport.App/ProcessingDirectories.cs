using System.IO;

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

         if (!sdi.Exists)
         {
            throw new DirectoryNotFoundException(sourceDirectory);
         }

         if (!odi.Exists)
         {
            odi.Create();
         }

         return new ProcessingDirectories(sdi, odi, null);
      }
   }
}