using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoImport.App
{
   public interface IPhotoImporter
   {
      Task ImportAsync(IProgress<decimal> progress, CancellationToken cancellationToken = default);
   }
}