using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class LecturersController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LecturersController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Lecturers
        public async Task<IActionResult> Index(string searchString)
        {
            var lecturersQuery = _context.Lecturers
                .Include(l => l.User)
                .GroupJoin(
                    _context.LecturerStudents,
                    l => l.LecturerId,
                    ls => ls.LecturerId,
                    (l, ls) => new Lecturer
                    {
                        LecturerId = l.LecturerId,
                        UserId = l.UserId,
                        User = l.User,
                        FullName = l.FullName,
                        Position = l.Position,
                        Department = l.Department,
                        Specialization = l.Specialization,
                        Phone = l.Phone,
                        Email = l.Email,
                        StudentCount = _context.LecturerStudents.Count(ls => ls.LecturerId == l.LecturerId)
                    }
                );

            if (!string.IsNullOrEmpty(searchString))
            {
                lecturersQuery = lecturersQuery.Where(l => l.FullName.Contains(searchString)
                                                           || l.Email.Contains(searchString)
                                                           || l.Position.Contains(searchString));
            }

            var lecturers = await lecturersQuery.ToListAsync();
            return View(lecturers);
        }

        // GET: Lecturers/Details/5
        public async Task<IActionResult> Details(int? id, string searchString)
        {
            if (id == null) return NotFound();

            // Thử tìm theo LecturerId trước
            var lecturer = await _context.Lecturers
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.LecturerId == id);

            // Nếu không tìm thấy theo LecturerId, thử tìm theo UserId
            if (lecturer == null)
            {
                 lecturer = await _context.Lecturers
                    .Include(u => u.User)
                    .FirstOrDefaultAsync(m => m.UserId == id); // Tìm theo UserId
            }

            if (lecturer == null) return NotFound();

            // Lấy danh sách sinh viên thuộc giảng viên này
            var studentsQuery = _context.LecturerStudents
                .Where(ls => ls.LecturerId == lecturer.LecturerId)
                .Include(ls => ls.Student)
                .Select(ls => ls.Student);

            // Áp dụng bộ lọc tìm kiếm nếu có
            if (!string.IsNullOrEmpty(searchString))
            {
                studentsQuery = studentsQuery.Where(s => s.FullName.Contains(searchString)
                                                           || s.Email.Contains(searchString)
                                                           || s.StudentId.ToString().Contains(searchString) // Tìm kiếm cả theo mã SV
                                                           || s.Class.Contains(searchString)); // Tìm kiếm cả theo lớp
            }

            var students = await studentsQuery.ToListAsync();

            ViewBag.Students = students;
            ViewBag.CurrentSearchString = searchString; // Lưu lại giá trị tìm kiếm

            return View(lecturer);
        }

        // GET: Lecturers/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,FullName,Position,Department,Specialization,Phone,Email,CreatedAt")] Lecturer lecturer, IFormFile? avatarFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    lecturer.CreatedAt = DateTime.Now;

                    var user = _context.Users.FirstOrDefault(m => m.UserId == lecturer.UserId);

                    if (avatarFile != null)
                    {
                        var fileFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "img", "avatar");
                        Directory.CreateDirectory(fileFolder);
                        var fileName = Guid.NewGuid() + Path.GetExtension(avatarFile.FileName);
                        var filePath = Path.Combine(fileFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await avatarFile.CopyToAsync(stream);

                        user.Avatar = "/uploads/img/avatar/" + fileName;
                    }
                    else
                    {
                        user.Avatar = "/uploads/img/avatar/default-avatar.jpg";
                    }

                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    lecturer.UserId = user.UserId;
                    _context.Add(lecturer);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Thêm giảng viên thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu giảng viên: " + ex.Message);
                }
            }

            return View(lecturer);
        }

        // GET: Lecturers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lecturer = await _context.Lecturers
                                    .Include(l => l.User)
                                    .FirstOrDefaultAsync(l => l.LecturerId == id);

            if (lecturer == null)
            {
                return NotFound();
            }
            return View(lecturer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("LecturerId,UserId,FullName,Position,Department,Specialization,Phone,Email,CreatedAt")] Lecturer lecturer, IFormFile? avatarFile)
        {
            if (id != lecturer.LecturerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == lecturer.UserId);
                    if (user != null)
                    {
                        if (avatarFile != null)
                        {
                            // Xóa avatar cũ nếu không phải mặc định
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
                        }
                        else if (string.IsNullOrEmpty(user.Avatar))
                        {
                            user.Avatar = "/uploads/img/avatar/default-avatar.jpg";
                        }

                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();
                    }

                    _context.Update(lecturer);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật giảng viên thành công!";
                    return RedirectToAction(nameof(Details), new { id = lecturer.LecturerId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LecturerExists(lecturer.LecturerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(lecturer);
        }

        // POST: Lecturers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Xóa các bản ghi liên kết trước
            var relatedStudents = await _context.LecturerStudents
                .Where(ls => ls.LecturerId == id)
                .ToListAsync();

            if (relatedStudents.Any())
            {
                _context.LecturerStudents.RemoveRange(relatedStudents);
            }

            // Sau đó mới xóa giảng viên
            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer != null)
            {
                _context.Lecturers.Remove(lecturer);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> DeleteSelected(int[] selectedLecturers)
        {
            if (selectedLecturers == null || selectedLecturers.Length == 0)
            {
                return RedirectToAction(nameof(Index));
            }

            foreach (var lecturerId in selectedLecturers)
            {
                // Xóa các bản ghi liên kết trước
                var linkedStudents = await _context.LecturerStudents
                    .Where(ls => ls.LecturerId == lecturerId)
                    .ToListAsync();

                if (linkedStudents.Any())
                {
                    _context.LecturerStudents.RemoveRange(linkedStudents);
                }

                var lecturer = await _context.Lecturers.FindAsync(lecturerId);
                if (lecturer != null)
                {
                    _context.Lecturers.Remove(lecturer);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> AddStudent(int lecturerId)
        {
            if (lecturerId == 0) return NotFound();

            var lecturer = await _context.Lecturers
                .Where(l => l.LecturerId == lecturerId)
                .Select(l => new AddStudentViewModel
                {
                    LecturerId = l.LecturerId,
                    FullName = l.FullName,
                    Students = _context.Students
                                    .Where(s => !_context.LecturerStudents.Any(ls => ls.LecturerId == lecturerId && ls.StudentId == s.StudentId))
                                    .ToList()
                })
                .FirstOrDefaultAsync();

            if (lecturer == null) return NotFound();

            return View(lecturer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudent(AddStudentViewModel model)
        {
            if (!model.SelectedStudentIds.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một sinh viên.");
                return View(model);
            }

            foreach (var studentId in model.SelectedStudentIds)
            {
                bool exists = await _context.LecturerStudents
                    .AnyAsync(ls => ls.LecturerId == model.LecturerId && ls.StudentId == studentId);

                if (!exists)
                {
                    _context.LecturerStudents.Add(new LecturerStudent
                    {
                        LecturerId = model.LecturerId,
                        StudentId = studentId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = model.LecturerId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int lecturerId, int studentId)
        {
            var lecturerStudent = await _context.LecturerStudents
                .FirstOrDefaultAsync(ls => ls.LecturerId == lecturerId && ls.StudentId == studentId);

            if (lecturerStudent != null)
            {
                _context.LecturerStudents.Remove(lecturerStudent);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = lecturerId });
        }

        private bool LecturerExists(int id)
        {
            return _context.Lecturers.Any(e => e.LecturerId == id);
        }
    }
}
