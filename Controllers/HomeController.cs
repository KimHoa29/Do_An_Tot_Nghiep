using Do_An_Tot_Nghiep.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Security.Claims;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(string tab)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                var currentUserId = CurrentUserID;
                _logger.LogInformation($"Current User ID: {currentUserId}");

                var topics = await _context.Topics
                .Include(t => t.User)
                .Include(t => t.TopicTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.TopicGroups)
                    .ThenInclude(tg => tg.Group)
                        .ThenInclude(g => g.GroupUsers)
                .Include(t => t.TopicUsers)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(t => t.LikeTopics)
                .Where(t =>
                    t.VisibilityType == "public" ||
                    t.UserId.ToString() == currentUserId ||
                    (t.VisibilityType == "private" && t.UserId.ToString() == currentUserId) ||
                    (t.VisibilityType == "group" && t.TopicGroups.Any(tg => 
                        tg.Group.GroupUsers.Any(gu => gu.UserId.ToString() == currentUserId))) ||
                    (t.VisibilityType == "custom" && t.TopicUsers.Any(tu => tu.UserId.ToString() == currentUserId))
                )
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

                var documents = await _context.Documents
                .Include(t => t.User)
                .Include(t => t.DocumentGroups)
                    .ThenInclude(dg => dg.Group)
                        .ThenInclude(g => g.GroupUsers)
                .Include(t => t.DocumentUsers)
                .Include(t => t.CommentDocuments)
                    .ThenInclude(c => c.User)
                .Include(t => t.CommentDocuments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(t => t.LikeDocuments)
                .Where(t =>
                    t.VisibilityType == "public" ||
                    t.UserId.ToString() == currentUserId ||
                    (t.VisibilityType == "private" && t.UserId.ToString() == currentUserId) ||
                    (t.VisibilityType == "group" && t.DocumentGroups.Any(dg => 
                        dg.Group.GroupUsers.Any(gu => gu.UserId.ToString() == currentUserId))) ||
                    (t.VisibilityType == "custom" && t.DocumentUsers.Any(du => du.UserId.ToString() == currentUserId))
                )
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

                var posts = await _context.Posts
                .Include(t => t.User)
                .Include(t => t.PostGroups)
                    .ThenInclude(pg => pg.Group)
                        .ThenInclude(g => g.GroupUsers)
                .Include(t => t.PostUsers)
                .Include(t => t.CommentPosts)
                    .ThenInclude(c => c.User)
                .Include(t => t.CommentPosts)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(t => t.LikePosts)
                    .ThenInclude(l => l.User)
                .Include(t => t.PostMentions)
                    .ThenInclude(pm => pm.User)
                .Where(t =>
                    t.VisibilityType == "public" ||
                    t.UserId.ToString() == currentUserId ||
                    (t.VisibilityType == "private" && t.UserId.ToString() == currentUserId) ||
                    (t.VisibilityType == "group" && t.PostGroups.Any(pg => 
                        pg.Group.GroupUsers.Any(gu => gu.UserId.ToString() == currentUserId))) ||
                    (t.VisibilityType == "custom" && t.PostUsers.Any(pu => pu.UserId.ToString() == currentUserId))
                )
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

                // Log thông tin chi tiết về các truy vấn
                foreach (var topic in topics)
                {
                    _logger.LogInformation($"Topic found - ID: {topic.TopicId}, Type: {topic.VisibilityType}, Creator: {topic.UserId}");
                    if (topic.VisibilityType == "group")
                    {
                        foreach (var tg in topic.TopicGroups)
                        {
                            _logger.LogInformation($"Topic {topic.TopicId} belongs to group {tg.GroupId}");
                            var groupMembers = tg.Group.GroupUsers.Select(gu => gu.UserId).ToList();
                            _logger.LogInformation($"Group {tg.GroupId} members: {string.Join(", ", groupMembers)}");
                        }
                    }
                }

                foreach (var doc in documents)
                {
                    _logger.LogInformation($"Document found - ID: {doc.DocumentId}, Type: {doc.VisibilityType}, Creator: {doc.UserId}");
                    if (doc.VisibilityType == "group")
                    {
                        foreach (var dg in doc.DocumentGroups)
                        {
                            _logger.LogInformation($"Document {doc.DocumentId} belongs to group {dg.GroupId}");
                            var groupMembers = dg.Group.GroupUsers.Select(gu => gu.UserId).ToList();
                            _logger.LogInformation($"Group {dg.GroupId} members: {string.Join(", ", groupMembers)}");
                        }
                    }
                }

                foreach (var post in posts)
                {
                    _logger.LogInformation($"Post found - ID: {post.PostId}, Type: {post.VisibilityType}, Creator: {post.UserId}");
                    if (post.VisibilityType == "group")
                    {
                        foreach (var pg in post.PostGroups)
                        {
                            _logger.LogInformation($"Post {post.PostId} belongs to group {pg.GroupId}");
                            var groupMembers = pg.Group.GroupUsers.Select(gu => gu.UserId).ToList();
                            _logger.LogInformation($"Group {pg.GroupId} members: {string.Join(", ", groupMembers)}");
                        }
                    }
                }

                ViewBag.SelectedTab = tab;
                ViewBag.document = documents;
                ViewBag.topic = topics;
                ViewBag.post = posts;
                ViewBag.currentUserID = CurrentUserID;
                return View();
            }
        }

        public IActionResult Chat()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
