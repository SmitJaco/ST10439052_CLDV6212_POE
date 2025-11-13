using ST10439052_CLDV_POE.Services;
using ST10439052_CLDV_POE.Data;
using Microsoft.Extensions.Azure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Entity Framework Core for Azure SQL Database
var sqlConnectionString = builder.Configuration.GetConnectionString("AzureSQL") ?? 
    throw new InvalidOperationException("Connection string 'AzureSQL' not found.");
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

// Add Authentication (Cookie-based)
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Login/Login";
        options.AccessDeniedPath = "/Login/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.SlidingExpiration = true;
    });

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(context =>
        context.User.HasClaim(c => c.Type == "Role" && c.Value == "Admin")));
});

// Add Azure Storage Service
builder.Services.AddSingleton<IAzureStorageService, AzureStorageService>();

// Add Authentication Service
builder.Services.AddScoped<IAuthService, AuthService>();

// Add Cart Service
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["StorageConnection:blobServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddQueueServiceClient(builder.Configuration["StorageConnection:queueServiceUri"]!).WithName("StorageConnection");
    clientBuilder.AddTableServiceClient(builder.Configuration["StorageConnection:tableServiceUri"]!).WithName("StorageConnection");
});

var app = builder.Build();

// Force initialize Azure storage (tables/containers/queues) at startup
_ = app.Services.GetRequiredService<IAzureStorageService>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
