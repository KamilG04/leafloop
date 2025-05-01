using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface IAddressService
    {
        Task<AddressDto> GetAddressByIdAsync(int id);
        Task<int> CreateAddressAsync(AddressDto addressDto);
        Task UpdateAddressAsync(AddressDto addressDto);
        Task DeleteAddressAsync(int id);
        Task<IEnumerable<AddressDto>> GetAddressesByCityAsync(string city);
        Task<IEnumerable<AddressDto>> GetAddressesByProvinceAsync(string province);
    }
}
