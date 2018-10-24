using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using PIMS3.Data.Entities;


namespace PIMS3.Data
{
    public class PIMS3Seeder
    {
        private readonly PIMS3Context _ctx;
        private readonly IHostingEnvironment _hosting;

        public PIMS3Seeder(PIMS3Context ctx, IHostingEnvironment hosting)
        {
            _ctx = ctx;
            _hosting = hosting;
        }


        public void Seed()
        {
            // Create Db only if non-existent.
            _ctx.Database.EnsureCreated();

            // ** Note Seed order. **
            if (!_ctx.AccountType.Any())
            {
                var rootPath = Path.Combine(_hosting.ContentRootPath, @"Data\Seed_Data\VAIO-SeedData\AccountType.json");
                string contents = File.ReadAllText(rootPath);
                var accountTypes = JsonConvert.DeserializeObject<IEnumerable<AccountType>>(contents);

                // Add collection to context.
                _ctx.AddRange(accountTypes);
                // Persist to Db.
                _ctx.SaveChanges();
            }


            if (!_ctx.Profile.Any())
            {
                var rootPath = Path.Combine(_hosting.ContentRootPath, @"Data\Seed_Data\VAIO-SeedData\Profile.json");
                string contents = File.ReadAllText(rootPath);
                var profiles = JsonConvert.DeserializeObject<IEnumerable<Profile>>(contents);

                _ctx.AddRange(profiles);
                _ctx.SaveChanges();
            }
            
                       
            if (!_ctx.Investor.Any())
            {
                var rootPath = Path.Combine(_hosting.ContentRootPath, @"Data\Seed_Data\VAIO-SeedData\Investor.json");
                string contents = File.ReadAllText(rootPath);
                var investors = JsonConvert.DeserializeObject<IEnumerable<Investor>>(contents);

                _ctx.AddRange(investors);
                _ctx.SaveChanges();
            }


            if (!_ctx.Asset.Any())
            {
                var rootPath = Path.Combine(_hosting.ContentRootPath, @"Data\Seed_Data\VAIO-SeedData\Asset.json");
                string contents = File.ReadAllText(rootPath);
                var assets = JsonConvert.DeserializeObject<IEnumerable<Asset>>(contents);

                _ctx.AddRange(assets);
                _ctx.SaveChanges();
            }


            if (!_ctx.AssetClass.Any())
            {
                var rootPath = Path.Combine(_hosting.ContentRootPath, @"Data\Seed_Data\VAIO-SeedData\AssetClass.json");
                string contents = File.ReadAllText(rootPath);
                var assetClasses = JsonConvert.DeserializeObject<IEnumerable<AssetClass>>(contents);

                _ctx.AddRange(assetClasses);
                _ctx.SaveChanges();
            }

            
            if (!_ctx.Position.Any())
            {
                var rootPath = Path.Combine(_hosting.ContentRootPath, @"Data\Seed_Data\VAIO-SeedData\Position.json");
                string contents = File.ReadAllText(rootPath);
                var positions = JsonConvert.DeserializeObject<IEnumerable<Position>>(contents);

               

                _ctx.AddRange(positions);
                _ctx.SaveChanges();
            }


            if (!_ctx.Income.Any()) // Comment If statement to add latest data.
            {
                var rootPath = Path.Combine(_hosting.ContentRootPath, @"Data\Seed_Data\VAIO-SeedData\Income.json");
                string contents = File.ReadAllText(rootPath);
                var revenue = JsonConvert.DeserializeObject<IEnumerable<Income>>(contents);

                //var id = string.Empty;
                //List<string> ids = new List<string>();
                //foreach (var item in revenue)
                //{
                //    ids.Add(item.PositionId);
                //}

                //ids.Sort();
                //var distinctIds = ids.Distinct().ToList();
                
                _ctx.AddRange(revenue);
                _ctx.SaveChanges();
            }

            
        }


    }
}
