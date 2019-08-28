using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            InitTestData();
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();


        public static void InitTestData()
        {
            using (var db = new ProductDbContext())
            {
                db.RemoveRange(db.Products);
                db.RemoveRange(db.Categories);
                db.SaveChanges();
                if (db.Products.Any())
                    return;

                Product[] products = Enumerable.Range(1, 100).Select(x =>
                
                    new Product {Id = x, DisplayName = $"Product {x}", CategoryId = x}
                ).ToArray();
                
                db.Products.AddRange(products);

                Category[] categories = Enumerable.Range(1, 100)
                    .Select(x => new Category {Id = x, Name = $"Category {x}", Internal = false}).ToArray();
               
                db.Categories.AddRange(categories);

                db.SaveChanges();
            }
        }
    }
}