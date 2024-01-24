using DataBase;
using DataBase.Repositories.Abstraction;
using DataBase.Repositories.MappingSettings;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataBase.Data;
using DataBase.Repositories;

public class Startup {
    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
        services.AddControllers();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddAutoMapper(typeof(MappingProfile));

        //services.AddSingleton<IProductRepository,ProductRepository>();
        services.AddMemoryCache(options => options.TrackStatistics = true);

        services.AddDbContext<DataContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"));
            options.UseLazyLoadingProxies();
        });

        services.AddScoped<IDbInitializer, EfDbInitializer>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IDbInitializer dbInitializer) {
        if (env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else {
            app.UseHttpsRedirection();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

        using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope()) {
            var dbContext = serviceScope.ServiceProvider.GetService<DataContext>();
            if (!dbContext.Database.CanConnect()) {
                dbContext.Database.EnsureCreated();
            }
            else if (!dbContext.Products.Any() || !dbContext.Storages.Any()) {
                // Handle the case when there are no records in the database
                dbInitializer.InitializeDb();
                Console.WriteLine(
                    "There are no records in the database. Please add some records before running the application.");
            }
        }
    }
}