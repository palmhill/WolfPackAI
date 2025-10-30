var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient();


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

app.Run();