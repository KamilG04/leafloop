using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class AddressRepository : Repository<Address>, IAddressRepository
    {
        public AddressRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Address>> GetAddressesByProvinceAsync(string province)
        {
            return await _context.Addresses
                .Where(a => a.Province == province)
                .ToListAsync();
        }

        public async Task<IEnumerable<Address>> GetAddressesByCityAsync(string city)
        {
            return await _context.Addresses
                .Where(a => a.City == city)
                .ToListAsync();
        }
    }
}