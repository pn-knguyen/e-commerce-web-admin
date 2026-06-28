using e_commerce_web_admin.Data;
using e_commerce_web_admin.Hubs;
using e_commerce_web_admin.Integrations.GiaoHangNhanh;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Services.Attributes;
using e_commerce_web_admin.Services.Brands;
using e_commerce_web_admin.Services.Categories;
using e_commerce_web_admin.Services.CategorySpecifications;
using e_commerce_web_admin.Services.CategoryVariantAttributes;
using e_commerce_web_admin.Services.CustomerMessages;
using e_commerce_web_admin.Services.Customers;
using e_commerce_web_admin.Services.FulfillmentLocations;
using e_commerce_web_admin.Services.Identity;
using e_commerce_web_admin.Services.Inventory;
using e_commerce_web_admin.Services.Orders;
using e_commerce_web_admin.Services.PaymentMethods;
using e_commerce_web_admin.Services.Products;
using e_commerce_web_admin.Services.ProductVariants;
using e_commerce_web_admin.Services.Promotions;
using e_commerce_web_admin.Services.Ratings;
using e_commerce_web_admin.Services.Shipping;
using e_commerce_web_admin.Services.Shipping.Providers;
using e_commerce_web_admin.Services.Specifications;
using e_commerce_web_admin.Services.Suppliers;
using e_commerce_web_admin.Services.Uploads;
using e_commerce_web_admin.Services.Vouchers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 64 * 1024;
    options.MaximumParallelInvocationsPerClient = 1;
});
builder.Services.AddMemoryCache();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("CustomerMessageHttp", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetCustomerMessageRateLimitKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = TimeSpan.FromMinutes(1),
            }));
    options.AddPolicy("CustomerMessageHubConnection", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetCustomerMessageRateLimitKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 30,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = TimeSpan.FromMinutes(1),
            }));
});
var customerMessageOrigins = builder.Configuration
    .GetSection("CustomerMessages:AllowedCustomerOrigins")
    .Get<string[]>()
    ?.Select(origin => origin.Trim().TrimEnd('/'))
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray() ?? [];
if (customerMessageOrigins.Length == 0)
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "CustomerMessages:AllowedCustomerOrigins must be configured for production.");
    }

    customerMessageOrigins =
    [
        "http://localhost:5132",
        "https://localhost:7124",
    ];
}
else if (customerMessageOrigins.Any(origin => !IsValidHttpOrigin(origin)))
{
    throw new InvalidOperationException(
        "CustomerMessages:AllowedCustomerOrigins must contain absolute HTTP or HTTPS origins without a path.");
}
else if (!builder.Environment.IsDevelopment() &&
    customerMessageOrigins.Any(IsLoopbackUrl))
{
    throw new InvalidOperationException(
        "CustomerMessages:AllowedCustomerOrigins cannot contain localhost or loopback origins in production.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("CustomerMessageRealtime", policy =>
    {
        policy
            .WithOrigins(customerMessageOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Identity dùng bảng staff cho tài khoản quản trị/nhân sự.
// Bảng users vẫn dành cho khách hàng, không dùng để đăng nhập trang admin.
builder.Services
    .AddIdentity<Staff, IdentityRole<long>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var customerMessageJwtSection = builder.Configuration.GetSection(CustomerMessageJwtOptions.SectionName);
var customerMessageJwt = customerMessageJwtSection.Get<CustomerMessageJwtOptions>() ?? new();
builder.Services
    .AddOptions<CustomerMessageJwtOptions>()
    .Bind(customerMessageJwtSection)
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Issuer) &&
            !string.IsNullOrWhiteSpace(options.AccessAudience) &&
            !string.IsNullOrWhiteSpace(options.AiReceiptAudience) &&
            Encoding.UTF8.GetByteCount(options.SigningKey ?? string.Empty) >= 32,
        "CustomerMessages:Jwt phải có issuer, audience và signing key tối thiểu 32 byte.")
    .ValidateOnStart();

builder.Services
    .AddAuthentication()
    .AddJwtBearer(CustomerMessageAuthenticationDefaults.Scheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = customerMessageJwt.Issuer,
            ValidateAudience = true,
            ValidAudience = customerMessageJwt.AccessAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(customerMessageJwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/customer-messages"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // Khi đổi role/quyền hoặc khóa staff, cookie cũ sẽ được kiểm tra lại ngay request kế tiếp.
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.AddScoped<IUserClaimsPrincipalFactory<Staff>, StaffClaimsPrincipalFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddGiaoHangNhanhIntegration(builder.Configuration);

// Business services cho các module quản trị.
builder.Services.AddScoped<ICategoryHierarchyService, CategoryHierarchyService>();
builder.Services.AddScoped<ICategoryAdminService, CategoryAdminService>();
builder.Services.AddScoped<IBrandAdminService, BrandAdminService>();
builder.Services.AddScoped<ISpecificationAdminService, SpecificationAdminService>();
builder.Services.AddScoped<ICategorySpecAdminService, CategorySpecAdminService>();
builder.Services.AddScoped<IAttributeAdminService, AttributeAdminService>();
builder.Services.AddScoped<ICvaAdminService, CvaAdminService>();
builder.Services.AddScoped<ICustomerAdminService, CustomerAdminService>();
builder.Services.AddScoped<ICustomerMessageAdminService, CustomerMessageAdminService>();
builder.Services.AddScoped<ICustomerMessageRealtimeNotifier, CustomerMessageRealtimeNotifier>();
builder.Services.AddSingleton<ICustomerMessageRateLimiter, CustomerMessageRateLimiter>();
builder.Services.AddSingleton<ICustomerAiReceiptValidator, CustomerAiReceiptValidator>();
builder.Services.AddScoped<IProductAdminService, ProductAdminService>();
builder.Services.AddScoped<IProductVariantAdminService, ProductVariantAdminService>();
builder.Services.AddScoped<IOrderAdminService, OrderAdminService>();
builder.Services.AddScoped<IPaymentMethodAdminService, PaymentMethodAdminService>();
builder.Services.AddScoped<IRatingAdminService, RatingAdminService>();
builder.Services.AddScoped<IPromotionAdminService, PromotionAdminService>();
builder.Services.AddScoped<ISupplierAdminService, SupplierAdminService>();
builder.Services.AddScoped<IVoucherAdminService, VoucherAdminService>();
builder.Services.AddScoped<IInventoryAdminService, InventoryAdminService>();
builder.Services.AddScoped<IShippingProviderGateway, GiaoHangNhanhShippingProviderGateway>();
builder.Services.AddScoped<IShipmentAdminService, ShipmentAdminService>();
builder.Services.AddHostedService<ShipmentStatusSyncWorker>();
builder.Services.AddScoped<IFulfillmentLocationAdminService, FulfillmentLocationAdminService>();
builder.Services.Configure<CloudinaryOptions>(
    builder.Configuration.GetSection(CloudinaryOptions.SectionName));
builder.Services.AddScoped<IImageUploadService, CloudinaryImageUploadService>();

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

app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets();
app.MapHub<CustomerMessageHub>("/hubs/customer-messages")
    .RequireCors("CustomerMessageRealtime")
    .RequireRateLimiting("CustomerMessageHubConnection")
    .RequireAuthorization(policy =>
    {
        policy.AddAuthenticationSchemes(
            IdentityConstants.ApplicationScheme,
            CustomerMessageAuthenticationDefaults.Scheme);
        policy.RequireAuthenticatedUser();
    });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static bool IsLoopbackUrl(string value) =>
    Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
    (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase));

static bool IsValidHttpOrigin(string value)
{
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
    {
        return false;
    }

    return (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/')) &&
        string.IsNullOrWhiteSpace(uri.Query) &&
        string.IsNullOrWhiteSpace(uri.Fragment);
}

static string GetCustomerMessageRateLimitKey(HttpContext context)
{
    var customerId = context.User.FindFirst(CustomerMessageAuthenticationDefaults.CustomerIdClaim)?.Value;
    if (!string.IsNullOrWhiteSpace(customerId))
    {
        return $"customer:{customerId}";
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
