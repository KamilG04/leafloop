using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LeafLoop.Data;
using LeafLoop.Middleware;
using LeafLoop.Models;
using LeafLoop.Models.API; // For ApiResponse used in HandleApiRedirect
using LeafLoop.Repositories;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services;
using LeafLoop.Services.Interfaces;
using LeafLoop.Services.Mappings; // For AutoMapper MappingProfile
using Microsoft.AspNetCore.Authentication; 
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore; // For DbContext and EF Core extension methods
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// FIXME: IdentityModelEventSource.ShowPII should only be true in Development for security reasons.
// Consider using: IdentityModelEventSource.ShowPII = builder.Environment.IsDevelopment();
IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure DbContext
builder.Services.AddDbContext<LeafLoopDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Unit of Work and Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>)); // Generic repository
// Specific repositories
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
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Add memory cache
builder.Services.AddMemoryCache();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// --- Authentication & Authorization Configuration ---
// 1. Configure Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false; // TODO: Consider enabling for stronger passwords.
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail = true;
        // TODO: Set SignIn.RequireConfirmedAccount to true once email confirmation flow is fully implemented and tested.
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<LeafLoopDbContext>()
    .AddDefaultTokenProviders()
    .AddRoleManager<RoleManager<IdentityRole<int>>>();

// 2. Configure combined authentication (Identity for MVC, JWT for API)
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ApplicationScheme;
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
                Encoding.UTF8.GetBytes(
                    builder.Configuration["JwtSettings:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var tokenSource = "None";
                var authHeader = context.Request.Headers["Authorization"].ToString();

                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                    tokenSource = "Authorization Header";
                }
                else
                {
                    var plannedCookieToken = context.Request.Cookies["auth_token"];
                    if (!string.IsNullOrEmpty(plannedCookieToken))
                    {
                        context.Token = plannedCookieToken;
                        tokenSource = "'auth_token' Cookie (HttpOnly)";
                    }
                    else
                    {
                        // TODO: Evaluate if the legacy 'jwt_token' cookie is still needed or can be phased out.
                        var legacyCookieToken = context.Request.Cookies["jwt_token"];
                        if (!string.IsNullOrEmpty(legacyCookieToken))
                        {
                            context.Token = legacyCookieToken;
                            tokenSource = "'jwt_token' Cookie (Non-HttpOnly, legacy)";
                        }
                    }
                }
                // FIXME: This console logging is too verbose for production. Use ILogger with appropriate log levels.
                Console.WriteLine(
                    $"SERVER (OnMessageReceived): Token provided by: {tokenSource}. Token value: {(string.IsNullOrEmpty(context.Token) ? "NONE" : "[PRESENT]")}");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                // FIXME: This console logging is too verbose for production. Use ILogger.
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("--------------------------------------------------------------------------");
                Console.WriteLine("SERVER: JWT AUTHENTICATION FAILED (OnAuthenticationFailed):");
                Console.WriteLine($"Exception Type: {context.Exception?.GetType().FullName}");
                Console.WriteLine($"Exception Message: {context.Exception?.Message}");
                Console.WriteLine("Full Exception Stack Trace:");
                Console.WriteLine(context.Exception?.ToString());
                Console.WriteLine("--------------------------------------------------------------------------");
                Console.ResetColor();
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                // FIXME: This console logging is too verbose for production. Use ILogger.
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("SERVER: JWT TOKEN SUCCESSFULLY VALIDATED for user: " +
                                  context.Principal?.Identity?.Name);
                Console.ResetColor();
                // TODO: Implement token blacklist logic here if needed for immediate revocation.
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // FIXME: This console logging is too verbose for production. Use ILogger.
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("SERVER: JWT CHALLENGE (OnChallenge) triggered.");
                Console.WriteLine(
                    $"Error: {context.Error}, Description: {context.ErrorDescription}, Auth Failure: {context.AuthenticateFailure?.Message}");
                Console.ResetColor();
                // If API routes should strictly return 401 for JWT issues without any fallback to cookie auth challenges:
                // if (context.Request.Path.StartsWithSegments("/api"))
                // {
                //     context.HandleResponse(); // This prevents other handlers, ensuring JWT is the sole challenger for /api.
                // }
                return Task.CompletedTask;
            }
        };
    });

// 3. Configure Application Cookie (for MVC/Identity UI)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Ensures cookie is sent only over HTTPS.
    options.Cookie.SameSite = SameSiteMode.Lax; // Good balance for security and usability.

    options.Events.OnRedirectToLogin = context =>
    {
        // This static method is defined at the end of this file.
        return HandleApiRedirect(context, StatusCodes.Status401Unauthorized, "Authentication required.");
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        // This static method is defined at the end of this file.
        return HandleApiRedirect(context, StatusCodes.Status403Forbidden, "Access denied.");
    };
});

