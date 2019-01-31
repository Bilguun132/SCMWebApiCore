using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCMWebApiCore.DataProviders;
using SCMWebApiCore.Models;
using System.Data.SqlClient;
using Swashbuckle.AspNetCore.Swagger;

namespace SCMWebApiCore
{
    public class Startup
    {
        private string _connection = null;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IDataProvider, DataProvider>();
            services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowCredentials()
                       .AllowAnyHeader();
            }));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            _connection = Configuration.GetConnectionString("53ConnectionString");
            services.AddDbContext<SCM_GAMEContext>(options => 
            options.UseLazyLoadingProxies()
            .UseSqlServer(_connection));
            services.AddSignalR();
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Version = "v1",
                    Title = "Beer Web Game API",
                    Description = "ASP.NET Core API used for React.JS web app",
                    TermsOfService = "None",
                    Contact = new Contact
                    {
                        Name = "Bilguun Batbold",
                        Email = "isebb@nus.edu.sg"
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
               // app.UseHsts();
            }

            app.UseCors("MyPolicy");

            app.UseSignalR(routes =>
            {
                routes.MapHub<ChatHub>("/chat");
            });

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("swagger/v1/swagger.json", "My API V1");
            });
            app.UseMvc();
        }
    }
}
