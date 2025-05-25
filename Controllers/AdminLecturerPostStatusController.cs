using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;
using Do_An_Tot_Nghiep.Models.ViewModels;
using System.Collections.Generic;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class AdminLecturerPostStatusController : BaseController
    {
        private readonly AppDbContext _context;

        public AdminLecturerPostStatusController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string status = null, string lecturerName = null)
        {
            var data = await _context.Posts
                .Include(p => p.PostMentions).ThenInclude(pm => pm.User)
                .Include(p => p.CommentPosts)
                .ToListAsync();

            var lecturers = await _context.Lecturers.Include(l => l.User).ToListAsync();
            var result = new List<AdminLecturerPostStatusViewModel>();

            foreach (var post in data)
            {
                foreach (var mention in post.PostMentions)
                {
                    var lecturer = lecturers.FirstOrDefault(l => l.UserId == mention.UserId);
                    if (lecturer == null) continue;

                    var hasResponded = post.CommentPosts.Any(c => c.UserId == mention.UserId);
                    var responseTime = hasResponded
                        ? post.CommentPosts.Where(c => c.UserId == mention.UserId).OrderByDescending(c => c.CreatedAt).FirstOrDefault()?.CreatedAt
                        : null;

                    // Lọc theo trạng thái
                    if (status == "responded" && !hasResponded) continue;
                    if (status == "pending" && hasResponded) continue;

                    // Lọc theo giảng viên
                    if (!string.IsNullOrEmpty(lecturerName) && !lecturer.FullName.Contains(lecturerName, StringComparison.OrdinalIgnoreCase)) continue;

                    result.Add(new AdminLecturerPostStatusViewModel
                    {
                        PostId = post.PostId,
                        PostTitle = post.Title,
                        PostCreatedAt = post.CreatedAt ?? DateTime.Now,
                        LecturerId = lecturer.LecturerId,
                        LecturerName = lecturer.FullName,
                        LecturerDepartment = lecturer.Department,
                        HasResponded = hasResponded,
                        ResponseTime = responseTime
                    });
                }
            }

            ViewBag.StatusFilter = status;
            ViewBag.LecturerList = lecturers.Select(l => l.FullName).Distinct().OrderBy(n => n).ToList();
            return View(result);
        }
    }
}
