using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LeafLoop.Repositories
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        public CompanyRepository(LeafLoopDbContext context) : base(context)
        {
        }

        public async Task<Company> GetCompanyWithAddressAsync(int companyId)
        {
            return await _context.Companies
                .Include(c => c.Address)
                .SingleOrDefaultAsync(c => c.Id == companyId);
        }

        public async Task<IEnumerable<Company>> GetVerifiedCompaniesAsync()
        {
            return await _context.Companies
                .Where(c => c.VerificationStatus == VerificationStatus.Verified)
                .ToListAsync();
        }

        public async Task<double> GetCompanyRatingAverageAsync(int companyId)
        {
            var ratings = await _context.Ratings
                .Where(r => r.RatedEntityId == companyId && r.RatedEntityType == RatedEntityType.Company)
                .ToListAsync();

            if (ratings.Any())
            {
                return ratings.Average(r => r.Value);
            }

            return 0;
        }
    }
}