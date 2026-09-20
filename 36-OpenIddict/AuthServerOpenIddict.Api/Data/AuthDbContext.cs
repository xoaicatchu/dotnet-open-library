using Microsoft.EntityFrameworkCore;

namespace AuthServerOpenIddict.Api.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }
}
