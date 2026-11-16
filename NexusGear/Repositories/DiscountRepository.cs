using Microsoft.EntityFrameworkCore;
using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories.Interfaces;

namespace NexusGear.Repositories;

public class DiscountRepository : RepositoryBase<DiscountCode>, IDiscountRepository
{
    public DiscountRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<DiscountCode?> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _context.DiscountCodes
            .FirstOrDefaultAsync(d => d.Code.ToUpper() == normalizedCode);
    }
}
