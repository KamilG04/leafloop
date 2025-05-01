using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;

namespace LeafLoop.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<CompanyDto> GetCompanyByIdAsync(int id);
        Task<CompanyWithDetailsDto> GetCompanyWithDetailsAsync(int id);
        Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync();
        Task<IEnumerable<CompanyDto>> GetVerifiedCompaniesAsync();
        Task<int> RegisterCompanyAsync(CompanyRegistrationDto registrationDto);
        Task UpdateCompanyAsync(CompanyUpdateDto companyDto);
        Task UpdateCompanyAddressAsync(int companyId, AddressDto addressDto);
        Task<bool> VerifyCompanyAsync(int id, VerificationStatus status);
        Task<double> GetCompanyRatingAverageAsync(int companyId);
    }
}
