using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SCMWebApiCore.DataProviders;
using SCMWebApiCore.Models;

namespace SCMWebApiCore
{
    public class Startup
    {
        readonly bool isDebug = false;

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
            var connection = (isDebug ? @"Server=172.19.76.55\SQLEXPRESS;initial catalog=SCM_GAME;persist security info=True;user id=sa;password=ISE_Admin@12345;MultipleActiveResultSets=True" : @"Server=nusisemgameserver.database.windows.net,1433;initial catalog=SCM_GAME;persist security info=True;user id=isemadmin;password=ISE_Admin@12345;MultipleActiveResultSets=True");
           // var connection = @"Server=nusisemgameserver.database.windows.net,1433;initial catalog=SCM_GAME;persist security info=True;user id=isemadmin;password=ISE_Admin@12345;MultipleActiveResultSets=True";
        //   var connection = @"Server=172.19.76.55\SQLEXPRESS;initial catalog=SCM_GAME;persist security info=True;user id=sa;password=ISE_Admin@12345;MultipleActiveResultSets=True";
            services.AddDbContext<SCM_GAMEContext>(options => options.UseSqlServer(connection));
            services.AddSignalR();
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
                app.UseHsts();
            }

            app.UseCors("MyPolicy");

            app.UseSignalR(routes =>
            {
                routes.MapHub<ChatHub>("/chat");
            });
            app.UseMvc();
        }
    }
}
