using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RoyalPalace.Data;
using RoyalPalace.Models;
using RoyalPalace.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================================
// 1. DATABASE CONTEXT
// =========================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =========================================
// 2. IDENTITY (Authentication & Authorization)
// =========================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// =========================================
// 3. SESSION CONFIGURATION
// =========================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =========================================
// 4. CONTROLLERS & VIEWS
// =========================================
builder.Services.AddControllersWithViews();

// =========================================
// 5. CUSTOM SERVICES (Dependency Injection)
// =========================================
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();

// =========================================
// 6. BUILD APP
// =========================================
var app = builder.Build();

// =========================================
// 7. DATABASE INITIALIZATION
// =========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Apply pending migrations automatically
        await context.Database.MigrateAsync();
        // Seed default data (Admin user, Room Categories, Sample Rooms)
        await DbInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database initialization.");
    }
}
// =========================================
// 8. MIDDLEWARE PIPELINE
// =========================================

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY"); // Prevent Clickjacking
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff"); // Prevent MIME sniffing
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// Error handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    // Custom 404 handling for production
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// =========================================
// 9. ENDPOINTS & ROUTES
// =========================================

// Admin Area Route
app.MapControllerRoute(
    name: "AdminArea",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default Public Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// API Route for Chatbot
app.MapControllers();

// =========================================
// 10. RUN APP
// =========================================
app.Run();