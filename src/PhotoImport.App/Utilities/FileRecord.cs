using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ExifLibrary;

namespace PhotoImport.App.Utilities
{
   [DebuggerDisplay("{Filename}")]
   public class FileRecord
   {
      private static HashAlgorithm MD5 = new MD5CryptoServiceProvider();

      public DirectoryInfo RootPath { get; }

      public FileInfo File { get; }
      public string Filename => File.Name;
      public long FileSize => File.Length;

      public string FileHash { get; private set; }

      public DateTime? DateTime { get; private set; }

      public string GetTargetKey()
      {
         if (this.DateTime == null)
            return null;

         var dateTime = this.DateTime.Value;

         return $"{dateTime.Year:D4}/{dateTime.Month:D2}/{dateTime.Day:D2}";
      }

      public DirectoryInfo GetTargetDirectory(string root)
      {
         if (this.DateTime == null)
            return null;

         var dateTime = DateTime.Value;

         var path = Path.Combine(root, $"{dateTime.Year:D4}", $"{dateTime.Month:D2}", $"{dateTime.Day:D2}");
         return new DirectoryInfo(path);
      }


      public FileRecord(DirectoryInfo rootPath, FileInfo file, DateTime? dateTime)
      {
         RootPath = rootPath;
         DateTime = dateTime;
         File = file;
      }

      public static async Task<FileRecord> FromFileAsync(DirectoryInfo rootPath, FileInfo file, CancellationToken cancellationToken)
      {
         cancellationToken.ThrowIfCancellationRequested();

         var dateTaken = await TryDetectDate(file);

         var record = new FileRecord(rootPath, file, dateTaken);

         return record;
      }

      private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".tiff" };

      private static async Task<DateTime?> TryDetectDate(FileInfo file)
      {
         var lastModifiedDate = file.LastWriteTimeUtc;

         try
         {
            // TODO: Blacklist certain folders such as .medres...
            if (ImageExtensions.Contains(file.Extension.ToLower()))
            {
               var imageFile = await ImageFile.FromFileAsync(file.FullName);

               return imageFile.Properties.Get<ExifDateTime>(ExifTag.DateTime);
            }

            return lastModifiedDate;
         }
         catch (Exception ex)
         {
            return lastModifiedDate;
         }
      }

      public async Task<string> CalculateHashAsync(CancellationToken cancellationToken)
      {
         cancellationToken.ThrowIfCancellationRequested();

         try
         {
            await using var stream = File.OpenRead();
            var hash = MD5.ComputeHash(stream);
            FileHash = Convert.ToBase64String(hash);
            //FileHash = BitConverter.ToString(hash);

            return FileHash;
         }
         catch (Exception e)
         {
            return null;
         }
      }
   }
}