namespace Viewer.Web
{
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Identity.Web;
    using Microsoft.Identity.Web.UI;
    using System.Threading.Tasks;
    using Viewer.Hubs;
    using Viewer.Web.Hubs;

    public class Program
    {
        protected Program() { }

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddControllersWithViews(options =>
                {
                    AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    options.Filters.Add(new AuthorizeFilter(policy));
                })
                .AddMicrosoftIdentityUI();
            builder.Services.AddRazorPages();

            builder.Services.AddSignalR();

            builder.Services.AddGridEventsHubClient(options =>
            {
                builder.Configuration.Bind("GridEventsHubClient", options);
            });

            builder.Services
                .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(options => builder.Configuration.Bind("Authentication", options));

            builder.Services.AddHostedService<GridEventListenerBackgroundService>();

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<GridEventsHub>("/_events/grid");
                endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            await app.RunAsync();
        }
    }
}
