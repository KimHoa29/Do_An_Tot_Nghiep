using Do_An_Tot_Nghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class ProfileController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(AppDbContext context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? userId)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            // Nếu không có userId được chỉ định, sử dụng ID của người dùng hiện tại
            var targetUserId = userId ?? int.Parse(CurrentUserID);
            
            // Lấy thông tin người dùng và student
            var user = await _context.Users
                .Include(u => u.Student) // Include thông tin Student
                .FirstOrDefaultAsync(u => u.UserId == targetUserId);

            if (user == null)
            {
                return NotFound();
            }

            // Lấy các bài đăng của người dùng
            var topics = await _context.Topics
                .Include(t => t.User)
                .Include(t => t.TopicTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(t => t.LikeTopics)
                .Include(t => t.Saves)
                .Where(t => t.UserId == targetUserId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var posts = await _context.Posts
                .Include(t => t.User)
                .Include(t => t.CommentPosts)
                    .ThenInclude(c => c.User)
                .Include(t => t.CommentPosts)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(t => t.LikePosts)
                .Include(t => t.PostMentions)
                    .ThenInclude(pm => pm.User)
                .Include(t => t.Saves)
                .Where(t => t.UserId == targetUserId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.User = user;
            ViewBag.Topics = topics;
            ViewBag.Posts = posts;
            ViewBag.CurrentUserId = CurrentUserID;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LoadMoreTopics(int userId, int skip)
        {
            if (!IsLogin)
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var topics = await _context.Topics
                .Include(t => t.User)
                .Include(t => t.TopicTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(t => t.LikeTopics)
                .Include(t => t.Saves)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip(skip)
                .Take(4)
                .ToListAsync();

            if (!topics.Any())
            {
                return Json(new { success = false, message = "No more topics" });
            }

            var html = "";
            foreach (var topic in topics)
            {
                html += await this.RenderViewAsync("_Topic", topic, true);
            }

            return Json(new { success = true, html = html });
        }

        [HttpGet]
        public async Task<IActionResult> LoadMorePosts(int userId, int skip)
        {
            if (!IsLogin)
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var posts = await _context.Posts
                .Include(t => t.User)
                .Include(t => t.CommentPosts)
                    .ThenInclude(c => c.User)
                .Include(t => t.CommentPosts)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(t => t.LikePosts)
                .Include(t => t.PostMentions)
                    .ThenInclude(pm => pm.User)
                .Include(t => t.Saves)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip(skip)
                .Take(4)
                .ToListAsync();

            if (!posts.Any())
            {
                return Json(new { success = false, message = "No more posts" });
            }

            var html = "";
            foreach (var post in posts)
            {
                html += await this.RenderViewAsync("_Post", post, true);
            }

            return Json(new { success = true, html = html });
        }
    }
} 