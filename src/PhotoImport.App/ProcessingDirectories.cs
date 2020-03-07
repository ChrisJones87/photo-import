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
         var ddi = new DirectoryInfo(duplicateDirectory);

         if (!sdi.Exists)
         {
            throw new DirectoryNotFoundException(sourceDirectory);
         }

         if (!odi.Exists)
         {
            odi.Create();
         }

         if (!ddi.Exists)
         {
            ddi.Create();
         }

         return new ProcessingDirectories(sdi, odi, ddi);
      }
   }
}