using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Http;
using GraphQL.Server;
using GraphQL.Server.Ui.Playground;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace test
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.TryAddSingleton<IDependencyResolver>(q => new FuncDependencyResolver(q.GetRequiredService));
            services.TryAddSingleton<IDocumentExecuter, DocumentExecuter>();
            services.TryAddSingleton<IDocumentWriter, DocumentWriter>();
            services.TryAddSingleton<IDataLoaderContextAccessor, DataLoaderContextAccessor>();
            services.TryAddSingleton<IDocumentExecutionListener, DataLoaderDocumentListener>();

            services.AddSingleton<IProductRepo, ProductRepo>();

            services.AddSingleton<CategoryType>();
            services.AddSingleton<ProductType>();
            services.AddSingleton<FrontendRoot>();
            services.AddSingleton<ISchema, FrontendSchema>();
            
            services.AddGraphQL(options =>
            {
                options.EnableMetrics = true;
                options.ExposeExceptions = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseDeveloperExceptionPage();

            app.UseGraphQL<ISchema>()
               .UseGraphQLPlayground(new GraphQLPlaygroundOptions());
           
            app.UseHttpsRedirection();
        }
    }
}