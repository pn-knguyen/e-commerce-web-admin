using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace e_commerce_web_admin.Integrations.GiaoHangNhanh;

public static class GiaoHangNhanhServiceCollectionExtensions
{
    public static IServiceCollection AddGiaoHangNhanhIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GiaoHangNhanhOptions>(
            configuration.GetSection(GiaoHangNhanhOptions.SectionName));
        services.AddHttpClient<IGiaoHangNhanhClient, GiaoHangNhanhClient>();

        return services;
    }
}
