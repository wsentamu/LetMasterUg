using LetMasterWebApp.Core;
using LetMasterWebApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LetMasterWebApp.DataLayer;
public class ApplicationDbContext:IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
    public DbSet<Property> Properties { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantUnit> TenantUnits { get; set; }
    public DbSet<ExpenseType> ExpenseTypes { get; set; }
    public DbSet<TenantUnitTransaction> TenantUnitTransactions { get; set; }
    public DbSet<PropertyType> PropertyTypes { get; set; }
    public DbSet<UnitType> UnitTypes { get; set; }
    public DbSet<MaintenaceRequest> MaintenaceRequests { get; set; }
    public DbSet<UserMessage> UserMessages { get; set; }
    public DbSet<AuditTrail> AuditTrails { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<ClientDebitRequest>ClientDebitRequests { get; set; }
    public DbSet<ScheduledJob> ScheduledJobs { get; set; }
    //automate created and update date entity columns
    public override int SaveChanges()
    {
        var entries = ChangeTracker.Entries().Where(e=>e.Entity is BaseModel && (e.State==EntityState.Added||e.State==EntityState.Modified));
        foreach(var entry in entries)
        {
            ((BaseModel)entry.Entity).UpdatedDate = DateTime.UtcNow;
            if(entry.State==EntityState.Added)
                ((BaseModel)entry.Entity).CreatedDate = DateTime.UtcNow;
        }
        return base.SaveChanges();
    }
}
