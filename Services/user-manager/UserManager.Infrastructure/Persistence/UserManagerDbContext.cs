using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using UserManager.Domain.Entities;

namespace UserManager.Infrastructure.Persistence
{
    public class UserManagerDbContext : DbContext
    {
        public UserManagerDbContext(DbContextOptions<UserManagerDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
    }
}
