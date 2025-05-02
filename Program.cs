using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LeafLoop.Data;
using LeafLoop.Middleware;
using LeafLoop.Models;
using LeafLoop.Models.API;
using LeafLoop.Repositories;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services;
using LeafLoop.Services.Interfaces;
using LeafLoop.Services.Mappings;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NWebsec.AspNetCore.Middleware;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Configure DbContext
builder.Services.AddDbContext<LeafLoopDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Unit of Work and Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
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

// Add memory cache
builder.Services.AddMemoryCache();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// --- Authentication & Authorization Configuration ---

// 1. Configure Identity
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
})
.AddEntityFrameworkStores<LeafLoopDbContext>()
.AddDefaultTokenProviders();

// 2. Configure Authentication Schemes (Cookie for MVC, JWT for API)
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // Add this line
    })
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
            Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
    };
});

// 3. Configure Application Cookie Options
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;

    // --- Corrected API Redirect Handling ---
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context => HandleApiRedirect(context, StatusCodes.Status401Unauthorized, "Authentication required."),
        OnRedirectToAccessDenied = context => HandleApiRedirect(context, StatusCodes.Status403Forbidden, "Access denied. You do not have permission.")
    };
});

// --- Koniec konfiguracji Authentication & Authorization ---


// Add Swagger/OpenAPI support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LeafLoop API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
     });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2", Name = "Bearer", In = ParameterLocation.Header,
            },
            new List<string>()
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
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LeafLoop API v1"));
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseEnhancedErrorHandling();
app.UseHttpsRedirection();

// Konfiguracja plików statycznych
var provider = new FileExtensionContentTypeProvider();
if (!provider.Mappings.ContainsKey(".css")) provider.Mappings.Add(".css", "text/css");
if (!provider.Mappings.ContainsKey(".webp")) provider.Mappings.Add(".webp", "image/webp");
if (!provider.Mappings.ContainsKey(".woff2")) provider.Mappings.Add(".woff2", "font/woff2");
app.UseStaticFiles(new StaticFileOptions {
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx => {
        var maxAge = TimeSpan.FromDays(7);
        if (ctx.File.Name.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            ctx.File.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        {
            maxAge = TimeSpan.FromHours(1);
        }
        ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={maxAge.TotalSeconds}");
        ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
     }
});

// Security middleware NWebsec
app.UseXContentTypeOptions();
app.UseReferrerPolicy(options => options.NoReferrer());
app.UseXXssProtection(options => options.EnabledWithBlockMode());
app.UseXfo(options => options.Deny());
app.UseCsp(options => options /* ... konfiguracja CSP ... */
    .DefaultSources(s => s.Self())
    .ScriptSources(s => s.Self()
        .CustomSources("https://cdn.jsdelivr.net", "https://cdnjs.cloudflare.com", "https://unpkg.com")
        .UnsafeInline())
    .StyleSources(s => s.Self()
        .CustomSources("https://cdn.jsdelivr.net", "https://cdnjs.cloudflare.com")
        .UnsafeInline())
    .ImageSources(s => s.Self().CustomSources("data:", "blob:"))
    .FontSources(s => s.Self().CustomSources("https://cdn.jsdelivr.net", "https://cdnjs.cloudflare.com"))
    .FormActions(s => s.Self())
    .FrameAncestors(s => s.None())
    .ObjectSources(s => s.None())
    .ConnectSources(s => s.Self())
    .UpgradeInsecureRequests()
);

// Poprawna kolejność middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Mapowanie endpointów
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed data
using (var scope = app.Services.CreateScope())
{
    // ... (kod seed data bez zmian, zakładając, że używa UserManager.CreateAsync) ...
     var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        // Sprawdź i dodaj kategorie
        if (!await unitOfWork.Categories.ExistsAsync(c => true))
        {
            logger.LogInformation("Seeding initial categories...");
            await unitOfWork.Categories.AddAsync(new Category { Name = "Elektronika", Description = "Urządzenia elektroniczne", IconPath = "/img/categories/electronics.png" });
            await unitOfWork.Categories.AddAsync(new Category { Name = "Ubrania", Description = "Odzież i dodatki", IconPath = "/img/categories/clothes.png" });
            await unitOfWork.CompleteAsync();
            logger.LogInformation("Initial categories seeded.");
        }

        // Sprawdź i dodaj użytkownika testowego
        var testUserEmail = "test@example.com";
        if (await userManager.FindByEmailAsync(testUserEmail) == null)
        {
             logger.LogInformation("Seeding test user...");
             var testUser = new User
             {
                 UserName = testUserEmail, Email = testUserEmail, FirstName = "Jan", LastName = "Testowy",
                 CreatedDate = DateTime.UtcNow, LastActivity = DateTime.UtcNow, IsActive = true, EcoScore = 100,
                 EmailConfirmed = true
             };
             var result = await userManager.CreateAsync(testUser, "Password123!");
             if (result.Succeeded) logger.LogInformation("Test user seeded successfully.");
             else logger.LogError("Failed to seed test user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database seeding.");
    }
}

app.Run();

// --- Funkcja pomocnicza do obsługi przekierowań dla API ---
static Task HandleApiRedirect(RedirectContext<CookieAuthenticationOptions> context, int statusCode, string message)
{
    // Sprawdzaj TYLKO ścieżkę żądania
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var response = ApiResponse.ErrorResponse(message);
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        // === ZMIANA: Zamiast HandleResponse(), po prostu piszemy do odpowiedzi ===
        // Zapisz odpowiedź JSON
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        // Nie wywołujemy context.Response.Redirect() ani context.HandleResponse()
        // Ustawienie StatusCode i zapisanie odpowiedzi powinno wystarczyć, aby middleware
        // nie wykonało domyślnego przekierowania.
    }

    // Dla żądań innych niż /api, pozwól na domyślne przekierowanie
    // Nie robimy nic, middleware wykona domyślną logikę (context.Response.Redirect)
    return Task.CompletedTask;
}
