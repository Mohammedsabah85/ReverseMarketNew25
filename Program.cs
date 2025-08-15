using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Services;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add Controllers and Views
builder.Services.AddControllersWithViews();

// Add Services
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsAppSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<WhatsAppService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Configure routes
app.MapControllerRoute(
    name: "admin_areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "admin_subcategory1",
    pattern: "Admin/Categories/CreateSubCategory1/{categoryId:int}",
    defaults: new { area = "Admin", controller = "Categories", action = "CreateSubCategory1" });

app.MapControllerRoute(
    name: "admin_subcategory2",
    pattern: "Admin/Categories/CreateSubCategory2/{subCategory1Id:int}",
    defaults: new { area = "Admin", controller = "Categories", action = "CreateSubCategory2" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();