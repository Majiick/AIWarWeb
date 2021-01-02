using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace AIWarWeb
{
    public class Startup
    {
        public ConnectionMultiplexer redis;

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
            redis = ConnectionMultiplexer.Connect("localhost:6379");
            Debug.Assert(redis.IsConnected);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/latestMap", async context => {
                    string map = redis.GetDatabase().StringGet("latest_map");
                    await context.Response.WriteAsync(map);
                });
                endpoints.MapPost("/postScript", async context => {
                    string scriptJson = "";
                    using (var reader = new StreamReader(context.Request.Body)) {
                        scriptJson = await reader.ReadToEndAsync();
                    }
                    dynamic obj = JsonConvert.DeserializeObject(scriptJson);
                    string redisKey = (string)obj.user + "_script";
                    Debug.Assert(redis.GetDatabase().StringSet(redisKey, (string)obj.code));
                    await context.Response.WriteAsync("OK");
                });
                endpoints.MapGet("/getSTDOUT/{playerName}", async context => {
                    string key = context.Request.RouteValues["playerName"] + "_stdout";
                    string err = redis.GetDatabase().StringGet(key);
                    if (err == null) {
                        err = "";
                    }
                    await context.Response.WriteAsync(err);
                });
                endpoints.MapPost("/postOneOff", async context => {
                    string scriptJson = "";
                    using (var reader = new StreamReader(context.Request.Body)) {
                        scriptJson = await reader.ReadToEndAsync();
                    }
                    redis.GetSubscriber().Publish("oneoff", scriptJson);
                    await context.Response.WriteAsync("OK");
                });
            });
            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                endpoints.MapRazorPages();
            });
        }
    }
}
