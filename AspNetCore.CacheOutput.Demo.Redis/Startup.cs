using AspNetCore.CacheOutput.Extensions;
using AspNetCore.CacheOutput.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AspNetCore.CacheOutput.Demo.Redis
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
            services.AddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
            services.AddSingleton<IApiOutputCache, StackExchangeRedisOutputCacheProvider>();
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(Configuration.GetConnectionString("RedisCache")));
            services.AddTransient<IDatabase>(e => e.GetRequiredService<IConnectionMultiplexer>().GetDatabase(-1, null));


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
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

            app.UseHttpsRedirection();

            // Put app.UseCacheOutput() before app.UseMvc()
            app.UseCacheOutput();

            app.UseMvc();
        }
    }
}
