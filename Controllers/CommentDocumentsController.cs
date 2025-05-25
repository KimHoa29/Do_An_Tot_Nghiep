// Controllers/DocumentCommentsController.cs
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
    public class CommentDocumentsController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CommentDocumentsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: DocumentComments
        public async Task<IActionResult> Index(string searchString)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            IQueryable<CommentDocument> commentsQuery = _context.CommentDocuments
                .Include(c => c.Document)
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User);

            if (!string.IsNullOrEmpty(searchString))
            {
                commentsQuery = commentsQuery.Where(c =>
                    c.Content.Contains(searchString) ||
                    c.Document.Title.Contains(searchString) ||
                    c.User.Username.Contains(searchString));
            }

            var rootComments = await commentsQuery
                .Where(c => c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var allReplies = await _context.CommentDocuments
                .Include(c => c.User)
                .Where(c => c.ParentCommentId != null)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            foreach (var comment in rootComments)
            {
                var directReplies = allReplies
                    .Where(r => r.ParentCommentId == comment.CommentDocumentId)
                    .ToList();

                if (directReplies.Any())
                {
                    comment.Replies = new List<CommentDocument>();
                    foreach (var reply in directReplies)
                    {
                        comment.Replies.Add(reply);
                        AddNestedReplies(reply, allReplies);
                    }
                }
            }

            return View(rootComments);
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

            var parentComment = _context.CommentDocuments
                .Include(c => c.User)
                .FirstOrDefault(c => c.CommentDocumentId == id);

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
        public async Task<IActionResult> CreateResponse([Bind("ParentCommentId,Content,ImageUrl")] CommentDocument comment, IFormFile? ImageUpload)
        {
            if (!IsLogin)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var parentComment = _context.CommentDocuments
                .Include(c => c.User)
                .Include(c => c.Document)
                .FirstOrDefault(m => m.CommentDocumentId == comment.ParentCommentId);
            
            if (parentComment == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bình luận gốc" });
            }

            comment.DocumentId = parentComment.DocumentId;
            if (parentComment.ParentCommentId != null)
            {
                var grandfatherComment = parentComment.ParentCommentId;
                comment.ParentCommentId = grandfatherComment;
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(m => m.UserId.ToString().Equals(CurrentUserID));
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
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

                _context.CommentDocuments.Add(comment);
                await _context.SaveChangesAsync();

                // Create notification for the parent comment owner
                if (parentComment.UserId != currentUser.UserId)
                {
                    var notification = new Notification
                    {
                        UserId = parentComment.UserId,
                        Title = "Phản hồi bình luận",
                        Content = $"Có phản hồi mới cho bình luận của bạn",
                        Type = 5,
                        CommentId = comment.CommentDocumentId,
                        DocumentId = parentComment.DocumentId,
                        Path = $"/Documents/Details/{parentComment.DocumentId}#comment-{comment.CommentDocumentId}",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                // Load the reply with user information for the response
                var newReply = await _context.CommentDocuments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CommentDocumentId == comment.CommentDocumentId);

                return Json(new { 
                    success = true, 
                    reply = new {
                        id = newReply.CommentDocumentId,
                        content = newReply.Content,
                        imageUrl = newReply.ImageUrl,
                        createdAt = newReply.CreatedAt.ToString("HH:mm, dd/MM/yyyy"),
                        username = newReply.User.Username,
                        role = newReply.User.Role,
                        avatar = string.IsNullOrEmpty(newReply.User.Avatar) ? "/css/img/default-avatar.jpg" : newReply.User.Avatar
                    }
                });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi thêm phản hồi" });
        }
        private void AddNestedReplies(CommentDocument comment, List<CommentDocument> allReplies)
        {
            var replies = allReplies
                .Where(r => r.ParentCommentId == comment.CommentDocumentId)
                .ToList();

            if (replies.Any())
            {
                comment.Replies = new List<CommentDocument>();
                foreach (var reply in replies)
                {
                    comment.Replies.Add(reply);
                    AddNestedReplies(reply, allReplies);
                }
            }
        }

        // GET: DocumentComments/Details/5
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

            var comment = await _context.CommentDocuments
                .Include(c => c.Document)
                .Include(c => c.User)
                .Include(c => c.Replies)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.CommentDocumentId == id);

            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // GET: DocumentComments/Create
        public IActionResult Create()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["DocumentId"] = new SelectList(_context.Documents, "DocumentId", "Title");
            return View();
        }

        // POST: DocumentComments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DocumentId,Content")] CommentDocument comment, IFormFile? ImageUpload)
        {
            if (!IsLogin)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var currentUser = await _context.Users.FirstOrDefaultAsync(m => m.UserId.ToString().Equals(CurrentUserID));
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
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

                // Create notification for document owner
                var document = await _context.Documents
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.DocumentId == comment.DocumentId);

                if (document != null && document.UserId != currentUser.UserId)
                {
                    var notification = new Notification
                    {
                        UserId = document.UserId,
                        Title = "Bình luận mới",
                        Content = $"Có bình luận mới trên tài liệu của bạn: {document.Title}",
                        Type = 1,
                        CommentId = comment.CommentDocumentId,
                        DocumentId = document.DocumentId,
                        Path = $"/Documents/Details/{document.DocumentId}#comment-{comment.CommentDocumentId}",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }

                // Load the comment with user information for the response
                var newComment = await _context.CommentDocuments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CommentDocumentId == comment.CommentDocumentId);

                return Json(new { 
                    success = true, 
                    comment = new {
                        id = newComment.CommentDocumentId,
                        content = newComment.Content,
                        imageUrl = newComment.ImageUrl,
                        createdAt = newComment.CreatedAt.ToString("HH:mm, dd/MM/yyyy"),
                        username = newComment.User.Username,
                        role = newComment.User.Role,
                        avatar = string.IsNullOrEmpty(newComment.User.Avatar) ? "/css/img/default-avatar.jpg" : newComment.User.Avatar
                    }
                });
            }

            return Json(new { success = false, message = "Có lỗi xảy ra khi thêm bình luận" });
        }

        // GET: DocumentComments/Edit/5
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

            var comment = await _context.CommentDocuments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }

            ViewData["DocumentId"] = new SelectList(_context.Documents, "DocumentId", "Title", comment.DocumentId);
            return View(comment);
        }

        // POST: DocumentComments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CommentDocumentId,DocumentId,Content")] CommentDocument comment, IFormFile? ImageUpload)
        {
            if (id != comment.CommentDocumentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingComment = await _context.CommentDocuments.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CommentDocumentId == id);

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
                    if (!CommentDocumentExists(comment.CommentDocumentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Documents", new { id = comment.DocumentId });
            }

            ViewData["DocumentId"] = new SelectList(_context.Documents, "DocumentId", "Title", comment.DocumentId);
            return View(comment);
        }

        // GET: DocumentComments/Delete/5
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

            var comment = await _context.CommentDocuments
                .Include(c => c.Document)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.CommentDocumentId == id);

            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // POST: DocumentComments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comment = await _context.CommentDocuments.FindAsync(id);
            if (comment != null)
            {
                _context.CommentDocuments.Remove(comment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Documents", new { id = comment.DocumentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(List<int> selectedComments)
        {
            if (selectedComments != null && selectedComments.Count > 0)
            {
                var commentsToDelete = await _context.CommentDocuments
                    .Where(c => selectedComments.Contains(c.CommentDocumentId))
                    .ToListAsync();

                _context.CommentDocuments.RemoveRange(commentsToDelete);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CommentDocumentExists(int id)
        {
            return _context.CommentDocuments.Any(e => e.CommentDocumentId == id);
        }
    }
}