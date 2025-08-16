using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Services;
using ReverseMarket.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session with enhanced configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // ����� ��� ������ ��� 60 �����
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "ReverseMarket.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add Controllers and Views
builder.Services.AddControllersWithViews(options =>
{
    // ����� ������ ����
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

// Add Services
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsAppSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<WhatsAppService>();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add Logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


// ����� ��� ����� �������� ��������
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/uploads",
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    ServeUnknownFileTypes = false,
    DefaultContentType = "application/octet-stream"
});
app.UseRouting();
app.UseSession(); // ��� �� ���� ��� UseAuthorization
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

// Routes ������
app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}",
    defaults: new { controller = "Account" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
CreateUploadDirectories(app.Environment.WebRootPath);

// ����� ����� �������� ��� �� ��� ������
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "��� ��� ����� ����� ����� ��������");
    }
}

app.Run();
void CreateUploadDirectories(string webRootPath)
{
    var uploadPaths = new[]
    {
        Path.Combine(webRootPath, "uploads"),
        Path.Combine(webRootPath, "uploads", "requests"),
        Path.Combine(webRootPath, "uploads", "profiles"),
        Path.Combine(webRootPath, "uploads", "advertisements"),
        Path.Combine(webRootPath, "uploads", "site"),
        Path.Combine(webRootPath, "logs")
    };

    foreach (var path in uploadPaths)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Console.WriteLine($"Created directory: {path}");
        }
    }
}