using System.Threading;
using System.Threading.Tasks;

namespace PhotoImport.App
{
   public interface IPhotoImporter
   {
      Task ImportAsync(CancellationToken cancellationToken = default);
   }
}