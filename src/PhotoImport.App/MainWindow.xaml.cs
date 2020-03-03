using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PhotoImport.App
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      public MainWindow()
      {
         InitializeComponent();
      }

      private async void ImportPhotos(object sender, RoutedEventArgs e)
      {
         var files = await Task.Run(async() => await FindFilesAsync());

      }

      private async Task<IReadOnlyList<FileRecord>> FindFilesAsync(CancellationToken cancellation = default)
      {
         var root = $@"F:\Test";

         var rootInfo = new DirectoryInfo(root);

         var records = new List<FileRecord>();

         foreach (var file in rootInfo.EnumerateFiles())
         {
            var record = await FileRecord.FromFileAsync(file);

            records.Add(record);
         }

         return records;
      }
   }


   public class FileRecord
   {
      public FileInfo File { get; }
      public string Filename => File.Name;
      public long FileSize => File.Length;

      public FileRecord(FileInfo file)
      {
         File = file;
      }

      public static async Task<FileRecord> FromFileAsync(FileInfo file)
      {
         return new FileRecord(file);
      }
   }
}
