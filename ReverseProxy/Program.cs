using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add YARP services
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddResponseTransform(async transformContext =>
        {
            var response = transformContext.HttpContext.Response;
            var requestPath = transformContext.HttpContext.Request.Path;
            var requestHost = transformContext.HttpContext.Request.Host.Host;

            // Skip transform for WebSocket responses
            if (transformContext.HttpContext.Request.Headers.Upgrade == "websocket")
            {
                return;
            }

            if (response.StatusCode is >= 300 and < 400 &&
                response.Headers.Location.Count > 0)
            {
                var originalLocation = response.Headers.Location.First();
                if (string.IsNullOrEmpty(originalLocation))
                    return;

                string targetBasePath = requestPath.StartsWithSegments("/litellm") ? "/litellm" :
                                        requestPath.StartsWithSegments("/n8n") ? "/n8n" :
                                        "";

                if (Uri.TryCreate(originalLocation, UriKind.Absolute, out var uri))
                {
                    if (uri.Host == "localhost" || uri.Host == requestHost)
                    {
                        var rewritten = $"{targetBasePath}{uri.PathAndQuery}";
                        response.Headers.Location = rewritten;
                    }
                }
                else if (originalLocation.StartsWith("/"))
                {
                    response.Headers.Location = $"{targetBasePath}{originalLocation}";
                }
            }
        });
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapReverseProxy();

app.Run();