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
using System.Net.Mail;
using System.Net;

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
        public async Task<IActionResult> Login(string role, string? userId, string? username, string password)
        {
            Console.WriteLine($"📌 Nhận dữ liệu đăng nhập: role={role}, userId={userId}, username={username}, password={password}");

            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(password) || (role == "Admin" && string.IsNullOrEmpty(username)) || ((role == "Student" || role == "Lecturer") && string.IsNullOrEmpty(userId)))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin đăng nhập.");
                return View();
            }

            var hashedPassword = HashPassword(password);
            User? user = null;
            if (role == "Admin")
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin" && u.Username == username && u.Password == hashedPassword);
            }
            else
            {
                if (int.TryParse(userId, out int userIdInt))
                {
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Role == role && u.UserId == userIdInt && u.Password == hashedPassword);
                }
                else
                {
                    ModelState.AddModelError("", "Mã tài khoản không hợp lệ.");
                    return View();
                }
            }

            if (user == null)
            {
                ModelState.AddModelError("", "Thông tin đăng nhập không đúng.");
                return View();
            }

            CurrentUserID = user.UserId.ToString();
            CurrentUserName = user.Username;
            CurrentUserRole = user.Role;
            if (user.Role == "Admin")
            {
                return RedirectToAction("Index", "Statistics");
            }
            else
            {
                return RedirectToAction("Index", "Home");
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

            // Kiểm tra xem mã tài khoản đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.UserId == model.UserId))
            {
                ModelState.AddModelError("", "Mã tài khoản đã tồn tại.");
                return View();
            }

            // Cho phép tên đăng nhập trùng
            // Không kiểm tra Username trùng nữa

            // Mã hóa mật khẩu trước khi lưu
            model.Password = HashPassword(model.Password);
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _context.Users.Add(model);
            await _context.SaveChangesAsync();

            // Chuyển hướng đến trang đăng nhập sau khi đăng ký thành công
            return RedirectToAction("Register");
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

        // Phương thức hiển thị form quên mật khẩu
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // Phương thức xử lý quên mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập email của bạn.");
                return View();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Vẫn hiển thị thông báo thành công để bảo mật
                TempData["SuccessMessage"] = "Nếu email của bạn tồn tại trong hệ thống, chúng tôi sẽ gửi link đặt lại mật khẩu.";
                return RedirectToAction("Login");
            }

            // Tạo token reset password
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.ResetPasswordToken = token;
            user.ResetPasswordTokenExpiry = DateTime.Now.AddHours(24); // Token hết hạn sau 24 giờ
            await _context.SaveChangesAsync();

            // Gửi email reset password
            var resetLink = Url.Action("ResetPassword", "Account", 
                new { token = token }, Request.Scheme);

            // Cấu hình email
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("hethongcvht@gmail.com", "qbpm dnag qczm kvgq"), // Thay thế bằng email và mật khẩu ứng dụng của bạn
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("hethongcvht@gmail.com", "Hệ thống CVHT"), // Thay thế bằng email của bạn
                Subject = "Đặt lại mật khẩu",
                Body = $@"
                    <h2>Yêu cầu đặt lại mật khẩu</h2>
                    <p>Xin chào {user.Username},</p>
                    <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                    <p>Vui lòng click vào link sau để đặt lại mật khẩu:</p>
                    <p><a href='{resetLink}'>{resetLink}</a></p>
                    <p>Link này sẽ hết hạn sau 24 giờ.</p>
                    <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                    <p>Trân trọng,<br>Hệ thống cố vấn học tập</p>",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(email);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                TempData["SuccessMessage"] = "Vui lòng kiểm tra email của bạn để đặt lại mật khẩu.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi gửi email: " + ex.ToString());
                return View();
            }

            return RedirectToAction("Login");
        }

        // Phương thức hiển thị form đặt lại mật khẩu
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            return View(new ResetPasswordViewModel { Token = token });
        }

        // Phương thức xử lý đặt lại mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ResetPasswordToken == model.Token && 
                                        u.ResetPasswordTokenExpiry > DateTime.Now);

            if (user == null)
            {
                ModelState.AddModelError("", "Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
                return View(model);
            }

            user.Password = HashPassword(model.NewPassword);
            user.ResetPasswordToken = null;
            user.ResetPasswordTokenExpiry = null;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction("Login");
        }
    }

    public class ResetPasswordViewModel
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }
}
