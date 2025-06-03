using Do_An_Tot_Nghiep.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Do_An_Tot_Nghiep.Controllers
{
    public static class ControllerExtensions
    {
        public static async Task<string> RenderViewAsync<TModel>(this Controller controller, string viewName, TModel model, bool partial = false)
        {
            if (string.IsNullOrEmpty(viewName))
            {
                viewName = controller.ControllerContext.ActionDescriptor.ActionName;
            }

            controller.ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                IViewEngine viewEngine = controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as ICompositeViewEngine;
                ViewEngineResult viewResult = viewEngine.FindView(controller.ControllerContext, viewName, !partial);

                if (!viewResult.Success)
                {
                    throw new InvalidOperationException($"A view with the name {viewName} could not be found");
                }

                ViewContext viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);

                return writer.GetStringBuilder().ToString();
            }
        }
    }

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

            var currentUserId = CurrentUserID;
            
            // Cache key for each type of content
            var topicsCacheKey = $"topics_{currentUserId}";
            var documentsCacheKey = $"documents_{currentUserId}";
            var postsCacheKey = $"posts_{currentUserId}";

            // Try to get from cache first
            var topics = await _context.Topics
                .Include(t => t.User)
                .Include(t => t.TopicTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.TopicGroups)
                    .ThenInclude(tg => tg.Group)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.LikeTopics)
                .Include(t => t.Saves)
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
                .Include(t => t.CommentDocuments)
                    .ThenInclude(c => c.User)
                .Include(t => t.LikeDocuments)
                .Include(t => t.Saves)
                .Where(t =>
                    t.VisibilityType == "public" ||
                    t.UserId.ToString() == currentUserId ||
                    (t.VisibilityType == "private" && t.UserId.ToString() == currentUserId) ||
                    (t.VisibilityType == "group" && t.DocumentGroups.Any(dg => 
                        dg.Group.GroupUsers.Any(gu => gu.UserId.ToString() == currentUserId))) ||
                    (t.VisibilityType == "custom" && t.DocumentUsers.Any(du => du.UserId.ToString() == currentUserId))
                )
                .OrderByDescending(t => t.CreatedAt)
                .Take(4)
                .ToListAsync();

            var posts = await _context.Posts
                .Include(t => t.User)
                .Include(t => t.PostGroups)
                    .ThenInclude(pg => pg.Group)
                .Include(t => t.CommentPosts)
                    .ThenInclude(c => c.User)
                .Include(t => t.LikePosts)
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

            ViewBag.SelectedTab = tab;
            ViewBag.document = documents;
            ViewBag.topic = topics;
            ViewBag.post = posts;
            ViewBag.currentUserID = CurrentUserID;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LoadMoreDocuments(int skip)
        {
            if (!IsLogin)
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var currentUserId = CurrentUserID;
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
                .Include(t => t.Saves)
                .Where(t =>
                    t.VisibilityType == "public" ||
                    t.UserId.ToString() == currentUserId ||
                    (t.VisibilityType == "private" && t.UserId.ToString() == currentUserId) ||
                    (t.VisibilityType == "group" && t.DocumentGroups.Any(dg => 
                        dg.Group.GroupUsers.Any(gu => gu.UserId.ToString() == currentUserId))) ||
                    (t.VisibilityType == "custom" && t.DocumentUsers.Any(du => du.UserId.ToString() == currentUserId))
                )
                .OrderByDescending(t => t.CreatedAt)
                .Skip(skip)
                .Take(4)
                .ToListAsync();

            if (!documents.Any())
            {
                return Json(new { success = false, message = "No more documents" });
            }

            var html = "";
            foreach (var doc in documents)
            {
                html += await this.RenderViewAsync("_Document", doc, true);
            }

            return Json(new { success = true, html = html });
        }

        [HttpGet]
        public async Task<IActionResult> LoadMorePosts(int skip)
        {
            if (!IsLogin)
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var currentUserId = CurrentUserID;
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
                .Include(t => t.Saves)
                .Where(t =>
                    t.VisibilityType == "public" ||
                    t.UserId.ToString() == currentUserId ||
                    (t.VisibilityType == "private" && t.UserId.ToString() == currentUserId) ||
                    (t.VisibilityType == "group" && t.PostGroups.Any(pg => 
                        pg.Group.GroupUsers.Any(gu => gu.UserId.ToString() == currentUserId))) ||
                    (t.VisibilityType == "custom" && t.PostUsers.Any(pu => pu.UserId.ToString() == currentUserId))
                )
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

        [HttpGet]
        public async Task<IActionResult> LoadMoreTopics(int skip)
        {
            if (!IsLogin)
            {
                return Json(new { success = false, message = "Please login first" });
            }

            var currentUserId = CurrentUserID;
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
                .Include(t => t.Saves)
                .Where(t =>
                    t.VisibilityType == "public" ||
                    t.UserId.ToString() == currentUserId ||
                    (t.VisibilityType == "private" && t.UserId.ToString() == currentUserId) ||
                    (t.VisibilityType == "group" && t.TopicGroups.Any(tg => 
                        tg.Group.GroupUsers.Any(gu => gu.UserId.ToString() == currentUserId))) ||
                    (t.VisibilityType == "custom" && t.TopicUsers.Any(tu => tu.UserId.ToString() == currentUserId))
                )
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
