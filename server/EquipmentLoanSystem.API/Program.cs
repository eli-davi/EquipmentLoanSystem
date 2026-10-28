using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using EquipmentLoanSystem.API.Middlewares;
using EquipmentLoanSystem.API.Profiles;
using EquipmentLoanSystem.Core.Constants;
using EquipmentLoanSystem.Core.Interfaces;
using EquipmentLoanSystem.Core.Settings;
using EquipmentLoanSystem.Data;
using EquipmentLoanSystem.Data.Repositories;
using EquipmentLoanSystem.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog;
using NLog.Web;

// לוגר מוקדם - כדי לתפוס גם שגיאות שקורות לפני שה-host עלה
var bootstrapLogger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ---------- 1. Logging: הקוד משתמש ב-ILogger הרגיל, NLog מתחבר מתחת ----------
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // ---------- 2. Configuration: JWT דרך IOptions ----------
    var jwtSection = builder.Configuration.GetSection(JwtSettings.SectionName);
    var jwtSecret = jwtSection["Secret"];

    if (string.IsNullOrWhiteSpace(jwtSecret))
    {
        if (!builder.Environment.IsDevelopment())
            throw new InvalidOperationException("JwtSettings:Secret is not configured.");

        // אין סוד בקוד ואין סוד ב-repo. אם לא הוגדר User Secret - נוצר מפתח אקראי לזמן הריצה בלבד,
        // כך שהמערכת עולה מיד אחרי clone. (tokens יפסיקו להיות תקפים אחרי הפעלה מחדש)
        jwtSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        bootstrapLogger.Warn("JwtSettings:Secret not found in User Secrets - using an ephemeral random key for this run.");
    }

    builder.Services.Configure<JwtSettings>(options =>
    {
        jwtSection.Bind(options);
        options.Secret = jwtSecret;
    });

    // ---------- 3. Authentication & Authorization ----------
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false; // שומרים על שמות ה-claims כפי שנכתבו: sub, role, email
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                NameClaimType = AppClaimTypes.Name,
                RoleClaimType = AppClaimTypes.Role,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

    builder.Services.AddAuthorization();

    // ---------- 4. CORS ----------
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Client", policy =>
        {
            if (allowedOrigins.Length > 0)
                policy.WithOrigins(allowedOrigins);
            else
                policy.AllowAnyOrigin();

            policy.AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Location", CorrelationIdMiddleware.HeaderName);
        });
    });

    // ---------- 5. Controllers + Swagger ----------
    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Equipment Loan API", Version = "v1" });

        var bearer = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "הדביקו כאן את ה-token שהתקבל מ-POST /api/auth/login",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };
        c.AddSecurityDefinition("Bearer", bearer);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearer, Array.Empty<string>() } });
    });

    // ---------- 6. EF Core: connection string מהקונפיגורציה בלבד ----------
    var rawConnectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException(
            "ConnectionStrings:Default is missing. Set it with dotnet user-secrets, or in the cloud with the environment variable ConnectionStrings__Default.");
    var connectionString = EquipmentLoanSystem.API.Extensions.ConnectionStringHelper.ToNpgsql(rawConnectionString);

    // DbContext = Scoped (ברירת המחדל של AddDbContext): מופע אחד לכל בקשת HTTP
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));

    // ---------- 7. DI: interfaces ב-Core, מימושים ב-Data/Service ----------
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<IItemRepository, ItemRepository>();
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<IItemCopyRepository, ItemCopyRepository>();
    builder.Services.AddScoped<ILoanRepository, LoanRepository>();
    builder.Services.AddScoped<IMemberRepository, MemberRepository>();

    builder.Services.AddScoped<IItemService, ItemService>();
    builder.Services.AddScoped<ILoanService, LoanService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>(); // stateless - בטוח כ-singleton

    builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

    var app = builder.Build();

    // ---------- 8. בסיס הנתונים נוצר/מתעדכן אוטומטית + Seed - בלי התערבות ידנית ----------
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        if (db.Database.GetMigrations().Any())
        {
            startupLogger.LogInformation("Applying database migrations...");
            await db.Database.MigrateAsync();
        }
        else
        {
            // רשת ביטחון: אם עדיין לא נוצרו migrations - יוצרים את הסכמה ישירות מהמודל
            startupLogger.LogWarning("No migrations found - creating the database from the model (EnsureCreated)");
            await db.Database.EnsureCreatedAsync();
        }
        startupLogger.LogInformation("Database is ready");
    }

    // ---------- 9. Pipeline - הסדר חשוב ----------
    app.UseMiddleware<ErrorHandlingMiddleware>();   // ראשון: תופס חריגות מכל מה שאחריו
    app.UseMiddleware<CorrelationIdMiddleware>();   // CorrelationId לכל בקשה + לוג בקשה נכנסת

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Equipment Loan API V1");
        c.RoutePrefix = "swagger";
    });

    // בענן (Render/Railway וכו') ה-HTTPS מטופל ע"י הפלטפורמה לפני האפליקציה, ולכן אין הפניה ל-HTTPS בקוד

    app.UseCors("Client");
    app.UseAuthentication();   // authentication לפני authorization
    app.UseAuthorization();
  app.MapGet("/", () => Results.Redirect("https://כתובת-האתר-שלך.onrender.com"));
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    bootstrapLogger.Error(ex, "Application stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}

// נדרש כדי שפרויקטי בדיקות יוכלו להפנות ל-Program
public partial class Program { }
