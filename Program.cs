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
    options.IdleTimeout = TimeSpan.FromMinutes(60); //  „œÌœ „œ… «·Ã·”… ≈·Ï 60 œﬁÌﬁ…
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "ReverseMarket.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add Controllers and Views
builder.Services.AddControllersWithViews(options =>
{
    // ≈÷«›… ›· —«  ⁄«„…
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

app.UseRouting();
app.UseSession(); // ÌÃ» √‰ ÌﬂÊ‰ ﬁ»· UseAuthorization
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

// Routes ··Õ”«»
app.MapControllerRoute(
    name: "account",
    pattern: "Account/{action=Login}",
    defaults: new { controller = "Account" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ≈‰‘«¡ ﬁ«⁄œ… «·»Ì«‰«  ≈–« ·„  ﬂ‰ „ÊÃÊœ…
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
        logger.LogError(ex, "ÕœÀ Œÿ√ √À‰«¡ ≈‰‘«¡ ﬁ«⁄œ… «·»Ì«‰« ");
    }
}

app.Run();