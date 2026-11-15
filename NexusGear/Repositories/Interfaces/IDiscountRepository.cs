using NexusGear.Models;

namespace NexusGear.Repositories.Interfaces;

public interface IDiscountRepository : IRepositoryBase<DiscountCode>
{
    Task<DiscountCode?> GetByCodeAsync(string code);
}