// 4. Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiAuthPolicy", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });
    // TODO: Define other role-based or claim-based policies as application requirements grow.
    // Example: options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin").Combine(options.GetPolicy("ApiAuthPolicy")!));
});
// --- End of Authentication & Authorization Configuration ---

// Add Swagger/OpenAPI support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "LeafLoop API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
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
                },
                Scheme = "oauth2", Name = "Bearer", In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // Include XML comments for richer Swagger documentation.
    // Ensure <GenerateDocumentationFile>true</GenerateDocumentationFile> is in the .csproj file.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    else
    {
        // This warning helps during setup if the XML file is missing.
        Console.WriteLine($"Warning: XML documentation file not found at '{xmlPath}'. Swagger UI descriptions might be incomplete.");
    }
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", corsPolicyBuilder =>
    {
        // FIXME: "AllowAll" is very permissive. For production, restrict to known origins.
        // If credentials (cookies, auth headers) are needed, .AllowAnyOrigin() cannot be used with .AllowCredentials().
        // Specific origins must be listed in that case.
        corsPolicyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();

        // Example for specific origins with credentials:
        // corsPolicyBuilder.WithOrigins("http://localhost:5185", "https://psgej.com") // Replace with your actual client origins
        //        .AllowAnyMethod()
        //        .AllowAnyHeader()
        //        .AllowCredentials();
    });
});

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
    app.UseExceptionHandler("/Home/Error"); // Standard MVC error handler page.
    app.UseHsts(); // Adds Strict-Transport-Security header.
}

// TODO: Implement Azure Key Vault (or other secure secret management) for production.
// The following is a placeholder. Ensure correct packages (Azure.Identity, Azure.Extensions.AspNetCore.Configuration.Secrets)
// and authentication (e.g., Managed Identity) are set up.
if (builder.Environment.IsProduction())
{
    var keyVaultName = builder.Configuration["KeyVaultName"];
    if (!string.IsNullOrEmpty(keyVaultName))
    {
        // var keyVaultUri = $"https://{keyVaultName}.vault.azure.net/";
        // builder.Configuration.AddAzureKeyVault( new Uri(keyVaultUri), new DefaultAzureCredential());
    }
}

app.UseMiddleware<EnhancedErrorHandlingMiddleware>(); // Custom global error handling.
app.UseHttpsRedirection(); // Redirect HTTP to HTTPS.

// Static files configuration
var fileExtensionProvider = new FileExtensionContentTypeProvider();
if (!fileExtensionProvider.Mappings.ContainsKey(".css")) fileExtensionProvider.Mappings.Add(".css", "text/css");
if (!fileExtensionProvider.Mappings.ContainsKey(".webp")) fileExtensionProvider.Mappings.Add(".webp", "image/webp");
if (!fileExtensionProvider.Mappings.ContainsKey(".woff2")) fileExtensionProvider.Mappings.Add(".woff2", "font/woff2");
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = fileExtensionProvider,
    OnPrepareResponse = ctx =>
    {
        var defaultMaxAge = TimeSpan.FromDays(7); // Cache for 7 days for most static assets.
        // Shorter cache for CSS/JS during development or if they change frequently.
        // TODO: Review caching strategy for production vs. development for CSS/JS.
        var specificMaxAge = (ctx.File.Name.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                              ctx.File.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                           ? TimeSpan.FromHours(1)
                           : defaultMaxAge;
        ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={specificMaxAge.TotalSeconds}");
        ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff"); // Security header.
    }
});

// Security Headers Middleware
app.UseXContentTypeOptions(); // Prevents MIME-sniffing.
app.UseReferrerPolicy(options => options.NoReferrer()); // Controls referrer information.
app.UseXXssProtection(options => options.EnabledWithBlockMode()); // XSS protection (mostly for older browsers).
app.UseXfo(options => options.Deny()); // Prevents clickjacking (X-Frame-Options).

// Content Security Policy (CSP)
// FIXME: CSP needs careful review. 'unsafe-inline' for scripts and styles should be avoided if possible by using nonces, hashes, or redesigning inline resources.
app.UseCsp(options => options
    .DefaultSources(s => s.Self())
    .ScriptSources(s => s.Self()
        .CustomSources("https://cdn.jsdelivr.net", "https://cdnjs.cloudflare.com", "https://unpkg.com")
        .UnsafeInline()) // FIXME: Strive to remove 'unsafe-inline'.
    .StyleSources(s => s.Self()
        .CustomSources("https://cdn.jsdelivr.net", "https://cdnjs.cloudflare.com")
        .UnsafeInline()) // FIXME: Strive to remove 'unsafe-inline'.
    .ImageSources(s => s.Self().CustomSources("data:", "blob:")) // 'data:' for inline images, 'blob:' for client-side generated images.
    .FontSources(s => s.Self().CustomSources("https://cdn.jsdelivr.net", "https://cdnjs.cloudflare.com"))
    .FormActions(s => s.Self()) // Restricts where forms can submit to.
    .FrameAncestors(s => s.None()) // Equivalent to X-Frame-Options: DENY.
    .ObjectSources(s => s.None()) // Disallows <object>, <embed>, <applet>.
    .ConnectSources(s => s.Self()) // Restricts AJAX, WebSockets, etc. Add other domains if your API calls them.
    .UpgradeInsecureRequests() // Converts HTTP requests to HTTPS on the client side.
);

