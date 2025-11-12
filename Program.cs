using ST10439052_CLDV_POE.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Azure Storage Service
builder.Services.AddSingleton<IAzureStorageService, AzureStorageService>();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
