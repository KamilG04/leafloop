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

builder.Services.AddIdentity<User, IdentityRole<int>>(options => {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
    
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.MaxFailedAccessAttempts = 5;
    
        options.User.RequireUniqueEmail = true;
    
        // Allow redirects during login/logout
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedPhoneNumber = false;
    })
    .AddEntityFrameworkStores<LeafLoopDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
app.UseApiExceptionHandling();

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
// Add MIME type for CSS if not present
if (!provider.Mappings.ContainsKey(".css"))
    provider.Mappings.Add(".css", "text/css");

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
app.Run();