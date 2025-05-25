using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;
using NuGet.Packaging;
using Do_An_Tot_Nghiep.Extensions;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class CommentsController : BaseController
    {
        private readonly AppDbContext _context;

        public CommentsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            // Lấy tất cả comment và include User, Topic
            IQueryable<Comment> commentsQuery = _context.Comments
                .Include(c => c.Topic)
                .Include(c => c.User)
                .Include(c => c.Replies) // comment con
                    .ThenInclude(r => r.User);

            // Áp dụng tìm kiếm nếu có
            if (!string.IsNullOrEmpty(searchString))
            {
                commentsQuery = commentsQuery.Where(c =>
                    c.Content.Contains(searchString) ||
                    c.Topic.Title.Contains(searchString) ||
                    c.User.Username.Contains(searchString));
            }

            // Lấy tất cả comment gốc (ParentCommentId == null)
            var rootComments = await commentsQuery
                .Where(c => c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Lấy tất cả comment con (ParentCommentId != null)
            var allReplies = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.ParentCommentId != null)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            // Tổ chức các reply theo cấu trúc cây
            foreach (var comment in rootComments)
            {
                // Lấy replies trực tiếp của comment này
                var directReplies = allReplies
                    .Where(r => r.ParentCommentId == comment.CommentId)
                    .ToList();

                if (directReplies.Any())
                {
                    comment.Replies = new List<Comment>();
                    foreach (var reply in directReplies)
                    {
                        comment.Replies.Add(reply);
                        // Đệ quy để lấy các replies của reply này
                        AddNestedReplies(reply, allReplies);
                    }
                }
            }

            return View(rootComments);
        }

        private void AddNestedReplies(Comment comment, List<Comment> allReplies)
        {
            var replies = allReplies
                .Where(r => r.ParentCommentId == comment.CommentId)
                .ToList();

            if (replies.Any())
            {
                comment.Replies = new List<Comment>();
                foreach (var reply in replies)
                {
                    comment.Replies.Add(reply);
                    // Tiếp tục đệ quy cho các reply con
                    AddNestedReplies(reply, allReplies);
                }
            }
        }

        // Hiển thị form trả lời comment
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

            var parentComment = _context.Comments
                .Include(c => c.User)
                .FirstOrDefault(c => c.CommentId == id);

            if (parentComment == null)
            {
                return NotFound();
            }

            ViewBag.ParentComment = parentComment;
            //var responseComment = new Comment
            //{
            //    ParentCommentId = id
            //};

            return View(); // đảm bảo tên view là CreateResponse.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResponse([Bind("ParentCommentId,Content,ImageUrl")] Comment comment, IFormFile? ImageUpload)
        {
            var parentComment = _context.Comments
                .Include(c => c.User)
                .Include(c => c.Topic)
                .FirstOrDefault(m => m.CommentId == comment.ParentCommentId);
            comment.TopicId = parentComment.TopicId;
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

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Create notification for the parent comment owner
            if (parentComment.UserId != currentUser.UserId)
            {
                var notification = new Notification
                {
                    UserId = parentComment.UserId,
                    Title = "Phản hồi bình luận",
                    Content = $"Có phản hồi mới cho bình luận của bạn",
                    Type = 1, // 1 for comment
                    CommentId = comment.CommentId,
                    TopicId = parentComment.TopicId,
                    Path = $"/Topics/Details/{parentComment.TopicId}#comment-{comment.CommentId}",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            // Load the user information for the new comment
            var commentWithUser = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == comment.CommentId);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    html = await this.RenderViewToStringAsync("_CommentPartial", commentWithUser),
                    commentId = comment.CommentId,
                    commentCount = await _context.Comments.Where(c => c.TopicId == comment.TopicId).CountAsync()
                });
            }

            return RedirectToAction("Details", "Topics", new { id = comment.TopicId });
        }


        // GET: Comments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.Topic)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.CommentId == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }


        // GET: Comments/Create
        public IActionResult Create()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }
            ViewData["TopicId"] = new SelectList(_context.Topics, "TopicId", "Title");
            return View();
        }

        // POST: Comments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TopicId,Content")] Comment comment, IFormFile? ImageUpload)
        {
            if (!IsLogin)
                return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận" });

            var currentUser = await _context.Users.FirstOrDefaultAsync(m => m.UserId.ToString().Equals(CurrentUserID));
            if (currentUser == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận" });

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

                // Create notification for the topic owner
                var topic = await _context.Topics
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.TopicId == comment.TopicId);

                if (topic != null && topic.UserId != currentUser.UserId)
                {
                    var notification = new Notification
                    {
                        UserId = topic.UserId,
                        Title = "Bình luận mới",
                        Content = $"Có bình luận mới trên bài viết của bạn: {topic.Title}",
                        Type = 1,
                        CommentId = comment.CommentId,
                        TopicId = topic.TopicId,
                        Path = $"/Topics/Details/{topic.TopicId}#comment-{comment.CommentId}",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                // Load the user information for the new comment
                var commentWithUser = await _context.Comments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CommentId == comment.CommentId);

                // Trả về JSON object cho AJAX
                return Json(new {
                    success = true,
                    comment = new {
                        id = commentWithUser.CommentId,
                        content = commentWithUser.Content,
                        imageUrl = commentWithUser.ImageUrl,
                        createdAt = commentWithUser.CreatedAt.ToString("HH:mm, dd/MM/yyyy"),
                        username = commentWithUser.User.Username,
                        role = commentWithUser.User.Role,
                        avatar = string.IsNullOrEmpty(commentWithUser.User.Avatar) ? "/css/img/default-avatar.jpg" : commentWithUser.User.Avatar
                    }
                });
            }

            // Nếu có lỗi ModelState
            return Json(new { success = false, message = "Có lỗi xảy ra khi gửi bình luận" });
        }

        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            ViewData["TopicId"] = new SelectList(_context.Topics, "TopicId", "Title", comment.TopicId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username", comment.UserId);
            return View(comment);
        }

        // POST: Comments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CommentId,TopicId,UserId,Content,CreatedAt,ImageUrl")] Comment comment, IFormFile? ImageUpload)
        {
            if (id != comment.CommentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy bình luận hiện tại từ cơ sở dữ liệu
                    var existingComment = await _context.Comments.AsNoTracking().FirstOrDefaultAsync(c => c.CommentId == id);
                    if (existingComment == null)
                    {
                        return NotFound();
                    }

                    // Nếu có ảnh mới, upload ảnh và cập nhật ImageUrl
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
                        // Giữ nguyên ảnh cũ nếu không tải ảnh mới lên
                        comment.ImageUrl = existingComment.ImageUrl;
                    }

                    // Giữ nguyên ngày tạo
                    comment.CreatedAt = existingComment.CreatedAt;

                    _context.Update(comment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.CommentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["TopicId"] = new SelectList(_context.Topics, "TopicId", "Title", comment.TopicId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username", comment.UserId);
            return View(comment);
        }



        // GET: Comments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.Topic)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.CommentId == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
            {
                _context.Comments.Remove(comment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Comments/DeleteSelected
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(List<int> selectedComments)
        {
            if (selectedComments != null && selectedComments.Count > 0)
            {
                var commentsToDelete = await _context.Comments
                    .Where(c => selectedComments.Contains(c.CommentId))
                    .ToListAsync();

                _context.Comments.RemoveRange(commentsToDelete);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        private bool CommentExists(int id)
        {
            return _context.Comments.Any(e => e.CommentId == id);
        }
    }
}