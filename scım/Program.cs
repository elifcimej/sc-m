
using Microsoft.EntityFrameworkCore;
using scım.Data;
using scım.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add SCIM services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IScimService, ScimService>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection(); // Development'ta HTTPS'i devre dışı bırak
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SCIM endpoints
app.MapControllerRoute(
    name: "scim",
    pattern: "scim/v2/{controller}/{action=Index}/{id?}");

app.Run("http://localhost:5000");
