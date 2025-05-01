using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;

namespace LeafLoop.Repositories.Interfaces
{
    public interface ICompanyRepository : IRepository<Company>
    {
        Task<Company> GetCompanyWithAddressAsync(int companyId);
        Task<IEnumerable<Company>> GetVerifiedCompaniesAsync();
        Task<double> GetCompanyRatingAverageAsync(int companyId);
    }
}
