using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults (health checks, OpenTelemetry, service discovery)
builder.AddServiceDefaults();

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        // Add path prefix removal transforms
        builderContext.AddPathRemovePrefix("/chat");
        builderContext.AddPathRemovePrefix("/litellm");
        builderContext.AddPathRemovePrefix("/n8n");

        // Add response header transforms for proper redirects
        builderContext.AddResponseTransform(transformContext =>
        {
            if (transformContext.ProxyResponse?.Headers.Location != null)
            {
                var location = transformContext.ProxyResponse.Headers.Location;
                // Rewrite location headers to maintain correct routing
                if (location.IsAbsoluteUri && location.PathAndQuery.StartsWith("/"))
                {
                    var path = location.PathAndQuery;
                    transformContext.HttpContext.Response.Headers.Location = path;
                }
            }
            return ValueTask.CompletedTask;
        });
    });

// Add HTTP client for health checks
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Map service defaults endpoints (/health, /alive)
app.MapDefaultEndpoints();

// Serve static files from wwwroot
app.UseStaticFiles();

// Add landing page route
app.MapGet("/", async context =>
{
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync(await File.ReadAllTextAsync("wwwroot/index.html"));
});

// Enable YARP reverse proxy
app.MapReverseProxy();

app.Run();