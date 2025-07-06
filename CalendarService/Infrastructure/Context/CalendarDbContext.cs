using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Context;


public class CalendarDbContext : DbContext
{
    public CalendarDbContext(DbContextOptions<CalendarDbContext> options) : base(options) { }

    public DbSet<CalendarEvents> CalendarEvents => Set<CalendarEvents>();
}
