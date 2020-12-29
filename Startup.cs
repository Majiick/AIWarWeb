using System;
using System.Collections.Generic;
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

namespace AIWarWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
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
                    string path = @"e:\tmp\latestMap.txt";
                    await context.Response.WriteAsync(File.ReadAllText(path));
                });
                endpoints.MapPost("/postScript", async context => {
                    string path = @"e:\tmp\scripts\";
                    string scriptJson = "";
                    using (var reader = new StreamReader(context.Request.Body)) {
                        scriptJson = await reader.ReadToEndAsync();
                    }
                    dynamic obj = JsonConvert.DeserializeObject(scriptJson);
                    File.WriteAllText(path + (string)obj.user + ".lua", (string)obj.code);
                    await context.Response.WriteAsync("OK");
                });
                endpoints.MapGet("/getErrors/{playerName}", async context => {
                    string path = @"e:\tmp\errors\" + context.Request.RouteValues["playerName"] + ".txt";
                    if (File.Exists(path)) {
                        await context.Response.WriteAsync(File.ReadAllText(path));
                    } else {
                        await context.Response.WriteAsync("");
                    }
                });
            });
            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                endpoints.MapRazorPages();
            });
        }
    }
}
