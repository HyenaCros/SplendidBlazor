using Microsoft.EntityFrameworkCore;
using Shared;

namespace Api
{
    public class SplendidContext : DbContext
    {
        public DbSet<RazorFile> RazorFiles { get; set; }
        public SplendidContext(DbContextOptions<SplendidContext> options) : base(options)
        {
            
        }
    }
}