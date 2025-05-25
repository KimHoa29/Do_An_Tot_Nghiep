using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;
using Do_An_Tot_Nghiep.Models.ViewModels;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class LecturerStatisticsController : BaseController
    {
        private readonly AppDbContext _context;

        public LecturerStatisticsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy ID của user đang đăng nhập
            var currentUserId = CurrentUserID;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin giảng viên đang đăng nhập
            var lecturer = await _context.Lecturers
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId.ToString() == currentUserId);

            if (lecturer == null)
            {
                return NotFound("Không tìm thấy thông tin giảng viên");
            }

            var lecturerStats = new LecturerPostStatisticsViewModel
            {
                LecturerId = lecturer.LecturerId,
                FullName = lecturer.FullName,
                Department = lecturer.Department,
                Position = lecturer.Position
            };

            // Lấy tất cả bài post mà giảng viên được mention
            var mentionedPosts = await _context.Posts
                .Include(p => p.PostMentions)
                .Include(p => p.CommentPosts)
                .Where(p => p.PostMentions.Any(pm => pm.UserId == lecturer.UserId))
                .ToListAsync();

            lecturerStats.TotalAssignedPosts = mentionedPosts.Count;

            foreach (var post in mentionedPosts)
            {
                var hasResponded = post.CommentPosts.Any(c => c.UserId == lecturer.UserId);
                var responseTime = hasResponded 
                    ? post.CommentPosts.Where(c => c.UserId == lecturer.UserId)
                        .OrderByDescending(c => c.CreatedAt)
                        .FirstOrDefault()?.CreatedAt
                    : null;

                lecturerStats.PostDetails.Add(new PostStatistics
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    CreatedAt = post.CreatedAt ?? DateTime.Now,
                    HasResponded = hasResponded,
                    ResponseTime = responseTime
                });

                if (hasResponded)
                {
                    lecturerStats.RespondedPosts++;
                }
            }

            lecturerStats.PendingPosts = lecturerStats.TotalAssignedPosts - lecturerStats.RespondedPosts;

            // Chuyển thành list để giữ nguyên model của view
            var statistics = new List<LecturerPostStatisticsViewModel> { lecturerStats };
            return View(statistics);
        }
    }
} 