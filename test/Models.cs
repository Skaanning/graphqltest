using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;

namespace test
{
    public static class Ext
    {
        public static async Task<TU> BatchLoader<T, TKey, TU>(this IDataLoaderContextAccessor accessor, ResolveFieldContext<T> context, 
            Func<IEnumerable<TKey>, Task<IDictionary<TKey, TU>>> func, Func<T, TKey> keySelector)
        {
            var loader = accessor.Context.GetOrAddBatchLoader($"{context.FieldName}loader", func);
            var handle = loader.LoadAsync(keySelector(context.Source));
            
            return await handle;
        }
    }
    
    public interface IProductRepo
    {
        Product GetProduct(int id);
        Product[] AllProducts();
        Task<IDictionary<int, Category>> Categories(IEnumerable<int> ids);
        Category GetCategory(int id);
    }

    public class ProductRepo : IProductRepo
    {
        public Product GetProduct(int id)
        {
            Console.WriteLine("GetProduct");
            using (var db = new ProductDbContext())
            {
                return db.Products.FirstOrDefault(x => x.Id == id);
            }
        }

        public Product[] AllProducts()
        {
            Console.WriteLine("AllProducts");

            using (var db = new ProductDbContext())
            {
                return db.Products.ToArray();
            }        
        }

        public Task<IDictionary<int, Category>> Categories(IEnumerable<int> ids)
        {
            Console.WriteLine("Categories");

            using (var db = new ProductDbContext())
            {
                return Task.FromResult((IDictionary<int, Category>)
                    db.Categories.Where(x => ids.Contains(x.Id)).ToDictionary(x => x.Id));
            }
        }

        public Category GetCategory(int id)
        {
            Console.WriteLine("GetCategory");

            using (var db = new ProductDbContext())
            {
                var a = db.Categories.FirstOrDefault(x => x.Id == id);
                return a;
            }        
        }
    }

    public class Category
    {
        [Field("Id of the category")]
        public int Id { get; set; }
        [Field("Name of the category.. Very nice")]
        public string Name { get; set; }
        [Field("Intenral?>!>! wow")]
        public bool Internal { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public int CategoryId { get; set; }
    }

    public abstract class OGT<T> : ObjectGraphType<T> where T : class
    {
        public OGT(string description)
        {
            var type = typeof(T);
            Name = type.Name;
            Description = description;

            foreach (var property in type.GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(FieldAttribute))))
            {
                var fieldAttribute = property.GetCustomAttribute(typeof(FieldAttribute)) as FieldAttribute;
                // ReSharper disable once PossibleNullReferenceException
                Field(property.PropertyType.GetGraphTypeFromType(), property.Name, fieldAttribute.Description);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAttribute : Attribute
    {
        public string Description { get; }

        public FieldAttribute(string description = null)
        {
            Description = description;
        }
    }

    public class CategoryType : OGT<Category>
    {
        public CategoryType() : base("This is a category")
        {
        }
    }
    
    public class ProductType : ObjectGraphType<Product>
    {
        public ProductType(IDataLoaderContextAccessor accessor, IProductRepo productRepo)
        {
            Name = "Product";
            Description = "This is a product";

            Field(p => p.Id);
            Field(p => p.DisplayName);
            Field(p => p.CategoryId);

            Field<CategoryType>().Name("Category")
                .ResolveAsync(async context =>
                    await accessor.BatchLoader(context, productRepo.Categories, x => x.CategoryId)
                );
        }
    }

    public class FrontendRoot : ObjectGraphType<object>
    {
        public FrontendRoot(IProductRepo productRepo)
        {
            Name = "query";
            Field<ProductType>("product", 
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "id", Description = "id of the product" }
                ),
                resolve: context => productRepo.GetProduct(context.GetArgument<int>("id")));

            Field<ListGraphType<ProductType>, Product[]>().Name("all")
                .Resolve(context => productRepo.AllProducts());
        }
    }
    
    public class FrontendSchema : Schema
    {
        public FrontendSchema(IDependencyResolver resolver) : base(resolver)
        {
            Query = resolver.Resolve<FrontendRoot>();
        }
    }
}