using Do_An_Tot_Nghiep.Data;
using Do_An_Tot_Nghiep.Models;
using Do_An_Tot_Nghiep.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// 📌 Đọc cấu hình từ appsettings.json
var configuration = builder.Configuration;

// 📌 Cấu hình Database (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

// 📌 Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Hết hạn session sau 30 phút
    options.Cookie.HttpOnly = true; // Bảo mật cookie
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
});

// 📌 Đăng ký HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 📌 Cấu hình MVC
builder.Services.AddControllersWithViews();

// 📌 Đăng ký DocumentService trước GeminiService
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();

builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

// 📌 Cấu hình CORS (nếu có API riêng)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login"; // Nếu chưa login thì chuyển về đây
        options.AccessDeniedPath = "/Account/AccessDenied"; // Nếu không đủ quyền
    });

builder.Services.AddHttpClient();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100 KB
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
});

// Ensure uploads directory exists
var uploadsPath = Path.Combine(builder.Environment.WebRootPath, "uploads", "images");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

var app = builder.Build();

// 📌 Middleware xử lý lỗi
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 📌 Middleware cơ bản
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAll"); // Kích hoạt CORS
app.UseSession(); // Kích hoạt Session
app.UseAuthentication(); // Xác thực
app.UseAuthorization(); // Phân quyền

// 📌 Khởi tạo dữ liệu mặc định (Admin...)
// (Chú ý không cần phải thay đổi ở đây nếu chỉ có cấu hình tải file)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(context);
}

// 📌 Định tuyến mặc định: vào Login trước khi đăng nhập
app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
