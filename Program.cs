using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Extensions;
using ReverseMarket.Models;

using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add application services
builder.Services.AddApplicationServices(builder.Configuration);

// Add memory cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

// Admin area routing
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    // Seed initial data
    await SeedDataAsync(context);
}

app.Run();

// Seed method
static async Task SeedDataAsync(ApplicationDbContext context)
{
    // Seed categories if they don't exist
    if (!context.Categories.Any())
    {
        var categories = new List<Category>
        {
            new Category { Name = "���������", Description = "����� ������ ��������" },
            new Category { Name = "������", Description = "����� ��������� ������" },
            new Category { Name = "������", Description = "��� ���� ���������� ��������" },
            new Category { Name = "����������", Description = "����� ���� ������" },
            new Category { Name = "�����", Description = "����� ����������" },
            new Category { Name = "�����", Description = "����� ������ ������" },
            new Category { Name = "���", Description = "��� ������ �����" },
            new Category { Name = "�����", Description = "����� ������ �����" }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();

        // Add some subcategories
        var subcategories1 = new List<SubCategory1>
        {
            new SubCategory1 { Name = "����� ������", CategoryId = 1 },
            new SubCategory1 { Name = "�����", CategoryId = 1 },
            new SubCategory1 { Name = "����", CategoryId = 2 },
            new SubCategory1 { Name = "����� ����", CategoryId = 2 },
            new SubCategory1 { Name = "��� ����", CategoryId = 3 },
            new SubCategory1 { Name = "������", CategoryId = 3 }
        };

        context.SubCategories1.AddRange(subcategories1);
        await context.SaveChangesAsync();
    }

    // Seed site settings if they don't exist
    if (!context.SiteSettings.Any())
    {
        var settings = new SiteSettings
        {
            AboutUs = "����� ������ �� ���� ������ ���� ��� �������� ��������� ������ ����� ������. ��� ���� ��� ������ ��� �� ���� ����� ������� ������.",
            ContactPhone = "+964 770 123 4567",
            ContactWhatsApp = "+964 770 123 4567",
            ContactEmail = "info@reversemarket.iq",
            PrivacyPolicy = "����� �������� ������ �������...",
            TermsOfUse = "���� ���������...",
            CopyrightInfo = "���� ������ ������ � 2024 ����� ������"
        };

        context.SiteSettings.Add(settings);
        await context.SaveChangesAsync();
    }
}