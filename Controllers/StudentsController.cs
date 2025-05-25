using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class StudentsController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StudentsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Students
        public async Task<IActionResult> Index(string searchString)
        {
            if (!IsLogin || CurrentUserRole.Equals("Student"))
            {
                return RedirectToAction("Login", "Account");
            }
            

            var students = from s in _context.Students select s;

            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.FullName.Contains(searchString)
                                            || s.Email.Contains(searchString)
                                            || s.Major.Contains(searchString));
            }

            return View(await students.ToListAsync());
        }



        // GET: Students/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                if (id == null)
                {
                    return NotFound();
                }

                // Chuyển đổi id từ string sang int
                if (!int.TryParse(id, out int userIdInt))
                {
                    return NotFound(); // Hoặc xử lý lỗi không parse được id
                }

                // Tìm sinh viên dựa trên UserId thay vì StudentId
                var student = await _context.Students
                    .Include(u => u.User)
                    .FirstOrDefaultAsync(m => m.UserId == userIdInt);

                if (student == null)
                {
                    return NotFound();
                }
                
                // Lấy danh sách giảng viên thuộc sinh viên này
                var lecturers = await _context.LecturerStudents
                    .Where(ls => ls.StudentId == student.StudentId) // Sử dụng StudentId của sinh viên tìm được
                    .Include(ls => ls.Lecturer)
                    .Select(ls => new
                    {
                        ls.Lecturer.FullName,
                        ls.Lecturer.Position,
                        ls.Lecturer.Email,
                        ls.Lecturer.Phone,
                    })
                    .ToListAsync();

                ViewBag.Lecturers = lecturers;

                return View(student);
            }
            
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,FullName,Class,Major,Email,Phone,CreatedAt")] Student student, IFormFile avatarFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Đặt CreatedAt là thời gian hiện tại
                    student.CreatedAt = DateTime.Now;
                    var user = _context.Users.FirstOrDefault(m => m.UserId == student.UserId);

                    // Xử lý upload avatar nếu có
                    if (avatarFile != null)
                    {
                        var fileFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "img", "avatar");
                        Directory.CreateDirectory(fileFolder);
                        var fileName = Guid.NewGuid() + Path.GetExtension(avatarFile.FileName);
                        var filePath = Path.Combine(fileFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await avatarFile.CopyToAsync(stream);
                        user.Avatar = "/uploads/img/avatar/" + fileName;
                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();


                    }
                    else{
                        user.Avatar = "/uploads/img/avatar/default-avatar.jpg";
                            _context.Users.Update(user);
                            await _context.SaveChangesAsync();
                    }
                    // Gán UserId cho student
                    student.UserId = user.UserId;

                    _context.Add(student);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Thêm sinh viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu sinh viên: " + ex.Message);
                }
            }

            return View(student);
        }


        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                                    .Include(s => s.User)
                                    .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StudentId,UserId,FullName,Class,Major,Email,Phone,CreatedAt")] Student student, IFormFile? avatarFile)
        {
            if (id != student.StudentId)
            {
                return NotFound();
            }

            var studentToUpdate = await _context.Students
                                        .Include(s => s.User)
                                        .FirstOrDefaultAsync(s => s.StudentId == student.StudentId);

            if (studentToUpdate == null)
            {
                return NotFound();
            }

            // Cập nhật các thuộc tính của studentToUpdate từ student model
            studentToUpdate.FullName = student.FullName;
            studentToUpdate.Class = student.Class;
            studentToUpdate.Major = student.Major;
            studentToUpdate.Email = student.Email;
            studentToUpdate.Phone = student.Phone;

            if (ModelState.IsValid)
            {
                try
                {
                    var user = studentToUpdate.User;
                    if (user != null)
                    {
                        // Chỉ xử lý avatar nếu có file mới được tải lên
                        if (avatarFile != null && avatarFile.Length > 0)
                        {
                            // Xóa avatar cũ nếu có và không phải ảnh mặc định
                            if (!string.IsNullOrEmpty(user.Avatar) && !user.Avatar.Contains("default-avatar.jpg"))
                            {
                                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, user.Avatar.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                                if (System.IO.File.Exists(oldPath))
                                {
                                    System.IO.File.Delete(oldPath);
                                }
                            }

                            // Upload avatar mới
                            var fileFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "img", "avatar");
                            Directory.CreateDirectory(fileFolder);

                            var fileName = Guid.NewGuid() + Path.GetExtension(avatarFile.FileName);
                            var filePath = Path.Combine(fileFolder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await avatarFile.CopyToAsync(stream);
                            }

                            user.Avatar = "/uploads/img/avatar/" + fileName;
                            _context.Users.Update(user);
                        }

                        _context.Entry(studentToUpdate).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        TempData["Success"] = "Cập nhật sinh viên thành công!";
                        return RedirectToAction(nameof(Details), new { id = studentToUpdate.UserId });
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu sinh viên: " + ex.Message);
                }
            }

            return View(studentToUpdate);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.StudentId == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                // Xóa liên kết với giảng viên nếu có
                var lecturerLinks = _context.LecturerStudents.Where(ls => ls.StudentId == id);
                _context.LecturerStudents.RemoveRange(lecturerLinks);

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult DeleteSelected(int[] selectedStudents)
        {
            if (selectedStudents == null || selectedStudents.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn sinh viên để xóa.";
                return RedirectToAction("Index");
            }

            foreach (var studentId in selectedStudents)
            {
                var student = _context.Students.Find(studentId);
                if (student != null)
                {
                    var links = _context.LecturerStudents.Where(ls => ls.StudentId == studentId);
                    _context.LecturerStudents.RemoveRange(links);

                    _context.Students.Remove(student);
                }
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentId == id);
        }
    }
}
