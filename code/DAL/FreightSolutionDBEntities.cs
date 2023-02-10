using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public partial class FreightSolutionDBEntities : IdentityDbContext<ApplicationUser, AspNetRoles, string, AspNetUserClaims, AspNetUserRoles, AspNetUserLogins, AspNetRolesClaim, IdentityUserToken<string>>, IFreightSolutionDBEntities, IFreightSolutionDBEntities2
    {
        public FreightSolutionDBEntities() : base() { }

        public virtual DbSet<InvoiceAdditionalCostsTemp> InvoiceAdditionalCostsTemp { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Settings for Invoice.AdditionalCostsTemp table - for faster statistics generation
            modelBuilder.Entity<InvoiceAdditionalCostsTemp>(e =>
            {
                e.ToTable("AdditionalCostsTemp", schema: "Invoice");
                e.HasNoKey();

                e.Property(e => e.CarrierId).HasColumnName("fk_Carrierid");
                e.Property(e => e.AdditionalCostId).HasColumnName("fk_AdditionalCostid");
                e.Property(e => e.InvoiceLineId).HasColumnName("fk_InvoiceLineid");
                e.Property(e => e.ServiceId).HasColumnName("fk_Serviceid");
                e.Property(e => e.ProductId).HasColumnName("fk_Productid");
                e.Property(e => e.SenderCountryId).HasColumnName("fk_SenderCountryid");
                e.Property(e => e.ReceiverCountryId).HasColumnName("fk_ReceiverCountryid");

                e.Property(e => e.AdditionalCostPrice).HasColumnType("decimal(16,6)");
                e.Property(e => e.FreightPrice).HasColumnType("decimal(16,6)");
                e.Property(e => e.TotalPrice).HasColumnType("decimal(16,6)");
            });
        }
    }
}