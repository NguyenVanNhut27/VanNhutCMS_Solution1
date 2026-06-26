using CMS.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace CMS.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Đăng ký DbContext vào hệ thống
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Khai báo dịch vụ xác thực Cookie
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)  
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login"; // Đường dẫn nếu chưa đăng nhập
                    options.AccessDeniedPath = "/Account/AccessDenied"; // Đường dẫn nếu vào trang không được phép
                });

            // ==========================================
            // CẤU HÌNH CORS CHO FRONTEND NEXT.JS
            // ==========================================
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowNextJS",
                    policy => policy.WithOrigins("http://localhost:3000") // Cổng của ứng dụng Next.js
                                    .AllowAnyMethod()
                                    .AllowAnyHeader()
                                    .AllowCredentials());
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // ==========================================
            // KÍCH HOẠT CORS (Bắt buộc phải đặt trước Authentication/Authorization)
            // ==========================================
            app.UseCors("AllowNextJS");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}