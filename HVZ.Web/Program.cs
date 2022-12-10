using Microsoft.AspNetCore.Identity;
using HVZ.Web.Data;
using HVZ.Persistence;
using HVZ.Persistence.MongoDB.Repos;
using HVZ.Web.Identity;
using HVZ.Web.Identity.Models;
using HVZ.Web.Settings;
using MongoDB.Driver;
using NodaTime;

namespace HVZ.Web;
internal static class Program
{
        public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions()
            {
                ApplicationName = "HVZ.Web",
            }
        );

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddHttpClient();

        #region Persistence
        var mongoClient = new MongoClient(
            builder.Configuration["DatabaseSettings:ConnectionString"]
        );

        var mongoDatabase = mongoClient.GetDatabase(
            builder.Configuration["DatabaseSettings:DatabaseName"]
        );

        IGameRepo gameRepo = new GameRepo(mongoDatabase, SystemClock.Instance);
        IUserRepo userRepo = new UserRepo(mongoDatabase, SystemClock.Instance);
        IOrgRepo  orgRepo  = new OrgRepo(mongoDatabase, SystemClock.Instance, userRepo, gameRepo);

        builder.Services.AddSingleton<IGameRepo>(gameRepo);
        builder.Services.AddSingleton<IUserRepo>(userRepo);
        builder.Services.AddSingleton<IOrgRepo>(orgRepo);


        #endregion

        #region Identity

        var mongoIdentitySettings = builder.Configuration.GetSection(nameof(MongoIdentityConfig)).Get<MongoIdentityConfig>();
        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
            (
                mongoIdentitySettings?.ConnectionString, mongoIdentitySettings?.Name
            );
        builder.Services.AddScoped<
            IUserClaimsPrincipalFactory<ApplicationUser>, 
            ApplicationClaimsPrincipalFactory
        >();

        #endregion

        builder.Services.AddSingleton<WeatherForecastService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapBlazorHub();
        // app.MapControllers(); // Enable for API
        app.MapFallbackToPage("/_Host");

        app.Run();
    }

}



