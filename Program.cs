using System.Text;
using LeafLoop.Data;
using LeafLoop.Middleware; // Add this
using LeafLoop.Models;
using LeafLoop.Repositories;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services;
using LeafLoop.Services.Interfaces;
using LeafLoop.Services.Mappings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Add this
using NWebsec.AspNetCore.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => // Add JSON options for consistency
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

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

// Add memory cache for better performance
builder.Services.AddMemoryCache();

// Add AutoMapper with our MappingProfile
builder.Services.AddAutoMapper(typeof(MappingProfile));
// --- POCZĄTEK POPRAWNEJ KONFIGURACJI ---

// 1. Skonfiguruj Identity (TYLKO RAZ!)
// To rejestruje usługi Identity ORAZ domyślne schematy uwierzytelniania (w tym ciasteczko)
builder.Services.AddIdentity<User, IdentityRole<int>>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;

    options.User.RequireUniqueEmail = true;

    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<LeafLoopDbContext>()
.AddDefaultTokenProviders();

// 2. Skonfiguruj Uwierzytelnianie - ustaw domyślny schemat i dodaj obsługę JWT
builder.Services.AddAuthentication(options =>
{
    // Ustaw domyślne schematy na ciasteczko zarejestrowane przez AddIdentity
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
    options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
     // DefaultSignInScheme często jest potrzebny dla operacji Identity jak ExternalLogin
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme; // Może być potrzebne przy logowaniu zewn.
})
// Dodaj obsługę JWT jako DODATKOWY schemat (nie domyślny)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]))
    };
});
// UWAGA: NIE wywołujemy tutaj .AddCookie(IdentityConstants.ApplicationScheme, ...)
// AddIdentity już to zrobiło.

// 3. Skonfiguruj OPCJE dla ciasteczek zarejestrowanych przez AddIdentity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true; // Zabezpieczenie przed XSS
    // Upewnij się, że polityka Secure jest zgodna z Twoim środowiskiem (HTTPS)
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Zalecane dla HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax; // Dobra równowaga między bezpieczeństwem a wygodą
    // options.ExpireTimeSpan = TimeSpan.FromDays(14); // Opcjonalnie: czas życia ciasteczka
});

// Skonfiguruj też ciasteczko zewnętrzne (jeśli planujesz np. logowanie Google/Facebook)
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Zalecane dla HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// --- KONIEC POPRAWNEJ KONFIGURACJI ---
// Add Swagger/OpenAPI support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LeafLoop API", Version = "v1" });
    
    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Register application services
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

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Enable Swagger in development
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LeafLoop API v1"));
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Add API exception handling middleware
app.UseEnhancedErrorHandling();

app.UseHttpsRedirection();
app.UseStaticFiles();

// Security middleware
app.UseHsts(options => options.MaxAge(days: 365).IncludeSubdomains());
app.UseXContentTypeOptions();
app.UseReferrerPolicy(options => options.NoReferrer());
app.UseXXssProtection(options => options.EnabledWithBlockMode());
app.UseXfo(options => options.Deny());
// In Program.cs, update the CSP configuration
app.UseCsp(options => options
    .DefaultSources(s => s.Self())
    .ScriptSources(s => s.Self()
        .CustomSources(
            "https://cdn.jsdelivr.net",
            "https://cdnjs.cloudflare.com",
            "https://unpkg.com"
        )
        .UnsafeInline()
    )
    .StyleSources(s => s.Self()
        .CustomSources("https://cdn.jsdelivr.net", "https://cdnjs.cloudflare.com")
        .UnsafeInline()
    )
    .ImageSources(s => s.Self()
        .CustomSources("data:", "blob:")
    )
    .FontSources(s => s.Self()
        .CustomSources("https://cdn.jsdelivr.net", "https://cdnjs.cloudflare.com")
    )
    .FormActions(s => s.Self())
    .FrameAncestors(s => s.None())
    .ObjectSources(s => s.None())
    .ConnectSources(s => s.Self()
        // Add your API domain here
        .CustomSources("https://zak7be8sse.execute-api.eu-central-1.amazonaws.com")
    )
    .UpgradeInsecureRequests()
);

app.UseRouting();
app.UseAuthentication();
app.UseCookiePolicy();
app.UseAuthorization();

// Map routes
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
        // Check if database is empty
        if (!await unitOfWork.Categories.ExistsAsync(c => true))
        {
            // Add some categories
            await unitOfWork.Categories.AddAsync(new Category 
            { 
                Name = "Elektronika", 
                Description = "Urządzenia elektroniczne",
                IconPath = "/img/categories/electronics.png"
            });
            
            await unitOfWork.Categories.AddAsync(new Category 
            { 
                Name = "Ubrania", 
                Description = "Odzież i dodatki",
                IconPath = "/img/categories/clothes.png"
            });
            
            // Add test user
            await unitOfWork.Users.AddAsync(new User
            {
                Email = "test@example.com",
                PasswordHash = "hash123", // In production use proper hashing
                FirstName = "Jan",
                LastName = "Testowy",
                CreatedDate = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IsActive = true,
                EcoScore = 100
            });
            
            await unitOfWork.CompleteAsync();
            
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Added initial data to database");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during database initialization");
    }
}

var provider = new FileExtensionContentTypeProvider();
// Ensure CSS files have the correct MIME type
if (!provider.Mappings.ContainsKey(".css"))
    provider.Mappings.Add(".css", "text/css");
// Add any other MIME types that might be missing
if (!provider.Mappings.ContainsKey(".webp"))
    provider.Mappings.Add(".webp", "image/webp");
if (!provider.Mappings.ContainsKey(".woff2"))
    provider.Mappings.Add(".woff2", "font/woff2");

// Configure static file middleware with proper MIME types
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx =>
    {
        // Set cache control headers for better performance
        var maxAge = ctx.File.Name.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ? 
            TimeSpan.FromHours(1) : TimeSpan.FromDays(7);
            
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={maxAge.TotalSeconds}");
            
        // Add security headers
        ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    }
});

app.Run();