using ConferenceRoomApi.Application.AdditionalServices.Interfaces;
using ConferenceRoomApi.Application.Bookings.Interfaces;
using ConferenceRoomApi.Application.Common.Interfaces;
using ConferenceRoomApi.Application.Reports.Interfaces;
using ConferenceRoomApi.Application.Rooms.Interfaces;
using ConferenceRoomApi.Domain.Pricing;
using ConferenceRoomApi.Infrastructure.Common;
using ConferenceRoomApi.Infrastructure.Options;
using ConferenceRoomApi.Infrastructure.Persistence;
using ConferenceRoomApi.Infrastructure.Repositories;
using ConferenceRoomApi.Infrastructure.Reports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceRoomApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BusinessSettings>(configuration.GetSection(BusinessSettings.SectionName));

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IAdditionalServiceRepository, AdditionalServiceRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IReportsService, ReportsService>();

        services.AddSingleton<IPricingPolicy, StandardPricingPolicy>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
