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
    public class PostsController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PostsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
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
            else
            {
                var query = _context.Posts
                    .Include(t => t.User)
                    .Include(t => t.PostGroups)
                    .Include(t => t.PostUsers)
                    .Include(t => t.CommentPosts)
                        .ThenInclude(c => c.User)
                    .AsQueryable();

                // Tìm kiếm
                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(t => t.Title.Contains(searchString) || t.Content.Contains(searchString));
                }

                // Lấy danh sách các nhóm mà người dùng thuộc về
                var userGroupIds = _context.GroupUsers
                    .Where(gu => gu.UserId.ToString() == CurrentUserID)
                    .Select(gu => gu.GroupId)
                    .ToList();

                // Lọc quyền truy cập
                if (CurrentUserRole != "Admin")
                {
                    query = query.Where(t =>
                        t.UserId.ToString() == CurrentUserID ||
                        t.VisibilityType == "public" ||
                        (t.VisibilityType == "private" && t.UserId.ToString() == CurrentUserID) ||
                        (t.VisibilityType == "group" && t.PostGroups.Any(g => userGroupIds.Contains(g.GroupId))) ||
                        (t.VisibilityType == "custom" && t.PostUsers.Any(u => u.UserId.ToString() == CurrentUserID))
                    );
                }

                var result = await query.ToListAsync();
                return View(result);
            }
        }

        // GET: Posts/Create
        public async Task<IActionResult> Create()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                ViewBag.Users = new MultiSelectList(_context.Users.Where(u => u.Role == "Lecturer").ToList(), "UserId", "Username");
                ViewBag.Groups = new MultiSelectList(_context.Groups.ToList(), "GroupId", "GroupName");
                ViewBag.VisibilityTypes = new SelectList(new[] {
                    new { Value = "public", Text = "Công khai" },
                    new { Value = "private", Text = "Riêng tư" },
                    new { Value = "group", Text = "Theo nhóm" },
                    new { Value = "custom", Text = "Tùy chọn người dùng" }
                }, "Value", "Text");
                ViewBag.Username = CurrentUserName;
                ViewBag.Role = CurrentUserRole;
                var currentUser = await _context.Users.FirstOrDefaultAsync(m => m.UserId.ToString().Equals(CurrentUserID));
                ViewBag.Avatar = currentUser?.Avatar;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, IFormFile? imageFile, IFormFile? documentFile, int[] selectedGroups, int[] selectedUsers, int[] mentionedUsers)
        {
            ViewBag.Users = new MultiSelectList(_context.Users.Where(u => u.Role == "Lecturer").ToList(), "UserId", "Username", selectedUsers);
            ViewBag.Groups = new MultiSelectList(_context.Groups.ToList(), "GroupId", "GroupName", selectedGroups);
            ViewBag.VisibilityTypes = new SelectList(new[] {
                new { Value = "public", Text = "Công khai" },
                new { Value = "private", Text = "Riêng tư" },
                new { Value = "group", Text = "Theo nhóm" },
                new { Value = "custom", Text = "Tùy chọn người dùng" }
            }, "Value", "Text", post.VisibilityType);

            var user = _context.Users.FirstOrDefault(m => m.UserId.ToString().Equals(CurrentUserID));
            post.UserId = user.UserId;
            post.CreatedAt = DateTime.Now;
            post.UpdatedAt = DateTime.Now;

            if (ModelState.IsValid)
            {
                // Xử lý ảnh
                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "images");
                    Directory.CreateDirectory(imageFolder);
                    var imageFileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    var imagePath = Path.Combine(imageFolder, imageFileName);

                    using (var stream = new FileStream(imagePath, FileMode.Create))
                        await imageFile.CopyToAsync(stream);

                    post.ImageUrl = "/uploads/images/" + imageFileName;
                }

                // Xử lý file đính kèm
                if (documentFile != null && documentFile.Length > 0)
                {
                    var fileFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "files");
                    Directory.CreateDirectory(fileFolder);
                    var filePath = Path.Combine(fileFolder, documentFile.FileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await documentFile.CopyToAsync(stream);
                    }
                    post.FileUrl = "/uploads/files/" + documentFile.FileName;
                }

                // Lưu bài viết
                _context.Posts.Add(post);
                await _context.SaveChangesAsync();

                // Thêm các nhóm được chọn
                if (post.VisibilityType == "group" && selectedGroups != null)
                {
                    foreach (var groupId in selectedGroups)
                    {
                        _context.PostGroups.Add(new PostGroup
                        {
                            PostId = post.PostId,
                            GroupId = groupId
                        });
                    }
                }

                // Thêm các người dùng được chọn
                if (post.VisibilityType == "custom" && selectedUsers != null)
                {
                    foreach (var userId in selectedUsers)
                    {
                        _context.PostUsers.Add(new PostUser
                        {
                            PostId = post.PostId,
                            UserId = userId
                        });
                    }
                }

                // Xử lý người dùng được tag
                if (mentionedUsers != null && mentionedUsers.Any())
                        {
                    foreach (var mentionedUserId in mentionedUsers)
                    {
                        var mention = new PostMention
                        {
                                        PostId = post.PostId,
                            UserId = mentionedUserId,
                                        CreatedAt = DateTime.Now
                                    };
                        _context.PostMentions.Add(mention);

                        // Tạo thông báo cho người được tag
                        if (mentionedUserId != user.UserId) // Không thông báo cho người tạo bài
                        {
                            var notification = new Notification
                            {
                                UserId = mentionedUserId,
                                Title = "Bạn được nhắc đến trong một bài viết",
                                Content = $"{user.Username} đã nhắc đến bạn trong bài viết: {post.Title}",
                                Type = 5, // 5 for post
                                PostId = post.PostId,
                                Path = $"/Posts/Details/{post.PostId}",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            };
                            _context.Notifications.Add(notification);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { tab = "GocCoVan" });
            }

            return View(post);
        }

        // GET: Posts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(t => t.PostGroups)
                .Include(t => t.PostUsers)
                .Include(t => t.PostMentions)
                .FirstOrDefaultAsync(t => t.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            ViewBag.Users = new MultiSelectList(_context.Users.Where(u => u.Role == "Lecturer").ToList(), "UserId", "Username", post.PostUsers.Select(tu => tu.UserId));
            ViewBag.Groups = new MultiSelectList(_context.Groups.ToList(), "GroupId", "GroupName", post.PostGroups.Select(tg => tg.GroupId));
            ViewBag.VisibilityTypes = new SelectList(new[] {
                new { Value = "public", Text = "Công khai" },
                new { Value = "private", Text = "Riêng tư" },
                new { Value = "group", Text = "Theo nhóm" },
                new { Value = "custom", Text = "Tùy chọn người dùng" }
            }, "Value", "Text", post.VisibilityType);

            ViewBag.Username = CurrentUserName;
            ViewBag.Role = CurrentUserRole;
            var currentUser = await _context.Users.FirstOrDefaultAsync(m => m.UserId.ToString().Equals(CurrentUserID));
            ViewBag.Avatar = currentUser?.Avatar;

            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Post model, IFormFile? imageFile, IFormFile? documentFile, int[] selectedUsers, int[] selectedGroups, int[] mentionedUsers)
        {
            if (id != model.PostId)
                return NotFound();

            var post = await _context.Posts
                .Include(t => t.PostUsers)
                .Include(t => t.PostGroups)
                .Include(t => t.PostMentions)
                .FirstOrDefaultAsync(t => t.PostId == id);

            if (post == null)
                return NotFound();

            post.Title = model.Title;
            post.Content = model.Content;
            post.VisibilityType = model.VisibilityType;
            post.UpdatedAt = DateTime.Now;

            // Xử lý ảnh và file tương tự như trong Create
            if (imageFile != null && imageFile.Length > 0)
            {
                // [Code xử lý ảnh giống như trong Create]
            }

            if (documentFile != null && documentFile.Length > 0)
            {
                // [Code xử lý file giống như trong Create]
            }

            // Cập nhật nhóm và người dùng
            _context.PostUsers.RemoveRange(post.PostUsers);
            _context.PostGroups.RemoveRange(post.PostGroups);
            _context.PostMentions.RemoveRange(post.PostMentions);

            if (model.VisibilityType == "custom" && selectedUsers != null)
            {
                foreach (var uid in selectedUsers)
                {
                    post.PostUsers.Add(new PostUser { PostId = post.PostId, UserId = uid });
                }
            }

            if (model.VisibilityType == "group" && selectedGroups != null)
            {
                foreach (var gid in selectedGroups)
                {
                    post.PostGroups.Add(new PostGroup { PostId = post.PostId, GroupId = gid });
                }
            }

            // Xử lý người dùng được tag
            if (mentionedUsers != null && mentionedUsers.Any())
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId.ToString() == CurrentUserID);
                foreach (var mentionedUserId in mentionedUsers)
                {
                    var mention = new PostMention
                    {
                        PostId = post.PostId,
                        UserId = mentionedUserId,
                        CreatedAt = DateTime.Now
                    };
                    _context.PostMentions.Add(mention);

                    // Tạo thông báo cho người được tag mới
                    if (mentionedUserId != currentUser.UserId)
                    {
                        var notification = new Notification
                        {
                            UserId = mentionedUserId,
                            Title = "Bạn được nhắc đến trong một bài viết",
                            Content = $"{currentUser.Username} đã nhắc đến bạn trong bài viết: {post.Title}",
                            Type = 5, // 5 for post
                            PostId = post.PostId,
                            Path = $"/Posts/Details/{post.PostId}",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };
                        _context.Notifications.Add(notification);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home", new { tab = "BaiViet" });
        }

        // GET: Posts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(t => t.User)
                .Include(t => t.CommentPosts)
                .Include(t => t.PostUsers)
                .Include(t => t.PostGroups)
                .FirstOrDefaultAsync(m => m.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts
                .Include(p => p.PostMentions)
                .Include(p => p.PostUsers)
                .Include(p => p.PostGroups)
                .Include(p => p.CommentPosts)
                    .ThenInclude(c => c.Replies)
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post != null)
            {
                // Xóa các like liên quan
                var likes = _context.LikePosts.Where(l => l.PostId == post.PostId);
                _context.LikePosts.RemoveRange(likes);

                // Xóa các file vật lý nếu có
                if (!string.IsNullOrEmpty(post.ImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, post.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                if (!string.IsNullOrEmpty(post.FileUrl))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, post.FileUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // Xóa tất cả các thông báo liên quan
                var notifications = _context.Notifications.Where(n => n.PostId == post.PostId);
                _context.Notifications.RemoveRange(notifications);

                // Xóa tất cả các bình luận và replies
                if (post.CommentPosts != null)
                {
                    foreach (var comment in post.CommentPosts)
                    {
                        // Xóa ảnh của replies nếu có
                        if (comment.Replies != null)
                        {
                            foreach (var reply in comment.Replies)
                            {
                                if (!string.IsNullOrEmpty(reply.ImageUrl))
                                {
                                    var replyImagePath = Path.Combine(_webHostEnvironment.WebRootPath, reply.ImageUrl.TrimStart('/'));
                                    if (System.IO.File.Exists(replyImagePath))
                                    {
                                        System.IO.File.Delete(replyImagePath);
                                    }
                                }
                            }
                            _context.CommentPosts.RemoveRange(comment.Replies);
                        }

                        // Xóa ảnh của comment nếu có
                        if (!string.IsNullOrEmpty(comment.ImageUrl))
                        {
                            var commentImagePath = Path.Combine(_webHostEnvironment.WebRootPath, comment.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(commentImagePath))
                            {
                                System.IO.File.Delete(commentImagePath);
                            }
                        }
                    }
                    _context.CommentPosts.RemoveRange(post.CommentPosts);
                }

                // Xóa các liên kết trong bảng PostMentions
                if (post.PostMentions != null)
                {
                    _context.PostMentions.RemoveRange(post.PostMentions);
                }

                // Xóa các liên kết trong bảng PostUsers
                if (post.PostUsers != null)
                {
                    _context.PostUsers.RemoveRange(post.PostUsers);
                }

                // Xóa các liên kết trong bảng PostGroups
                if (post.PostGroups != null)
                {
                    _context.PostGroups.RemoveRange(post.PostGroups);
                }

                // Cuối cùng xóa Post
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home", new { tab = "BaiViet" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple(int[] selectedPosts)
        {
            if (selectedPosts == null || selectedPosts.Length == 0)
            {
                TempData["ErrorMessage"] = "Bạn chưa chọn bài viết nào để xóa.";
                return RedirectToAction("Index");
            }

            foreach (var id in selectedPosts)
            {
                var post = await _context.Posts
                    .Include(t => t.PostUsers)
                    .Include(t => t.PostGroups)
                    .Include(t => t.CommentPosts)
                    .FirstOrDefaultAsync(t => t.PostId == id);

                if (post != null)
                {
                    // Xóa các like liên quan
                    var likes = _context.LikePosts.Where(l => l.PostId == post.PostId);
                    _context.LikePosts.RemoveRange(likes);

                    // Xóa files
                    if (!string.IsNullOrEmpty(post.ImageUrl))
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, post.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    if (!string.IsNullOrEmpty(post.FileUrl))
                    {
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, post.FileUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    // Xóa comments và related data
                    if (post.CommentPosts != null)
                    {
                        _context.CommentPosts.RemoveRange(post.CommentPosts);
                    }

                    // Xóa user associations
                    if (post.PostUsers != null)
                    {
                        _context.PostUsers.RemoveRange(post.PostUsers);
                    }

                    // Xóa group associations
                    if (post.PostGroups != null)
                    {
                        _context.PostGroups.RemoveRange(post.PostGroups);
                    }

                    _context.Posts.Remove(post);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa thành công các bài viết đã chọn.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(int id)
        {
            if (!IsLogin || string.IsNullOrEmpty(CurrentUserID))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thích bài viết" });
            }

            if (!int.TryParse(CurrentUserID, out int currentUserId))
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại" });
            }
            
            // Tải lại post và include LikePosts để có dữ liệu mới nhất
            var post = await _context.Posts
                .Include(p => p.LikePosts)
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại" });
            }

            var existingLike = await _context.LikePosts
                .FirstOrDefaultAsync(l => l.PostId == id && l.UserId == currentUserId);

            if (existingLike != null)
            {
                // Unlike
                _context.LikePosts.Remove(existingLike);
                await _context.SaveChangesAsync();
                
                // Đếm lại số lượt like
                var updatedLikeCount = await _context.LikePosts.CountAsync(l => l.PostId == id);
                return Json(new { success = true, likeCount = updatedLikeCount, isLiked = false });
            }
            else
            {
                // Like
                var newLike = new LikePost
                {
                    PostId = id,
                    UserId = currentUserId,
                    CreatedAt = DateTime.Now
                };
                _context.LikePosts.Add(newLike);
                await _context.SaveChangesAsync();
                
                // Đếm lại số lượt like
                var updatedLikeCount = await _context.LikePosts.CountAsync(l => l.PostId == id);
                return Json(new { success = true, likeCount = updatedLikeCount, isLiked = true });
            }
        }

        // GET: Posts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.CommentPosts)
                    .ThenInclude(c => c.User)
                .Include(p => p.LikePosts)
                .Include(p => p.PostGroups)
                    .ThenInclude(pg => pg.Group)
                .Include(p => p.PostUsers)
                    .ThenInclude(pu => pu.User)
                .Include(p => p.PostMentions)
                    .ThenInclude(pm => pm.User)
                .FirstOrDefaultAsync(m => m.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền xem bài viết
            var currentUserId = CurrentUserID;
            var canView = post.VisibilityType == "public" ||
                         post.UserId.ToString() == currentUserId ||
                         (post.VisibilityType == "private" && post.UserId.ToString() == currentUserId) ||
                         (post.VisibilityType == "group" && post.PostGroups.Any(pg => pg.Group.GroupUsers.Any(gu => gu.UserId.ToString() == currentUserId))) ||
                         (post.VisibilityType == "custom" && post.PostUsers.Any(pu => pu.UserId.ToString() == currentUserId));

            // Add admin check here
            if (CurrentUserRole == "Admin" || canView)
            {
                // Kiểm tra xem người dùng hiện tại đã like bài viết chưa
                var isLiked = false;
                if (!string.IsNullOrEmpty(currentUserId) && int.TryParse(currentUserId, out int currentUserIdInt))
                {
                    isLiked = post.LikePosts != null && post.LikePosts.Any(lp => lp.UserId == currentUserIdInt);
                }

                ViewBag.IsLiked = isLiked;
                ViewBag.LikeCount = post.LikePosts?.Count ?? 0;

                return View(post);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.PostId == id);
        }
    }
}