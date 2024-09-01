using Mango.Services.EmailAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.EmailAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) // Tools->Nuget Package Manager-> console -> add-migration Initial, update db
        {
            
        }

        public DbSet<EmailLogger> EmailLoggers { get; set; }

    }
}
