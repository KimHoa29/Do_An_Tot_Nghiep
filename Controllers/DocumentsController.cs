﻿using Do_An_Tot_Nghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class DocumentsController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DocumentsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<IActionResult> Index(string searchString)
        {
            if (!IsLogin || CurrentUserRole.Equals("Student"))
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Documents
                .Include(t => t.User)
                .Include(t => t.DocumentGroups)
                .Include(t => t.DocumentUsers)
                .Include(t => t.CommentDocuments)
                    .ThenInclude(c => c.User)
                .AsQueryable();

            // Nếu không phải admin thì chỉ lấy tài liệu của người dùng hiện tại
            if (CurrentUserRole != "Admin")
            {
                query = query.Where(t => t.UserId.ToString() == CurrentUserID);
            }

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(t => t.Title.Contains(searchString) || t.Content.Contains(searchString));
            }

            var result = await query.ToListAsync();
            return View(result);
        }

        // GET: Documents/Details/5
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

            var document = await _context.Documents
                .Include(t => t.User)
                .Include(t => t.DocumentGroups)
                .Include(t => t.DocumentUsers)
                .FirstOrDefaultAsync(m => m.DocumentId == id);

            if (document == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập
            if (CurrentUserRole != "Admin")
            {
                var userGroupIds = _context.GroupUsers
                    .Where(gu => gu.UserId.ToString() == CurrentUserID)
                    .Select(gu => gu.GroupId)
                    .ToList();

                var hasAccess = document.UserId.ToString() == CurrentUserID ||
                              document.VisibilityType == "public" ||
                              (document.VisibilityType == "private" && document.UserId.ToString() == CurrentUserID) ||
                              (document.VisibilityType == "group" && document.DocumentGroups.Any(g => userGroupIds.Contains(g.GroupId))) ||
                              (document.VisibilityType == "custom" && document.DocumentUsers.Any(u => u.UserId.ToString() == CurrentUserID));

                if (!hasAccess)
                {
                    TempData["Error"] = "Bạn không có quyền truy cập tài liệu này.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(document);
        }

        // GET: Documents/Create
        public async Task<IActionResult> Create()
        {
            if (!IsLogin || CurrentUserRole.Equals("Student"))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Users = new MultiSelectList(_context.Users.Where(u => u.Role != "Admin").ToList(), "UserId", "Username");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Document document, IFormFile? imageFile, IFormFile? documentFile, int[] selectedGroups, int[] selectedUsers)
        {
            if (!IsLogin || CurrentUserRole.Equals("Student"))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Users = new MultiSelectList(_context.Users.Where(u => u.Role != "Admin").ToList(), "UserId", "Username", selectedUsers);
            ViewBag.Groups = new MultiSelectList(_context.Groups.ToList(), "GroupId", "GroupName", selectedGroups);
            ViewBag.VisibilityTypes = new SelectList(new[] {
                new { Value = "public", Text = "Công khai" },
                new { Value = "private", Text = "Riêng tư" },
                new { Value = "group", Text = "Theo nhóm" },
                new { Value = "custom", Text = "Tùy chọn người dùng" }
            }, "Value", "Text", document.VisibilityType);

            var user = _context.Users.FirstOrDefault(m => m.UserId.ToString().Equals(CurrentUserID));
            document.UserId = user.UserId;
            document.CreatedAt = DateTime.Now;
            document.UpdatedAt = DateTime.Now;

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

                    document.ImageUrl = "/uploads/images/" + imageFileName;
                }

                // Xử lý tài liệu
                if (documentFile != null && documentFile.Length > 0)
                {
                    var fileFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "files");
                    Directory.CreateDirectory(fileFolder);
                    var filePath = Path.Combine(fileFolder, documentFile.FileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await documentFile.CopyToAsync(stream);
                    }
                    document.FileUrl = "/uploads/files/" + documentFile.FileName;
                }

                // Lưu tài liệu
                _context.Documents.Add(document);
                await _context.SaveChangesAsync(); // để có DocumentId

                // Thêm các nhóm được chọn
                if (document.VisibilityType == "group" && selectedGroups != null)
                {
                    foreach (var groupId in selectedGroups)
                    {
                        _context.DocumentGroups.Add(new DocumentGroup
                        {
                            DocumentId = document.DocumentId,
                            GroupId = groupId
                        });
                    }
                }

                // Thêm các người dùng được chọn
                if (document.VisibilityType == "custom" && selectedUsers != null)
                {
                    foreach (var userId in selectedUsers)
                    {
                        _context.DocumentUsers.Add(new DocumentUser
                        {
                            DocumentId = document.DocumentId,
                            UserId = userId
                        });
                    }
                }

                // Lưu tất cả thay đổi
                await _context.SaveChangesAsync();

                // Tạo thông báo cho thành viên nhóm
                if (document.VisibilityType == "group" && selectedGroups != null)
                {
                    foreach (var groupId in selectedGroups)
                    {
                        var group = await _context.Groups
                            .Include(g => g.GroupUsers)
                            .FirstOrDefaultAsync(g => g.GroupId == groupId);

                        if (group != null)
                        {
                            foreach (var groupUser in group.GroupUsers)
                            {
                                if (groupUser.UserId != user.UserId) // Không thông báo cho người tạo
                                {
                                    var notification = new Notification
                                    {
                                        UserId = groupUser.UserId,
                                        Title = "Tài liệu mới trong nhóm",
                                        Content = $"Có tài liệu mới trong nhóm {group.GroupName}: {document.Title}",
                                        Type = 4, // 4 for document
                                        DocumentId = document.DocumentId,
                                        GroupId = group.GroupId,
                                        Path = $"/Documents/Details/{document.DocumentId}",
                                        IsRead = false,
                                        CreatedAt = DateTime.Now
                                    };

                                    _context.Notifications.Add(notification);
                                }
                            }
                        }
                    }
                }

                // Tạo thông báo cho người dùng được chọn
                if (document.VisibilityType == "custom" && selectedUsers != null)
                {
                    foreach (var selectedUserId in selectedUsers)
                    {
                        if (selectedUserId != user.UserId) // Không thông báo cho người tạo
                        {
                            var notification = new Notification
                            {
                                UserId = selectedUserId,
                                Title = "Tài liệu mới dành cho bạn",
                                Content = $"Có tài liệu mới dành cho bạn: {document.Title}",
                                Type = 4, // 4 for document
                                DocumentId = document.DocumentId,
                                Path = $"/Documents/Details/{document.DocumentId}",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            };

                            _context.Notifications.Add(notification);
                        }
                    }
                }

                // Tạo thông báo cho tất cả người dùng (trừ admin) khi tài liệu công khai
                if (document.VisibilityType == "public")
                {
                    var allUsers = await _context.Users
                        .Where(u => u.Role != "Admin" && u.UserId != user.UserId) // Lấy tất cả người dùng trừ admin và người tạo
                        .ToListAsync();

                    foreach (var targetUser in allUsers)
                    {
                        var notification = new Notification
                        {
                            UserId = targetUser.UserId,
                            Title = "Tài liệu mới công khai",
                            Content = $"Có tài liệu mới công khai: {document.Title}",
                            Type = 4, // 4 for document
                            DocumentId = document.DocumentId,
                            Path = $"/Documents/Details/{document.DocumentId}",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };

                        _context.Notifications.Add(notification);
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { tab = "TaiLieu" });
            }

            return View(document);
        }

        // GET: Documents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLogin || CurrentUserRole.Equals("Student"))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents
                .Include(t => t.DocumentGroups)
                .Include(t => t.DocumentUsers)
                .FirstOrDefaultAsync(t => t.DocumentId == id);

            if (document == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền chỉnh sửa
            if (CurrentUserRole != "Admin" && document.UserId.ToString() != CurrentUserID)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa tài liệu này.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Users = new MultiSelectList(_context.Users.ToList(), "UserId", "Username", document.DocumentUsers.Select(tu => tu.UserId));
            ViewBag.Groups = new MultiSelectList(_context.Groups.ToList(), "GroupId", "GroupName", document.DocumentGroups.Select(tg => tg.GroupId));
            ViewBag.VisibilityTypes = new SelectList(new[] {
                new { Value = "public", Text = "Công khai" },
                new { Value = "private", Text = "Riêng tư" },
                new { Value = "group", Text = "Theo nhóm" },
                new { Value = "custom", Text = "Tùy chọn người dùng" }
            }, "Value", "Text", document.VisibilityType);

            ViewBag.Username = CurrentUserName;
            ViewBag.Role = CurrentUserRole;
            var currentUser = await _context.Users.FirstOrDefaultAsync(m => m.UserId.ToString().Equals(CurrentUserID));
            ViewBag.Avatar = currentUser?.Avatar;

            return View(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Document model, IFormFile? imageFile, IFormFile? documentFile, int[] selectedUsers, int[] selectedGroups)
        {
            if (!IsLogin || CurrentUserRole.Equals("Student"))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != model.DocumentId)
                return NotFound();

            var document = await _context.Documents
                .Include(t => t.DocumentUsers)
                .Include(t => t.DocumentGroups)
                .FirstOrDefaultAsync(t => t.DocumentId == id);

            if (document == null)
                return NotFound();

            // Kiểm tra quyền chỉnh sửa
            if (CurrentUserRole != "Admin" && document.UserId.ToString() != CurrentUserID)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa tài liệu này.";
                return RedirectToAction(nameof(Index));
            }

            document.Title = model.Title;
            document.Content = model.Content;
            document.VisibilityType = model.VisibilityType;
            document.UpdatedAt = DateTime.Now;

            // Xử lý ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                var imageFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "images");
                Directory.CreateDirectory(imageFolder);
                var imageFileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var imagePath = Path.Combine(imageFolder, imageFileName);
                using (var stream = new FileStream(imagePath, FileMode.Create))
                    await imageFile.CopyToAsync(stream);
                document.ImageUrl = "/uploads/images/" + imageFileName;
            }

            // Xử lý tài liệu
            if (documentFile != null && documentFile.Length > 0)
            {
                var docFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "files");
                if (!Directory.Exists(docFolder))
                {
                    Directory.CreateDirectory(docFolder);
                }

                string fileName = documentFile.FileName;
                string filePath = Path.Combine(docFolder, fileName);
                int counter = 1;

                // Kiểm tra nếu file đã tồn tại thì thêm số đếm vào tên file
                while (System.IO.File.Exists(filePath))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    fileName = $"{fileNameWithoutExtension}({counter}){extension}";
                    filePath = Path.Combine(docFolder, fileName);
                    counter++;
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await documentFile.CopyToAsync(stream);
                }
                document.FileUrl = "/uploads/files/" + documentFile.FileName;
            }

            // Cập nhật nhóm và người dùng
            _context.DocumentUsers.RemoveRange(document.DocumentUsers);
            _context.DocumentGroups.RemoveRange(document.DocumentGroups);

            if (model.VisibilityType == "custom" && selectedUsers != null)
            {
                foreach (var uid in selectedUsers)
                {
                    document.DocumentUsers.Add(new DocumentUser { DocumentId = document.DocumentId, UserId = uid });
                }

            }

            if (model.VisibilityType == "group" && selectedGroups != null)
            {
                foreach (var gid in selectedGroups)
                {
                    document.DocumentGroups.Add(new DocumentGroup { DocumentId = document.DocumentId, GroupId = gid });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home", new { tab = "TaiLieu" });
        }

        // GET: Documents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLogin || CurrentUserRole.Equals("Student"))
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var document = await _context.Documents
                .Include(t => t.User)
                .Include(t => t.CommentDocuments)
                .Include(t => t.DocumentUsers)
                .Include(t => t.DocumentGroups)
                .FirstOrDefaultAsync(m => m.DocumentId == id);

            if (document == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền xóa
            if (CurrentUserRole != "Admin" && document.UserId.ToString() != CurrentUserID)
            {
                TempData["Error"] = "Bạn không có quyền xóa tài liệu này.";
                return RedirectToAction(nameof(Index));
            }

            return View(document);
        }

        // POST: Documents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLogin || CurrentUserRole.Equals("Student"))
            {
                return RedirectToAction("Login", "Account");
            }

            var document = await _context.Documents
                .Include(t => t.DocumentUsers)
                .Include(t => t.DocumentGroups)
                .Include(t => t.CommentDocuments)
                .FirstOrDefaultAsync(t => t.DocumentId == id);

            if (document != null)
            {
                // Kiểm tra quyền xóa
                if (CurrentUserRole != "Admin" && document.UserId.ToString() != CurrentUserID)
                {
                    TempData["Error"] = "Bạn không có quyền xóa tài liệu này.";
                    return RedirectToAction(nameof(Index));
                }

                // Xóa các like liên quan
                var likes = _context.LikeDocuments.Where(l => l.DocumentId == document.DocumentId);
                _context.LikeDocuments.RemoveRange(likes);

                // Xóa các saves liên quan
                var saves = _context.Saves.Where(s => s.DocumentId == document.DocumentId);
                _context.Saves.RemoveRange(saves);

                // Xóa các file vật lý nếu có
                if (!string.IsNullOrEmpty(document.ImageUrl))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, document.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                if (!string.IsNullOrEmpty(document.FileUrl))
                {
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FileUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // Xóa tất cả các bình luận và replies
                if (document.CommentDocuments != null)
                {
                    foreach (var comment in document.CommentDocuments)
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
                            _context.CommentDocuments.RemoveRange(comment.Replies);
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
                    _context.CommentDocuments.RemoveRange(document.CommentDocuments);
                }

                // Xóa các liên kết trong bảng DocumentUsers
                if (document.DocumentUsers != null)
                {
                    _context.DocumentUsers.RemoveRange(document.DocumentUsers);
                }

                // Xóa các liên kết trong bảng DocumentGroups
                if (document.DocumentGroups != null)
                {
                    _context.DocumentGroups.RemoveRange(document.DocumentGroups);
                }

                // Cuối cùng xóa Document
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Home", new { tab = "TaiLieu" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple(int[] selectedDocuments)
        {
            if (!IsLogin || CurrentUserRole.Equals("Student"))
            {
                return RedirectToAction("Login", "Account");
            }

            if (selectedDocuments == null || selectedDocuments.Length == 0)
            {
                TempData["ErrorMessage"] = "Bạn chưa chọn tài liệu nào để xóa.";
                return RedirectToAction("Index");
            }

            // Kiểm tra quyền xóa cho từng tài liệu
            if (CurrentUserRole != "Admin")
            {
                var documents = await _context.Documents
                    .Where(d => selectedDocuments.Contains(d.DocumentId))
                    .ToListAsync();

                var unauthorizedDocuments = documents
                    .Where(d => d.UserId.ToString() != CurrentUserID)
                    .ToList();

                if (unauthorizedDocuments.Any())
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xóa một số tài liệu đã chọn.";
                    return RedirectToAction("Index");
                }
            }

            foreach (var id in selectedDocuments)
            {
                var document = await _context.Documents
                    .Include(t => t.DocumentUsers)
                    .Include(t => t.DocumentGroups)
                    .Include(t => t.CommentDocuments)
                        .ThenInclude(c => c.Replies)
                    .FirstOrDefaultAsync(t => t.DocumentId == id);

                if (document != null)
                {
                    // Xóa các like liên quan
                    var likes = _context.LikeDocuments.Where(l => l.DocumentId == document.DocumentId);
                    _context.LikeDocuments.RemoveRange(likes);

                    // Xóa các saves liên quan
                    var saves = _context.Saves.Where(s => s.DocumentId == document.DocumentId);
                    _context.Saves.RemoveRange(saves);

                    // Xóa các file vật lý nếu có
                    if (!string.IsNullOrEmpty(document.ImageUrl))
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, document.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    if (!string.IsNullOrEmpty(document.FileUrl))
                    {
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, document.FileUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    // Xóa tất cả các bình luận và replies
                    if (document.CommentDocuments != null)
                    {
                        foreach (var comment in document.CommentDocuments)
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
                                _context.CommentDocuments.RemoveRange(comment.Replies);
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
                        _context.CommentDocuments.RemoveRange(document.CommentDocuments);
                    }

                    // Xóa các liên kết trong bảng DocumentUsers
                    if (document.DocumentUsers != null)
                    {
                        _context.DocumentUsers.RemoveRange(document.DocumentUsers);
                    }

                    // Xóa các liên kết trong bảng DocumentGroups
                    if (document.DocumentGroups != null)
                    {
                        _context.DocumentGroups.RemoveRange(document.DocumentGroups);
                    }

                    // Cuối cùng xóa Document
                    _context.Documents.Remove(document);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa thành công các tài liệu đã chọn.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(int id)
        {
            if (!IsLogin)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thích tài liệu" });
            }

            if (!int.TryParse(CurrentUserID, out int currentUserId))
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại" });
            }

            int retryCount = 3;
            while (retryCount > 0)
            {
                try
                {
                    var document = await _context.Documents
                        .Include(p => p.LikeDocuments)
                        .FirstOrDefaultAsync(p => p.DocumentId == id);

                    if (document == null)
                    {
                        return Json(new { success = false, message = "Tài liệu không tồn tại" });
                    }

                    var existingLike = await _context.LikeDocuments
                        .FirstOrDefaultAsync(l => l.DocumentId == id && l.UserId == currentUserId);

                    if (existingLike != null)
                    {
                        // Unlike
                        _context.LikeDocuments.Remove(existingLike);
                        await _context.SaveChangesAsync();

                        // Đếm lại số lượt like
                        var updatedLikeCount = await _context.LikeDocuments.CountAsync(l => l.DocumentId == id);
                        return Json(new { success = true, likeCount = updatedLikeCount, isLiked = false });
                    }
                    else
                    {
                        // Like
                        var newLike = new LikeDocument
                        {
                            DocumentId = id,
                            UserId = currentUserId,
                            CreatedAt = DateTime.Now
                        };
                        _context.LikeDocuments.Add(newLike);
                        await _context.SaveChangesAsync();

                        // Đếm lại số lượt like
                        var updatedLikeCount = await _context.LikeDocuments.CountAsync(l => l.DocumentId == id);
                        return Json(new { success = true, likeCount = updatedLikeCount, isLiked = true });
                    }
                }
                catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1205) // Deadlock
                {
                    retryCount--;
                    if (retryCount == 0) throw;
                    await Task.Delay(100);
                }
            }
            return Json(new { success = false, message = "Có lỗi xảy ra khi xử lý like. Vui lòng thử lại." });
        }

        [HttpGet]
        public async Task<IActionResult> PopupPartial(int id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            var document = await _context.Documents
                .Include(t => t.User)
                .Include(t => t.DocumentGroups)
                .Include(t => t.DocumentUsers)
                .Include(t => t.LikeDocuments)
                .Include(t => t.CommentDocuments)
                    .ThenInclude(c => c.User)
                .Include(t => t.CommentDocuments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.DocumentId == id);

            if (document == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập
            if (CurrentUserRole != "Admin")
            {
                var userGroupIds = _context.GroupUsers
                    .Where(gu => gu.UserId.ToString() == CurrentUserID)
                    .Select(gu => gu.GroupId)
                    .ToList();

                var hasAccess = document.UserId.ToString() == CurrentUserID ||
                              document.VisibilityType == "public" ||
                              (document.VisibilityType == "private" && document.UserId.ToString() == CurrentUserID) ||
                              (document.VisibilityType == "group" && document.DocumentGroups.Any(g => userGroupIds.Contains(g.GroupId))) ||
                              (document.VisibilityType == "custom" && document.DocumentUsers.Any(u => u.UserId.ToString() == CurrentUserID));

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bạn không có quyền truy cập tài liệu này." });
                }
            }

            return PartialView("_DocumentPopupPartial", document);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentPopup(int id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            var document = await _context.Documents
                .Include(d => d.User)
                .Include(d => d.DocumentGroups)
                    .ThenInclude(dg => dg.Group)
                        .ThenInclude(g => g.GroupUsers)
                .Include(d => d.DocumentUsers)
                .Include(d => d.CommentDocuments)
                    .ThenInclude(c => c.User)
                .Include(d => d.CommentDocuments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(d => d.LikeDocuments)
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (document == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập
            if (CurrentUserRole != "Admin")
            {
                var userGroupIds = _context.GroupUsers
                    .Where(gu => gu.UserId.ToString() == CurrentUserID)
                    .Select(gu => gu.GroupId)
                    .ToList();

                var hasAccess = document.UserId.ToString() == CurrentUserID ||
                              document.VisibilityType == "public" ||
                              (document.VisibilityType == "private" && document.UserId.ToString() == CurrentUserID) ||
                              (document.VisibilityType == "group" && document.DocumentGroups.Any(g => userGroupIds.Contains(g.GroupId))) ||
                              (document.VisibilityType == "custom" && document.DocumentUsers.Any(u => u.UserId.ToString() == CurrentUserID));

                if (!hasAccess)
                {
                    return Json(new { success = false, message = "Bạn không có quyền truy cập tài liệu này." });
                }
            }

            return PartialView("_DocumentPopupPartial", document);
        }

        private bool DocumentExists(int id)
        {
            return _context.Documents.Any(e => e.DocumentId == id);
        }
    }
}
