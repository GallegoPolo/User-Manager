using System;
using System.Collections.Generic;
using System.Text;

namespace AuthService.Domain.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
