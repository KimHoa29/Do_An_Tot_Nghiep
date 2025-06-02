using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class TopicsController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TopicsController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
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

            var query = _context.Topics
                .Include(t => t.User)
                .Include(t => t.TopicGroups)
                .Include(t => t.TopicUsers)
                .Include(t => t.TopicTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .AsQueryable();

            // Nếu không phải admin thì chỉ lấy chủ đề của người dùng hiện tại
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

        // GET: Topics/Details/5
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

            var topic = await _context.Topics
                .Include(t => t.User)
                .Include(t => t.TopicTags)
                    .ThenInclude(tt => tt.Tag)
                .Include(t => t.Comments)
                    .ThenInclude(c => c.User)
                .Include(t => t.TopicGroups)
                .Include(t => t.TopicUsers)
                .FirstOrDefaultAsync(m => m.TopicId == id);

            if (topic == null)
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

                var hasAccess = topic.UserId.ToString() == CurrentUserID ||
                              topic.VisibilityType == "public" ||
                              (topic.VisibilityType == "private" && topic.UserId.ToString() == CurrentUserID) ||
                              (topic.VisibilityType == "group" && topic.TopicGroups.Any(g => userGroupIds.Contains(g.GroupId))) ||
                              (topic.VisibilityType == "custom" && topic.TopicUsers.Any(u => u.UserId.ToString() == CurrentUserID));

                if (!hasAccess)
                {
                    TempData["Error"] = "Bạn không có quyền truy cập chủ đề này.";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(topic);
        }

        // GET: Topics/Create
        public async Task<IActionResult> Create()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Tags = new MultiSelectList(_context.Tags.ToList(), "TagId", "Name");
            ViewBag.Users = new MultiSelectList(_context.Users.Where(u => u.Role != "Admin").ToList(), "UserId", "Username");
            
            // Lấy danh sách các nhóm mà người dùng hiện tại là thành viên
            var userGroups = await _context.GroupUsers
                .Where(gu => gu.UserId.ToString() == CurrentUserID)
                .Select(gu => gu.Group)
                .ToListAsync();
            
            ViewBag.Groups = new MultiSelectList(userGroups, "GroupId", "GroupName");
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
        public async Task<IActionResult> Create(Topic topic, IFormFile? imageFile, IFormFile? documentFile, int[] selectedGroups, int[] selectedUsers, int[] selectedTagIds)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Tags = new MultiSelectList(_context.Tags.ToList(), "TagId", "Name", selectedTagIds);
            ViewBag.Users = new MultiSelectList(_context.Users.Where(u => u.Role != "Admin").ToList(), "UserId", "Username", selectedUsers);
            ViewBag.Groups = new MultiSelectList(_context.Groups.ToList(), "GroupId", "GroupName", selectedGroups);
            ViewBag.VisibilityTypes = new SelectList(new[] {
                new { Value = "public", Text = "Công khai" },
                new { Value = "private", Text = "Riêng tư" },
                new { Value = "group", Text = "Theo nhóm" },
                new { Value = "custom", Text = "Tùy chọn người dùng" }
            }, "Value", "Text", topic.VisibilityType);

            var user = _context.Users.FirstOrDefault(m => m.UserId.ToString().Equals(CurrentUserID));
            topic.UserId = user.UserId;
            topic.CreatedAt = DateTime.Now;
            topic.UpdatedAt = DateTime.Now;

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

                    topic.ImageUrl = "/uploads/images/" + imageFileName;
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
                    topic.FileUrl = "/uploads/files/" + documentFile.FileName;
                }

                // Lưu chủ đề
                _context.Topics.Add(topic);
                await _context.SaveChangesAsync(); // để có TopicId

                // Thêm các nhóm được chọn
                if (topic.VisibilityType == "group" && selectedGroups != null)
                {
                    foreach (var groupId in selectedGroups)
                    {
                        _context.TopicGroups.Add(new TopicGroup
                        {
                            TopicId = topic.TopicId,
                            GroupId = groupId
                        });
                    }
                }

                // Thêm các người dùng được chọn
                if (topic.VisibilityType == "custom" && selectedUsers != null)
                {
                    foreach (var userId in selectedUsers)
                    {
                        _context.TopicUsers.Add(new TopicUser
                        {
                            TopicId = topic.TopicId,
                            UserId = userId
                        });
                    }
                }

                // Gán tag
                if (selectedTagIds != null && selectedTagIds.Any())
                {
                    foreach (var tagId in selectedTagIds)
                    {
                        _context.TopicTags.Add(new TopicTag
                        {
                            TopicId = topic.TopicId,
                            TagId = tagId
                        });
                    }
                }

                // Lưu tất cả thay đổi
                await _context.SaveChangesAsync();

                // Tạo thông báo cho thành viên nhóm
                if (topic.VisibilityType == "group" && selectedGroups != null)
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
                                        Title = "Chủ đề mới trong nhóm",
                                        Content = $"Có chủ đề mới trong nhóm {group.GroupName}: {topic.Title}",
                                        Type = 3, // 3 for topic
                                        TopicId = topic.TopicId,
                                        GroupId = group.GroupId,
                                        Path = $"/Topics/Details/{topic.TopicId}",
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
                if (topic.VisibilityType == "custom" && selectedUsers != null)
                {
                    foreach (var selectedUserId in selectedUsers)
                    {
                        if (selectedUserId != user.UserId) // Không thông báo cho người tạo
                        {
                            var notification = new Notification
                            {
                                UserId = selectedUserId,
                                Title = "Chủ đề mới dành cho bạn",
                                Content = $"Có chủ đề mới dành cho bạn: {topic.Title}",
                                Type = 3, // 3 for topic
                                TopicId = topic.TopicId,
                                Path = $"/Topics/Details/{topic.TopicId}",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            };

                            _context.Notifications.Add(notification);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home", new { tab = "ChuDe" });
            }

            return View(topic);
        }

        // GET: Topics/Edit/5
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

            var topic = await _context.Topics
                .Include(t => t.TopicGroups)
                .Include(t => t.TopicUsers)
                .Include(t => t.TopicTags)
                .FirstOrDefaultAsync(t => t.TopicId == id);

            if (topic == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền chỉnh sửa
            if (CurrentUserRole != "Admin" && topic.UserId.ToString() != CurrentUserID)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa chủ đề này.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Tags = new MultiSelectList(_context.Tags.ToList(), "TagId", "Name", topic.TopicTags.Select(t => t.TagId));
            ViewBag.Users = new MultiSelectList(_context.Users.Where(u => u.Role != "Admin").ToList(), "UserId", "Username", topic.TopicUsers.Select(tu => tu.UserId));
            
            // Lấy danh sách các nhóm mà người dùng hiện tại là thành viên
            var userGroups = await _context.GroupUsers
                .Where(gu => gu.UserId.ToString() == CurrentUserID)
                .Select(gu => gu.Group)
                .ToListAsync();
            
            // Lấy các nhóm đã được chọn trước đó
            var selectedGroupIds = topic.TopicGroups.Select(tg => tg.GroupId).ToList();
            
            // Tạo MultiSelectList với các nhóm của người dùng và các nhóm đã chọn
            ViewBag.Groups = new MultiSelectList(userGroups, "GroupId", "GroupName", selectedGroupIds);
            
            ViewBag.VisibilityTypes = new SelectList(new[] {
                new { Value = "public", Text = "Công khai" },
                new { Value = "private", Text = "Riêng tư" },
                new { Value = "group", Text = "Theo nhóm" },
                new { Value = "custom", Text = "Tùy chọn người dùng" }
            }, "Value", "Text", topic.VisibilityType);

            ViewBag.Username = CurrentUserName;
            ViewBag.Role = CurrentUserRole;
            var currentUser = await _context.Users.FirstOrDefaultAsync(m => m.UserId.ToString().Equals(CurrentUserID));
            ViewBag.Avatar = currentUser?.Avatar;

            return View(topic);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Topic model, IFormFile? imageFile, IFormFile? documentFile, int[] selectedUsers, int[] selectedGroups, int[] selectedTagIds)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != model.TopicId)
                return NotFound();

            var topic = await _context.Topics
                .Include(t => t.TopicUsers)
                .Include(t => t.TopicGroups)
                .Include(t => t.TopicTags)
                .FirstOrDefaultAsync(t => t.TopicId == id);

            if (topic == null)
                return NotFound();

            // Kiểm tra quyền chỉnh sửa
            if (CurrentUserRole != "Admin" && topic.UserId.ToString() != CurrentUserID)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa chủ đề này.";
                return RedirectToAction(nameof(Index));
            }

            // Store old visibility type and groups/users for comparison
            var oldVisibilityType = topic.VisibilityType;
            var oldGroupIds = topic.TopicGroups.Select(tg => tg.GroupId).ToList();
            var oldUserIds = topic.TopicUsers.Select(tu => tu.UserId).ToList();

            topic.Title = model.Title;
            topic.Content = model.Content;
            topic.VisibilityType = model.VisibilityType;
            topic.UpdatedAt = DateTime.Now;

            // Xử lý ảnh
            if (imageFile != null && imageFile.Length > 0)
            {
                var imageFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "images");
                Directory.CreateDirectory(imageFolder);
                var imageFileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var imagePath = Path.Combine(imageFolder, imageFileName);
                using (var stream = new FileStream(imagePath, FileMode.Create))
                    await imageFile.CopyToAsync(stream);
                topic.ImageUrl = "/uploads/images/" + imageFileName;
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
                topic.FileUrl = "/uploads/files/" + documentFile.FileName;
            }

            // Cập nhật nhóm và người dùng
            _context.TopicUsers.RemoveRange(topic.TopicUsers);
            _context.TopicGroups.RemoveRange(topic.TopicGroups);

            // Create notifications for newly added group members
            if (model.VisibilityType == "group" && selectedGroups != null)
            {
                foreach (var groupId in selectedGroups)
                {
                    if (!oldGroupIds.Contains(groupId))
                    {
                        var group = await _context.Groups
                            .Include(g => g.GroupUsers)
                            .FirstOrDefaultAsync(g => g.GroupId == groupId);

                        if (group != null)
                        {
                            foreach (var groupUser in group.GroupUsers)
                            {
                                if (groupUser.UserId.ToString() != CurrentUserID) // Don't notify the topic creator
                                {
                                    var notification = new Notification
                                    {
                                        UserId = groupUser.UserId,
                                        Title = "Chủ đề mới trong nhóm",
                                        Content = $"Có chủ đề mới trong nhóm {group.GroupName}: {topic.Title}",
                                        Type = 3, // 3 for topic
                                        TopicId = topic.TopicId,
                                        GroupId = group.GroupId,
                                        Path = $"/Topics/Details/{topic.TopicId}",
                                        IsRead = false,
                                        CreatedAt = DateTime.Now
                                    };

                                    _context.Notifications.Add(notification);
                                }
                            }
                        }
                    }
                }
            }

            // Create notifications for newly added users
            if (model.VisibilityType == "custom" && selectedUsers != null)
            {
                foreach (var selectedUserId in selectedUsers)
                {
                    if (!oldUserIds.Contains(selectedUserId) && selectedUserId.ToString() != CurrentUserID)
                    {
                        var notification = new Notification
                        {
                            UserId = selectedUserId,
                            Title = "Chủ đề mới dành cho bạn",
                            Content = $"Có chủ đề mới dành cho bạn: {topic.Title}",
                            Type = 3, // 3 for topic
                            TopicId = topic.TopicId,
                            Path = $"/Topics/Details/{topic.TopicId}",
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        };

                        _context.Notifications.Add(notification);
                    }
                }
            }

            if (model.VisibilityType == "custom" && selectedUsers != null)
            {
                foreach (var uid in selectedUsers)
                {
                    topic.TopicUsers.Add(new TopicUser { TopicId = topic.TopicId, UserId = uid });
                }
            }

            if (model.VisibilityType == "group" && selectedGroups != null)
            {
                foreach (var gid in selectedGroups)
                {
                    topic.TopicGroups.Add(new TopicGroup { TopicId = topic.TopicId, GroupId = gid });
                }
            }

            // Cập nhật lại các tag
            _context.TopicTags.RemoveRange(topic.TopicTags);
            if (selectedTagIds != null && selectedTagIds.Any())
            {
                foreach (var tagId in selectedTagIds)
                {
                    topic.TopicTags.Add(new TopicTag { TopicId = topic.TopicId, TagId = tagId });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home", new { tab = "ChuDe" });
        }


        // GET: Topics/Delete/5
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

            var topic = await _context.Topics
                .Include(t => t.User)
                .Include(t => t.Comments)
                .Include(t => t.TopicTags)
                .Include(t => t.TopicUsers)
                .Include(t => t.TopicGroups)
                .FirstOrDefaultAsync(m => m.TopicId == id);

            if (topic == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền xóa
            if (CurrentUserRole != "Admin" && topic.UserId.ToString() != CurrentUserID)
            {
                TempData["Error"] = "Bạn không có quyền xóa chủ đề này.";
                return RedirectToAction(nameof(Index));
            }

            return View(topic);
        }

        // POST: Topics/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                var topic = await _context.Topics
                    .Include(t => t.TopicTags)
                    .Include(t => t.TopicUsers)
                    .Include(t => t.TopicGroups)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.Replies)
                    .Include(t => t.Notifications)
                    .FirstOrDefaultAsync(t => t.TopicId == id);

                if (topic != null)
                {
                    // Kiểm tra quyền xóa
                    if (CurrentUserRole != "Admin" && topic.UserId.ToString() != CurrentUserID)
                    {
                        TempData["Error"] = "Bạn không có quyền xóa chủ đề này.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Xóa các like liên quan
                    var likes = _context.LikeTopics.Where(l => l.TopicId == topic.TopicId);
                    _context.LikeTopics.RemoveRange(likes);

                    // Xóa các save liên quan
                    var saves = _context.Saves.Where(s => s.TopicId == topic.TopicId);
                    _context.Saves.RemoveRange(saves);

                    // Xóa các file vật lý nếu có
                    try
                    {
                        if (!string.IsNullOrEmpty(topic.ImageUrl))
                        {
                            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, topic.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi xóa ảnh chủ đề: " + ex.Message);
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(topic.FileUrl))
                        {
                            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, topic.FileUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi xóa file chủ đề: " + ex.Message);
                    }

                    // Xóa tất cả các bình luận và replies
                    if (topic.Comments != null)
                    {
                        foreach (var comment in topic.Comments)
                        {
                            // Xóa ảnh của replies nếu có
                            if (comment.Replies != null)
                            {
                                foreach (var reply in comment.Replies)
                                {
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(reply.ImageUrl))
                                        {
                                            var replyImagePath = Path.Combine(_webHostEnvironment.WebRootPath, reply.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                                            if (System.IO.File.Exists(replyImagePath))
                                            {
                                                System.IO.File.Delete(replyImagePath);
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Lỗi xóa ảnh reply: " + ex.Message);
                                    }
                                }
                                _context.Comments.RemoveRange(comment.Replies);
                            }

                            // Xóa ảnh của comment nếu có
                            try
                            {
                                if (!string.IsNullOrEmpty(comment.ImageUrl))
                                {
                                    var commentImagePath = Path.Combine(_webHostEnvironment.WebRootPath, comment.ImageUrl.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                                    if (System.IO.File.Exists(commentImagePath))
                                    {
                                        System.IO.File.Delete(commentImagePath);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Lỗi xóa ảnh comment: " + ex.Message);
                            }
                        }
                        _context.Comments.RemoveRange(topic.Comments);
                    }

                    // Xóa các liên kết trong bảng TopicTags
                    if (topic.TopicTags != null)
                    {
                        _context.TopicTags.RemoveRange(topic.TopicTags);
                    }

                    // Xóa các liên kết trong bảng TopicUsers
                    if (topic.TopicUsers != null)
                    {
                        _context.TopicUsers.RemoveRange(topic.TopicUsers);
                    }

                    // Xóa các liên kết trong bảng TopicGroups
                    if (topic.TopicGroups != null)
                    {
                        _context.TopicGroups.RemoveRange(topic.TopicGroups);
                    }

                    // Xóa các thông báo liên quan
                    if (topic.Notifications != null)
                    {
                        _context.Notifications.RemoveRange(topic.Notifications);
                    }

                    // Cuối cùng xóa Topic
                    _context.Topics.Remove(topic);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Index", "Home", new { tab = "ChuDe" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xóa chủ đề: " + ex.ToString());
                return StatusCode(500, "Lỗi xóa chủ đề: " + ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMultiple(int[] selectedTopics)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            if (selectedTopics == null || selectedTopics.Length == 0)
            {
                TempData["ErrorMessage"] = "Bạn chưa chọn chủ đề nào để xóa.";
                return RedirectToAction("Index");
            }

            // Kiểm tra quyền xóa cho từng chủ đề
            if (CurrentUserRole != "Admin")
            {
                var topics = await _context.Topics
                    .Where(t => selectedTopics.Contains(t.TopicId))
                    .ToListAsync();

                var unauthorizedTopics = topics
                    .Where(t => t.UserId.ToString() != CurrentUserID)
                    .ToList();

                if (unauthorizedTopics.Any())
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xóa một số chủ đề đã chọn.";
                    return RedirectToAction("Index");
                }
            }

            foreach (var id in selectedTopics)
            {
                var topic = await _context.Topics
                    .Include(t => t.TopicTags)
                    .Include(t => t.TopicUsers)
                    .Include(t => t.TopicGroups)
                    .Include(t => t.Comments)
                        .ThenInclude(c => c.Replies)
                    .Include(t => t.Notifications)
                    .FirstOrDefaultAsync(t => t.TopicId == id);

                if (topic != null)
                {
                    // Xóa các like liên quan
                    var likes = _context.LikeTopics.Where(l => l.TopicId == topic.TopicId);
                    _context.LikeTopics.RemoveRange(likes);

                    // Xóa các save liên quan
                    var saves = _context.Saves.Where(s => s.TopicId == topic.TopicId);
                    _context.Saves.RemoveRange(saves);

                    // Xóa các file vật lý nếu có
                    if (!string.IsNullOrEmpty(topic.ImageUrl))
                    {
                        var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, topic.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    if (!string.IsNullOrEmpty(topic.FileUrl))
                    {
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, topic.FileUrl.TrimStart('/'));
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    // Xóa tất cả các thông báo liên quan
                    if (topic.Notifications != null)
                    {
                        _context.Notifications.RemoveRange(topic.Notifications);
                    }

                    // Xóa tất cả các bình luận và replies
                    if (topic.Comments != null)
                    {
                        foreach (var comment in topic.Comments)
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
                                _context.Comments.RemoveRange(comment.Replies);
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
                        _context.Comments.RemoveRange(topic.Comments);
                    }

                    // Xóa các liên kết trong bảng TopicTags
                    if (topic.TopicTags != null)
                    {
                        _context.TopicTags.RemoveRange(topic.TopicTags);
                    }

                    // Xóa các liên kết trong bảng TopicUsers
                    if (topic.TopicUsers != null)
                    {
                        _context.TopicUsers.RemoveRange(topic.TopicUsers);
                    }

                    // Xóa các liên kết trong bảng TopicGroups
                    if (topic.TopicGroups != null)
                    {
                        _context.TopicGroups.RemoveRange(topic.TopicGroups);
                    }

                    // Cuối cùng xóa Topic
                    _context.Topics.Remove(topic);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa thành công các chủ đề đã chọn.";
            return RedirectToAction("Index");
        }
        private bool TopicExists(int id)
        {
            return _context.Topics.Any(e => e.TopicId == id);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(int id)
        {
            if (!IsLogin)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để thích chủ đề" });
            }

            if (!int.TryParse(CurrentUserID, out int currentUserId))
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, vui lòng thử lại" });
            }
            
            var topic = await _context.Topics
                .Include(p => p.LikeTopics)
                .FirstOrDefaultAsync(p => p.TopicId == id);

            if (topic == null)
            {
                return Json(new { success = false, message = "Chủ đề không tồn tại" });
            }

            var existingLike = await _context.LikeTopics
                .FirstOrDefaultAsync(l => l.TopicId == id && l.UserId == currentUserId);

            if (existingLike != null)
            {
                // Unlike
                _context.LikeTopics.Remove(existingLike);
                await _context.SaveChangesAsync();
                
                // Đếm lại số lượt like
                var updatedLikeCount = await _context.LikeTopics.CountAsync(l => l.TopicId == id);
                return Json(new { success = true, likeCount = updatedLikeCount, isLiked = false });
            }
            else
            {
                // Like
                var newLike = new LikeTopic
                {
                    TopicId = id,
                    UserId = currentUserId,
                    CreatedAt = DateTime.Now
                };
                _context.LikeTopics.Add(newLike);
                await _context.SaveChangesAsync();
                
                // Đếm lại số lượt like
                var updatedLikeCount = await _context.LikeTopics.CountAsync(l => l.TopicId == id);
                return Json(new { success = true, likeCount = updatedLikeCount, isLiked = true });
            }
        }
    }
}
