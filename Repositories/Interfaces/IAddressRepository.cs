using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface IAddressRepository : IRepository<Address>
    {
        Task<IEnumerable<Address>> GetAddressesByProvinceAsync(string province);
        Task<IEnumerable<Address>> GetAddressesByCityAsync(string city);
    }
}
