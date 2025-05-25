using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Net.Http;
using System.Text.Json;
using Do_An_Tot_Nghiep.ViewModels;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class AccountController : BaseController
    {
        private readonly AppDbContext _context;

        private readonly HttpClient _client;

        public AccountController(AppDbContext context, HttpClient client)
        {
            _context = context;
            _client = client;
            _client.BaseAddress = new Uri("http://localhost:11434");
        }


        [HttpGet]
        public IActionResult ChatIndex() => View(new ChatStatisticsViewModel());

        [HttpPost]
        public async Task<IActionResult> ChatIndex(ChatStatisticsViewModel model)
        {
            var requestBody = new
            {
                model = "mistral",
                prompt = model.UserMessage,
                stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/generate", content);
            var responseString = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseString);
            model.BotResponse = doc.RootElement.GetProperty("response").GetString();

            return View("ChatIndex", model);
        }

        // Phương thức hiển thị trang đăng nhập
        public IActionResult Login() => View();

        // Phương thức hiển thị trang đăng ký
        public IActionResult Register() => View();

        // Phương thức xử lý đăng nhập
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            Console.WriteLine($"📌 Nhận dữ liệu đăng nhập: {username} - {password}");

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("❌ Lỗi: Tên đăng nhập hoặc mật khẩu trống.");
                ModelState.AddModelError("", "Tên đăng nhập và mật khẩu không được để trống.");
                return View();
            }

            var hashedPassword = HashPassword(password);
            Console.WriteLine($"🔐 Mật khẩu đã hash: {hashedPassword}");

            var user = _context.Users
                .AsEnumerable()
                .FirstOrDefault(u => 
                    BaseController.ToSimpleUsername(u.Username) == username.ToLower() 
                    && u.Password == hashedPassword);

            if (user == null)
            {
                Console.WriteLine("❌ Không tìm thấy tài khoản.");
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View();
            }

            Console.WriteLine($"✅ Đăng nhập thành công! UserId: {user.UserId}, Role: {user.Role}");
            CurrentUserID = user.UserId.ToString();
            CurrentUserName = user.Username;
            CurrentUserRole = user.Role;
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Statistics");
            }
            else 
            {
                return RedirectToAction("Index", "Home"); // hoặc có thể chuyển tới trang riêng cho giảng viên
            }
        }

        // Phương thức xử lý đăng ký
        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Chặn đăng ký tài khoản Admin
            if (model.Role == "Admin")
            {
                ModelState.AddModelError("", "Không thể tự tạo tài khoản Admin.");
                return View();
            }

            // Kiểm tra xem tên người dùng đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Username.Equals(model.Username) ))
            {
                ModelState.AddModelError("", "Tên người dùng đã tồn tại.");
                return View();
            }

            // Mã hóa mật khẩu trước khi lưu
            model.Password = HashPassword(model.Password);
            model.CreatedAt = DateTime.Now;

            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            // Chuyển hướng đến trang đăng nhập sau khi đăng ký thành công
            return RedirectToAction("Login");
        }

        // Phương thức hiển thị thông tin cá nhân
        public async Task<IActionResult> Profile()
        {
            if(!IsLogin)
            {
                return RedirectToAction("Login");
            }

            if (!int.TryParse(CurrentUserID, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (user.Role == "Student")
            {
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.UserId == user.UserId);
                
                if (student != null)
                {
                    return View("StudentProfile", student);
                }
            }
            else if (user.Role == "Lecturer")
            {
                var lecturer = await _context.Lecturers
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.UserId == user.UserId);
                
                if (lecturer != null)
                {
                    return View("LecturerProfile", lecturer);
                }
            }

            return View(user);
        }

        // Phương thức hiển thị form chỉnh sửa thông tin
        public async Task<IActionResult> Edit(int id)
        {
            if(!IsLogin)
            {
                return RedirectToAction("Login");
            }

            if (!int.TryParse(CurrentUserID, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (user.Role == "Student")
            {
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.StudentId == id && s.UserId == user.UserId);
                
                if (student == null)
                {
                    return NotFound();
                }
                return View("~/Views/Account/EditStudent.cshtml", student);
            }
            else if (user.Role == "Lecturer")
            {
                var lecturer = await _context.Lecturers
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.LecturerId == id && l.UserId == user.UserId);
                
                if (lecturer == null)
                {
                    return NotFound();
                }
                return View("~/Views/Account/EditLecturer.cshtml", lecturer);
            }

            return NotFound();
        }

        // Phương thức xử lý cập nhật thông tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string role, IFormCollection form)
        {
            if(!IsLogin)
            {
                return RedirectToAction("Login");
            }

            if (!int.TryParse(CurrentUserID, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (role == "Student")
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentId == id && s.UserId == user.UserId);
                
                if (student == null)
                {
                    return NotFound();
                }

                student.FullName = form["FullName"];
                student.Class = form["Class"];
                student.Major = form["Major"];
                student.Email = form["Email"];
                student.Phone = form["Phone"];

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Profile));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Students.AnyAsync(e => e.StudentId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else if (role == "Lecturer")
            {
                var lecturer = await _context.Lecturers
                    .FirstOrDefaultAsync(l => l.LecturerId == id && l.UserId == user.UserId);
                
                if (lecturer == null)
                {
                    return NotFound();
                }

                lecturer.FullName = form["FullName"];
                lecturer.Position = form["Position"];
                lecturer.Department = form["Department"];
                lecturer.Specialization = form["Specialization"];
                lecturer.Email = form["Email"];
                lecturer.Phone = form["Phone"];

                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Profile));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Lecturers.AnyAsync(e => e.LecturerId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return NotFound();
        }

        // Phương thức đăng xuất
        public IActionResult Logout()
        {
            CurrentUserID = "";
            CurrentUserName = "";
            CurrentUserRole = "";
            return RedirectToAction("Login");
        }

        // Phương thức mã hóa mật khẩu
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        // Phương thức hiển thị form đổi mật khẩu
        public IActionResult ChangePassword()
        {
            if(!IsLogin)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        // Phương thức xử lý đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if(!IsLogin)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu mới và xác nhận mật khẩu không khớp.");
                return View();
            }

            if (!int.TryParse(CurrentUserID, out int userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var hashedCurrentPassword = HashPassword(currentPassword);
            if (user.Password != hashedCurrentPassword)
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
                return View();
            }

            user.Password = HashPassword(newPassword);
            user.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction(nameof(Profile));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật mật khẩu.");
                return View();
            }
        }
    }
}
