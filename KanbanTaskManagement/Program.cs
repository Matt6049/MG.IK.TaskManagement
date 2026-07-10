using KanbanTaskManagement.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace KanbanTaskManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var databaseSettings = builder.Configuration.GetSection("Database");
            builder.Services.Configure<DbSettings>(databaseSettings);
            builder.Services.AddScoped<MongoDBContext>();


            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => {
                    options.LoginPath = "/Login";
                    options.ExpireTimeSpan = TimeSpan.FromDays(3);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    //bez securepolicy bo robimy http
                });


            var app = builder.Build();

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

            app.Run();
        }
    }
}
