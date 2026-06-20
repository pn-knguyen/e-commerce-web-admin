using e_commerce_web_admin.Data;
using e_commerce_web_admin.Models.Entities;
using e_commerce_web_admin.Services.Attributes;
using e_commerce_web_admin.Services.Brands;
using e_commerce_web_admin.Services.Categories;
using e_commerce_web_admin.Services.CategorySpecifications;
using e_commerce_web_admin.Services.CategoryVariantAttributes;
using e_commerce_web_admin.Services.Customers;
using e_commerce_web_admin.Services.Identity;
using e_commerce_web_admin.Services.Inventory;
using e_commerce_web_admin.Services.Orders;
using e_commerce_web_admin.Services.PaymentMethods;
using e_commerce_web_admin.Services.Products;
using e_commerce_web_admin.Services.ProductVariants;
using e_commerce_web_admin.Services.Promotions;
using e_commerce_web_admin.Services.Ratings;
using e_commerce_web_admin.Services.Specifications;
using e_commerce_web_admin.Services.Suppliers;
using e_commerce_web_admin.Services.Uploads;
using e_commerce_web_admin.Services.Vouchers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
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

// Business services cho các module quản trị.
builder.Services.AddScoped<ICategoryHierarchyService, CategoryHierarchyService>();
builder.Services.AddScoped<ICategoryAdminService, CategoryAdminService>();
builder.Services.AddScoped<IBrandAdminService, BrandAdminService>();
builder.Services.AddScoped<ISpecificationAdminService, SpecificationAdminService>();
builder.Services.AddScoped<ICategorySpecAdminService, CategorySpecAdminService>();
builder.Services.AddScoped<IAttributeAdminService, AttributeAdminService>();
builder.Services.AddScoped<ICvaAdminService, CvaAdminService>();
builder.Services.AddScoped<ICustomerAdminService, CustomerAdminService>();
builder.Services.AddScoped<IProductAdminService, ProductAdminService>();
builder.Services.AddScoped<IProductVariantAdminService, ProductVariantAdminService>();
builder.Services.AddScoped<IOrderAdminService, OrderAdminService>();
builder.Services.AddScoped<IPaymentMethodAdminService, PaymentMethodAdminService>();
builder.Services.AddScoped<IRatingAdminService, RatingAdminService>();
builder.Services.AddScoped<IPromotionAdminService, PromotionAdminService>();
builder.Services.AddScoped<ISupplierAdminService, SupplierAdminService>();
builder.Services.AddScoped<IVoucherAdminService, VoucherAdminService>();
builder.Services.AddScoped<IInventoryAdminService, InventoryAdminService>();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