// Middleware order is critical for security and functionality.
app.UseRouting();         // Determines which endpoint will handle the request.
app.UseCors("AllowAll");  // Applies CORS policy. Must be after Routing and before Auth.
app.UseAuthentication();  // Identifies the user.
app.UseAuthorization();   // Verifies if the identified user has permission.

// Map controllers for API and MVC routes.
app.MapControllers(); // For attribute-routed API controllers.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // For conventional MVC routes.

// Seed data - This should be idempotent.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    var configuration = services.GetRequiredService<IConfiguration>();

    string[] roleNames = { "Admin", "User", "Moderator" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            if (roleResult.Succeeded)
            {
                logger.LogInformation("Role '{RoleName}' created.", roleName);
            }
            else
            {
                logger.LogError("Failed to create role '{RoleName}': {Errors}", roleName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }
    }

    try
    {
        // TODO: Consider moving seeding logic into a dedicated static class or service for better separation of concerns.
        await RoleInitializationService.InitializeRoles(services);
        await AdminUserSeeder.SeedAdminUser(services, configuration); // Assumes this seeder is idempotent.
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing roles or seeding system admin user.");
    }

    try
    {
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        // Seed categories
        if (await unitOfWork.Categories.CountAsync() == 0) 
        {
            logger.LogInformation("Seeding initial categories...");
            await unitOfWork.Categories.AddRangeAsync(new List<Category>
            {
                new Category { Name = "Electronics", Description = "Electronic devices", IconPath = "/img/categories/electronics.png" },
                new Category { Name = "Clothing", Description = "Apparel and accessories", IconPath = "/img/categories/clothes.png" }
                // TODO: Add more essential initial categories.
            });
            await unitOfWork.CompleteAsync(); 
            logger.LogInformation("Initial categories seeded.");
        }

        // Seed a default admin user (idempotent pattern)
        var adminEmail = configuration["AdminUser:Email"] ?? "admin@leafloop.pl";
        var adminPassword = configuration["AdminUser:Password"] ?? "Admin123!"; // FIXME: Default password should not be hardcoded here for production setup. Load from secure config.
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new User
            {
                UserName = adminEmail, Email = adminEmail, FirstName = "Admin", LastName = "Leafloop",
                CreatedDate = DateTime.UtcNow, LastActivity = DateTime.UtcNow, IsActive = true, EcoScore = 100, EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                logger.LogInformation("Default admin user '{AdminEmail}' created and assigned Admin role.", adminEmail);
            }
            else
            {
                logger.LogError("Failed to create default admin user '{AdminEmail}': {Errors}", adminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        // Seed a test user (development environment only, idempotent pattern)
        if (app.Environment.IsDevelopment())
        {
            var testUserEmail = "test@example.com";
            var testUserPassword = "Password123!"; // FIXME: Test user password for development.
            if (await userManager.FindByEmailAsync(testUserEmail) == null)
            {
                logger.LogInformation("Seeding test user '{TestUserEmail}' for development environment...", testUserEmail);
                var testUser = new User
                {
                    UserName = testUserEmail, Email = testUserEmail, FirstName = "Jan", LastName = "Testowy",
                    CreatedDate = DateTime.UtcNow, LastActivity = DateTime.UtcNow, IsActive = true, EcoScore = 75, EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(testUser, testUserPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, "User");
                    logger.LogInformation("Test user '{TestUserEmail}' seeded successfully and assigned User role.", testUserEmail);
                }
                else
                {
                    logger.LogError("Failed to seed test user '{TestUserEmail}': {Errors}", testUserEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database data seeding.");
    }
}

app.Run();

// --- Helper function for handling API redirects for cookie authentication ---
// This function ensures that API calls (identified by path starting with /api)
// that would normally be redirected by cookie authentication (e.g., to a login page)
// instead receive a proper JSON error response, suitable for API clients.
static async Task HandleApiRedirect(RedirectContext<CookieAuthenticationOptions> context, int statusCode, string message)
{
    // Check if the request path targets an API endpoint.
    if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.ErrorResponse(message); 

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        // Write the JSON response.
        // This prevents the default redirect behavior of the cookie authentication middleware.
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
        // No need to call context.HandleResponse() here, as writing to the response directly
        // and not calling context.Response.Redirect() effectively handles it.
    }
    // For non-API requests, allow the default redirect behavior.
    // The middleware will proceed with context.Response.Redirect(context.RedirectUri) if not handled here.
}
