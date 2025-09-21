using MFAApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace MFAApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserDevice> UserDevices => Set<UserDevice>();
        public DbSet<LoginChallenge> LoginChallenges => Set<LoginChallenge>();
    }
}
