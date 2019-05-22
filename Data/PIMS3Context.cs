using Microsoft.EntityFrameworkCore;
using PIMS3.Data.Entities;


namespace PIMS3.Data
{
    // DbContext - class executing queries against a datastore. Also is NOT tied to just SQL Server,
    //             but can be associated with multiple kinds of datastores!

    public class PIMS3Context : DbContext
    {

        // Pass startup.cs db connection options.
        public PIMS3Context(DbContextOptions<PIMS3Context> options) : base(options)
        {
        }

        public DbSet<Income> Income { get; set; }
        public DbSet<Profile> Profile { get; set; }
        public DbSet<Asset> Asset { get; set; }
        public DbSet<Investor> Investor { get; set; }
        public DbSet<Position> Position { get; set; }
        public DbSet<AccountType> AccountType { get; set; }
        public DbSet<AssetClass> AssetClass { get; set; }


        // Note: May not need to include child DbSets (of parent-child relations) if we're not DIRECTLY
        //       quering against the child table(s). Otherwise, using parent entity for DbSet should suffice.


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Note: Method is run everytime PIMS3Context is instantiated!

            // Example usage:
            //modelBuilder.Entity<RecordOfSale>()
            //    .HasOne(s => s.Car)
            //    .WithMany(c => c.SaleHistory)
            //    .HasForeignKey(s => s.CarLicensePlate)
            //    .HasPrincipalKey(c => c.LicensePlate);

            // For simple lookups, or involving relatively static data, using
            //  ** modelBuilder.Entity<AssetClass>().HasData(new AssetClass{}) ** is a good way of
            //     seeding one or more table(s).




            modelBuilder.Entity<AssetClass>()
                .Property(ac => ac.AssetId)
                .IsRequired(false); // nullable


            // Create *non-unique* indexes on FKs.
            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.ProfileId)
                .IsUnique(false);
            modelBuilder.Entity<Position>()
               .HasIndex(p => p.AccountTypeId)
               .IsUnique(false);
            modelBuilder.Entity<Position>()
               .HasIndex(p => p.AssetId)
               .IsUnique(false);
            modelBuilder.Entity<Income>()
               .HasIndex(i => i.PositionId)
               .IsUnique(false);
            modelBuilder.Entity<AssetClass>()
              .HasIndex(ac => ac.AssetId)
              .IsUnique(false);
           


            // Define necessary mappings for unconventional Db types, to avoid EF Core model validation errors.
            modelBuilder.Entity<Income>()
                .Property(i => i.AmountRecvd).HasColumnType("decimal");
            modelBuilder.Entity<Position>()
                .Property(p => p.Fees).HasColumnType("decimal");
            modelBuilder.Entity<Position>()
                .Property(p => p.Quantity).HasColumnType("integer");
            modelBuilder.Entity<Position>()
                .Property(p => p.UnitCost).HasColumnType("decimal");
            modelBuilder.Entity<Profile>()
                .Property(pr => pr.DividendRate).HasColumnType("decimal");
            modelBuilder.Entity<Profile>()
                .Property(pr => pr.DividendYield).HasColumnType("decimal");
            modelBuilder.Entity<Profile>()
                .Property(pr => pr.EarningsPerShare).HasColumnType("decimal");
            modelBuilder.Entity<Profile>()
                .Property(pr => pr.PERatio).HasColumnType("decimal");
            modelBuilder.Entity<Profile>()
                .Property(pr => pr.UnitPrice).HasColumnType("decimal");



            // Necessary fluent API configurations to establish dependent entity in 1:1 setup.
            modelBuilder.Entity<Asset>()
                .HasOne(p => p.AssetClass)
                .WithOne(ac => ac.Asset)
                .HasForeignKey<AssetClass>(ac => ac.AssetId);
            modelBuilder.Entity<Profile>()
                       .HasOne(p => p.Asset)
                       .WithOne(x => x.Profile)
                       .HasForeignKey<Asset>(a => a.ProfileId);


            // Required fluent API configuration for M:M Asset-Investor setup.
            modelBuilder.Entity<AssetInvestor>()
                        .HasKey(ai => new { ai.AssetId, ai.InvestorId }); 
            modelBuilder.Entity<AssetInvestor>()
                        .HasOne(ai => ai.Investor)          // ref navigation prop
                        .WithMany(i => i.AssetInvestors)    // many side via collection ref navigation prop
                        .HasForeignKey(ai => ai.AssetId);
            modelBuilder.Entity<AssetInvestor>()
                        .HasOne(ai => ai.Asset)
                        .WithMany(a => a.AssetInvestors)
                        .HasForeignKey(ai => ai.InvestorId);

        }


        // Note: May not need to include child DbSets (of parent-child relations) if we're not DIRECTLY
        //       quering against the child table(s). Otherwise, using parent entity for DbSet should suffice.


        public DbSet<PIMS3.Data.Entities.AssetInvestor> AssetInvestor { get; set; }


    }
}
