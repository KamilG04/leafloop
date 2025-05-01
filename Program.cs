using System.Text;
using LeafLoop.Data;
using LeafLoop.Models;
using LeafLoop.Repositories;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services;
using LeafLoop.Services.Interfaces;
using LeafLoop.Services.Mappings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NWebsec.AspNetCore.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure DbContext
builder.Services.AddDbContext<LeafLoopDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Unit of Work and Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register specific repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();
builder.Services.AddScoped<IPhotoRepository, PhotoRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISavedSearchRepository, SavedSearchRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<IItemService, ItemService>();
// In Program.cs
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
// Add memory cache for better performance
builder.Services.AddMemoryCache();

// Add AutoMapper with our MappingProfile
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddIdentity<User, IdentityRole<int>>(options => {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
    
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.MaxFailedAccessAttempts = 5;
    
        options.User.RequireUniqueEmail = true;
    
        // Dodaj to, aby zezwolić na przekierowania podczas logowania/wylogowania
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<LeafLoopDbContext>()
    .AddDefaultTokenProviders();

// Register application services - THESE MUST BE BEFORE app.Build()
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IPhotoService, PhotoService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();

// NOW build the app (no more service registrations after this line)
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Security middleware
app.UseHsts(options => options.MaxAge(days: 365).IncludeSubdomains());
app.UseXContentTypeOptions();
app.UseReferrerPolicy(options => options.NoReferrer());
app.UseXXssProtection(options => options.EnabledWithBlockMode());
app.UseXfo(options => options.Deny());
app.UseCsp(options => options
    .DefaultSources(s => s.Self()) // Ogólne źródło
    .ScriptSources(s => s.Self()   // Skrypty
            .CustomSources(
                "https://cdn.jsdelivr.net",
                "https://cdnjs.cloudflare.com",
                "https://unpkg.com" // Jeśli nadal używasz CDN
            )
            .UnsafeInline() // Uważaj z tym
    )
    .StyleSources(s => s.Self().CustomSources("https://cdn.jsdelivr.net", "https://cdnjs.cloudflare.com").UnsafeInline())
    // --- ZMODYFIKOWANA DYREKTYWA ImageSources ---
    .ImageSources(s => s.Self()           // Pozwól na obrazki z własnej domeny
            .CustomSources("data:", // Pozwól na dane base64 (np. inline SVG)
                "blob:")  // <<<--- DODAJ TO dla podglądu zdjęć
        // Możesz tu dodać inne zaufane domeny, skąd pochodzą obrazki, np. CDN
        // "https://twoj-cdn.com"
    )
    // --- KONIEC MODYFIKACJI ImageSources ---
    .FontSources(s => s.Self().CustomSources("https://cdn.jsdelivr.net", "https://cdnjs.cloudflare.com"))
    .FormActions(s => s.Self())
    .FrameAncestors(s => s.None())
    .ObjectSources(s => s.None())
    // Dodaj ConnectSources, jeśli API Reacta będzie się łączyć z innymi źródłami niż 'self'
    .ConnectSources(s => s.Self()) // Pozwala Reactowi łączyć się z Twoim API (/api/...)
    .UpgradeInsecureRequests()
);
// Ensure UseAuthentication is called before UseAuthorization
app.UseRouting();
app.UseAuthentication();
app.UseCookiePolicy();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "login",
    pattern: "Account/Login",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "register",
    pattern: "Account/Register",
    defaults: new { controller = "Account", action = "Register" });

app.MapControllerRoute(
    name: "logout",
    pattern: "Account/Logout",
    defaults: new { controller = "Account", action = "Logout" });

// Add seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        // Sprawdź, czy baza danych jest pusta
        if (!await unitOfWork.Categories.ExistsAsync(c => true))
        {
            // Dodaj kilka kategorii
            await unitOfWork.Categories.AddAsync(new LeafLoop.Models.Category 
            { 
                Name = "Elektronika", 
                Description = "Urządzenia elektroniczne",
                IconPath = "/img/categories/electronics.png"
            });
            
            await unitOfWork.Categories.AddAsync(new LeafLoop.Models.Category 
            { 
                Name = "Ubrania", 
                Description = "Odzież i dodatki",
                IconPath = "/img/categories/clothes.png"
            });
            
            // Dodaj testowego użytkownika
            await unitOfWork.Users.AddAsync(new LeafLoop.Models.User
            {
                Email = "test@example.com",
                PasswordHash = "hash123", // W produkcji użyj prawdziwego hashowania
                FirstName = "Jan",
                LastName = "Testowy",
                CreatedDate = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IsActive = true,
                EcoScore = 100
            });
            
            await unitOfWork.CompleteAsync();
            
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Dodano początkowe dane do bazy danych.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Wystąpił błąd podczas inicjalizacji bazy danych.");
    }
}

app.Run();