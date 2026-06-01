using e_commerce_web_admin.Data;
using e_commerce_web_admin.Services.Attributes;
using e_commerce_web_admin.Services.Brands;
using e_commerce_web_admin.Services.Categories;
using e_commerce_web_admin.Services.CategorySpecifications;
using e_commerce_web_admin.Services.CategoryVariantAttributes;
using e_commerce_web_admin.Services.PaymentMethods;
using e_commerce_web_admin.Services.Products;
using e_commerce_web_admin.Services.Specifications;
using e_commerce_web_admin.Services.Suppliers;
using e_commerce_web_admin.Services.Uploads;
using e_commerce_web_admin.Services.Vouchers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ICategoryAdminService, CategoryAdminService>();
builder.Services.AddScoped<IBrandAdminService, BrandAdminService>();
builder.Services.AddScoped<ISpecificationAdminService, SpecificationAdminService>();
builder.Services.AddScoped<ICategorySpecAdminService, CategorySpecAdminService>();
builder.Services.AddScoped<IAttributeAdminService, AttributeAdminService>();
builder.Services.AddScoped<ICvaAdminService, CvaAdminService>();
builder.Services.AddScoped<IProductAdminService, ProductAdminService>();
builder.Services.AddScoped<IPaymentMethodAdminService, PaymentMethodAdminService>();
builder.Services.AddScoped<ISupplierAdminService, SupplierAdminService>();
builder.Services.AddScoped<IVoucherAdminService, VoucherAdminService>();
builder.Services.Configure<CloudinaryOptions>(
    builder.Configuration.GetSection(CloudinaryOptions.SectionName));
builder.Services.AddScoped<IImageUploadService, CloudinaryImageUploadService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
