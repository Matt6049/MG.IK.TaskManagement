using KanbanTaskManagement.Data;
using KanbanTaskManagement.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace KanbanTaskManagement
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var databaseSettings = builder.Configuration.GetSection("Database");
            builder.Services.Configure<DbSettings>(databaseSettings);
            builder.Services.AddScoped<MongoDBContext>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUser, CurrentUserService>();

            var requireAuth = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            builder.Services.AddControllersWithViews(options => {
                options.Filters.Add(new AuthorizeFilter(requireAuth));
            });
            builder.Services.AddRazorPages();
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/Login";
                    options.ReturnUrlParameter = "returnUrl";
                    options.ExpireTimeSpan = TimeSpan.FromDays(3);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    //bez securepolicy bo robimy http
                });


            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<MongoDBContext>();
                await DbInitializer.InitializeAsync(db);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

			app.UseAuthentication();
			app.UseAuthorization();


            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            await app.RunAsync();
        }
    }
}
