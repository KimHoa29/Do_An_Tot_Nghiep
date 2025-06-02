using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class CommentPostsController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CommentPostsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            IQueryable<CommentPost> commentsQuery = _context.CommentPosts
                .Include(c => c.Post)
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User);

            // Nếu không phải admin thì chỉ lấy bình luận của người dùng hiện tại
            if (CurrentUserRole != "Admin")
            {
                commentsQuery = commentsQuery.Where(c => c.UserId.ToString() == CurrentUserID);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                commentsQuery = commentsQuery.Where(c =>
                    c.Content.Contains(searchString) ||
                    c.Post.Title.Contains(searchString) ||
                    c.User.Username.Contains(searchString));
            }

            var rootComments = await commentsQuery
                .Where(c => c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var allReplies = await _context.CommentPosts
                .Include(c => c.User)
                .Where(c => c.ParentCommentId != null)
                .Where(c => CurrentUserRole == "Admin" || c.UserId.ToString() == CurrentUserID) // Admin xem được tất cả replies, user thường chỉ xem được của mình
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            foreach (var comment in rootComments)
            {
                var directReplies = allReplies
                    .Where(r => r.ParentCommentId == comment.CommentPostId)
                    .ToList();

                if (directReplies.Any())
                {
                    comment.Replies = new List<CommentPost>();
                    foreach (var reply in directReplies)
                    {
                        comment.Replies.Add(reply);
                        AddNestedReplies(reply, allReplies);
                    }
                }
            }

            return View(rootComments);
        }

        public IActionResult CreateResponse(int? id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }
            if (id == null)
            {
                return NotFound();
            }

            var parentComment = _context.CommentPosts
                .Include(c => c.User)
                .FirstOrDefault(c => c.CommentPostId == id);

            if (parentComment == null)
            {
                return NotFound();
            }

            ViewBag.ParentComment = parentComment;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResponse([Bind("ParentCommentId,Content,ImageUrl")] CommentPost comment, IFormFile? ImageUpload)
        {
            var parentComment = _context.CommentPosts
                .Include(c => c.User)
                .Include(c => c.Post)
                .FirstOrDefault(m => m.CommentPostId == comment.ParentCommentId);
            
            comment.PostId = parentComment.PostId;
            if (parentComment.ParentCommentId != null)
            {
                var grandfatherComment = parentComment.ParentCommentId;
                comment.ParentCommentId = grandfatherComment;
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(m => m.UserId.ToString().Equals(CurrentUserID));
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận" });
            }
            comment.UserId = currentUser.UserId;
            comment.CreatedAt = DateTime.Now;

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi bình luận" });
            }

            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                var fileName = Path.GetFileNameWithoutExtension(ImageUpload.FileName);
                var extension = Path.GetExtension(ImageUpload.FileName);
                var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/comments");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, uniqueFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageUpload.CopyToAsync(stream);
                }

                comment.ImageUrl = "/uploads/comments/" + uniqueFileName;
            }

            _context.CommentPosts.Add(comment);
            await _context.SaveChangesAsync();

            // Create notification for the parent comment owner
            if (parentComment.UserId != currentUser.UserId)
            {
                var notification = new Notification
                {
                    UserId = parentComment.UserId,
                    Title = "Phản hồi bình luận",
                    Content = $"Có phản hồi mới cho bình luận của bạn",
                    Type = 7, // 6 for post comment
                    CommentPostId = comment.CommentPostId,
                    PostId = parentComment.PostId,
                    Path = $"/Posts/Details/{parentComment.PostId}#comment-{comment.CommentPostId}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            // Load the user information for the new reply
            var replyWithUser = await _context.CommentPosts
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentPostId == comment.CommentPostId);

            return Json(new {
                success = true,
                reply = new {
                    id = replyWithUser.CommentPostId,
                    content = replyWithUser.Content,
                    imageUrl = replyWithUser.ImageUrl,
                    createdAt = replyWithUser.CreatedAt.ToString("HH:mm, dd/MM/yyyy"),
                    username = replyWithUser.User.Username,
                    role = replyWithUser.User.Role,
                    avatar = string.IsNullOrEmpty(replyWithUser.User.Avatar) ? "/css/img/default-avatar.jpg" : replyWithUser.User.Avatar
                }
            });
        }

        private void AddNestedReplies(CommentPost comment, List<CommentPost> allReplies)
        {
            var replies = allReplies
                .Where(r => r.ParentCommentId == comment.CommentPostId)
                .ToList();

            if (replies.Any())
            {
                comment.Replies = new List<CommentPost>();
                foreach (var reply in replies)
                {
                    comment.Replies.Add(reply);
                    AddNestedReplies(reply, allReplies);
                }
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.CommentPosts
                .Include(c => c.Post)
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.CommentPostId == id);

            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        public IActionResult Create()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["PostId"] = new SelectList(_context.Posts, "PostId", "Title");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PostId,Content")] CommentPost comment, IFormFile? ImageUpload)
        {
            if (!IsLogin)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận" });
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(m => m.UserId.ToString().Equals(CurrentUserID));
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận" });
            }

            comment.UserId = currentUser.UserId;
            comment.CreatedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    var fileName = Path.GetFileNameWithoutExtension(ImageUpload.FileName);
                    var extension = Path.GetExtension(ImageUpload.FileName);
                    var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/comments");

                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    var filePath = Path.Combine(uploadPath, uniqueFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageUpload.CopyToAsync(stream);
                    }

                    comment.ImageUrl = "/uploads/comments/" + uniqueFileName;
                }

                _context.Add(comment);
                await _context.SaveChangesAsync();

                // Create notification for post owner
                var post = await _context.Posts
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PostId == comment.PostId);

                if (post != null && post.UserId != currentUser.UserId)
                {
                    var notification = new Notification
                    {
                        UserId = post.UserId,
                        Title = "Bình luận mới",
                        Content = $"Có bình luận mới trên bài viết của bạn: {post.Title}",
                        Type = 1, // 1 for comment
                        CommentId = comment.CommentPostId,
                        PostId = post.PostId,
                        Path = $"/Posts/Details/{post.PostId}#comment-{comment.CommentPostId}",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                // Load the user information for the new comment
                var commentWithUser = await _context.CommentPosts
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CommentPostId == comment.CommentPostId);

                return Json(new {
                    success = true,
                    comment = new {
                        id = commentWithUser.CommentPostId,
                        content = commentWithUser.Content,
                        imageUrl = commentWithUser.ImageUrl,
                        createdAt = commentWithUser.CreatedAt.ToString("HH:mm, dd/MM/yyyy"),
                        username = commentWithUser.User.Username,
                        role = commentWithUser.User.Role,
                        avatar = string.IsNullOrEmpty(commentWithUser.User.Avatar) ? "/css/img/default-avatar.jpg" : commentWithUser.User.Avatar
                    }
                });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi gửi bình luận" });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.CommentPosts.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            ViewData["PostId"] = new SelectList(_context.Posts, "PostId", "Title", comment.PostId);
            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CommentPostId,PostId,Content")] CommentPost comment, IFormFile? ImageUpload)
        {
            if (id != comment.CommentPostId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingComment = await _context.CommentPosts.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CommentPostId == id);

                    if (existingComment == null)
                    {
                        return NotFound();
                    }

                    if (ImageUpload != null && ImageUpload.Length > 0)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(ImageUpload.FileName);
                        var extension = Path.GetExtension(ImageUpload.FileName);
                        var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{extension}";
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/comments");

                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }

                        var filePath = Path.Combine(uploadPath, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageUpload.CopyToAsync(stream);
                        }

                        comment.ImageUrl = "/uploads/comments/" + uniqueFileName;
                    }
                    else
                    {
                        comment.ImageUrl = existingComment.ImageUrl;
                    }

                    comment.CreatedAt = existingComment.CreatedAt;
                    comment.UserId = existingComment.UserId;

                    _context.Update(comment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentPostExists(comment.CommentPostId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Posts", new { id = comment.PostId });
            }

            ViewData["PostId"] = new SelectList(_context.Posts, "PostId", "Title", comment.PostId);
            return View(comment);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.CommentPosts
                .Include(c => c.Post)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.CommentPostId == id);

            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comment = await _context.CommentPosts.FindAsync(id);
            if (comment != null)
            {
                _context.CommentPosts.Remove(comment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(List<int> selectedComments)
        {
            if (selectedComments != null && selectedComments.Count > 0)
            {
                var commentsToDelete = await _context.CommentPosts
                    .Where(c => selectedComments.Contains(c.CommentPostId))
                    .ToListAsync();

                _context.CommentPosts.RemoveRange(commentsToDelete);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        private bool CommentPostExists(int id)
        {
            return _context.CommentPosts.Any(e => e.CommentPostId == id);
        }
    }
}