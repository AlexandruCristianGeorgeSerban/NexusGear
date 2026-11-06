using NexusGear.Data;
using NexusGear.Models;
using NexusGear.Repositories.Interfaces;

namespace NexusGear.Repositories;

public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }
}

