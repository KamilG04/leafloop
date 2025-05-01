using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IPhotoRepository : IRepository<Photo>
    {
        Task<IEnumerable<Photo>> GetPhotosByItemAsync(int itemId);
    }
}
