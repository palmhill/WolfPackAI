using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy.Transforms;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient();

// Add YARP services
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddResponseTransform(transformContext =>
        {
            var response = transformContext.HttpContext.Response;

            if (response.StatusCode >= 300 && response.StatusCode < 400)
            {
                if (response.Headers.Location.Any())
                {
                    var location = response.Headers.Location.FirstOrDefault();
                    if (!string.IsNullOrEmpty(location))
                    {
                        // If it's an absolute URL to the backend, rewrite it
                        if (location.StartsWith($"http://localhost:4000/") ||
                            location.StartsWith($"https://localhost:4000/"))
                        {
                            var path = location.Substring(location.IndexOf('/', 8)); // Skip protocol and host
                            if (path.StartsWith("/litellm/litellm"))
                            {
                                path = path.Substring(8);
                            }

                            response.Headers.Location = $"{path}";
                        }
                        // If it's a relative URL, prefix it
                        else if (location.StartsWith("/"))
                        {
                            response.Headers.Location = $"{location}";
                        }
                    }
                }
            }

            return ValueTask.CompletedTask;
        });
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Serve static files from wwwroot
app.UseStaticFiles();

// Add landing page route
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(await File.ReadAllTextAsync("wwwroot/index.html"));
});

// Map the reverse proxy
app.MapReverseProxy();

app.Run();