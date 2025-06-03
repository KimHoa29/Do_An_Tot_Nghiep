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
    public class GroupsController : BaseController
    {
        private readonly AppDbContext _context;

        public GroupsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Groups
        public async Task<IActionResult> Index(string searchString)
        {
            if (!IsLogin || CurrentUserRole.Equals("Student"))
            {
                return RedirectToAction("Login", "Account");
            }

            var groupsQuery = _context.Groups
                                    .Include(g => g.GroupUsers)
                                    .Include(g => g.CreatorUser)
                                    .AsQueryable();

            // Lấy vai trò và ID của người dùng hiện tại, ưu tiên TempData nếu có
            var currentUserRole = TempData["CurrentUserRole"] as string ?? CurrentUserRole;
            var currentUserId = TempData["CurrentUserId"] as string ?? CurrentUserID;

            // Áp dụng bộ lọc nhóm dựa trên vai trò người dùng
            if (currentUserRole == "Student")
            {
                // Sinh viên chỉ thấy nhóm mình là thành viên
                if (!string.IsNullOrEmpty(currentUserId) && int.TryParse(currentUserId, out int userId))
                {
                    groupsQuery = groupsQuery.Where(g => g.GroupUsers.Any(gu => gu.UserId == userId));
                }
                else
                {
                    // Nếu không xác định được UserID của sinh viên, trả về danh sách rỗng
                    groupsQuery = groupsQuery.Where(g => false);
                }
            }
            // Giảng viên và Admin thấy tất cả các nhóm (không cần thêm điều kiện lọc theo vai trò)

            if (!string.IsNullOrEmpty(searchString))
            {
                groupsQuery = groupsQuery.Where(g =>
                    g.GroupName.Contains(searchString) ||
                    g.Description.Contains(searchString));
            }

            var groups = await groupsQuery.ToListAsync();

            // Truyền vai trò người dùng hiện tại đến View để xử lý hiển thị nút
            ViewBag.CurrentUserRole = currentUserRole;
            ViewBag.CurrentUserId = currentUserId;

            return View(groups);
        }

        // GET: Groups/StudentIndex
        public async Task<IActionResult> StudentIndex(string searchString, int myGroupsPage = 1, int joinedGroupsPage = 1)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra xem người dùng có phải là sinh viên không
            if (CurrentUserRole != "Student")
            {
                return RedirectToAction("Index");
            }

            var currentUserId = CurrentUserID;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToAction("Login", "Account");
            }

            var groupsQuery = _context.Groups
                                    .Include(g => g.GroupUsers)
                                    .Include(g => g.CreatorUser)
                                    .AsQueryable();

            // Lọc nhóm mà sinh viên là thành viên
            if (int.TryParse(currentUserId, out int userId))
            {
                groupsQuery = groupsQuery.Where(g => g.GroupUsers.Any(gu => gu.UserId == userId));
            }
            else
            {
                groupsQuery = groupsQuery.Where(g => false);
            }

            // Tìm kiếm theo tên hoặc mô tả
            if (!string.IsNullOrEmpty(searchString))
            {
                groupsQuery = groupsQuery.Where(g =>
                    g.GroupName.Contains(searchString) ||
                    g.Description.Contains(searchString));
            }

            var allGroups = await groupsQuery.ToListAsync();

            // Phân trang cho nhóm của tôi
            var myGroups = allGroups.Where(g => g.CreatorUserId == userId).ToList();
            var myGroupsPageSize = 6;
            var myGroupsTotalPages = (int)Math.Ceiling(myGroups.Count / (double)myGroupsPageSize);
            myGroupsPage = Math.Max(1, Math.Min(myGroupsPage, myGroupsTotalPages));
            var myGroupsPaged = myGroups.Skip((myGroupsPage - 1) * myGroupsPageSize).Take(myGroupsPageSize).ToList();

            // Phân trang cho nhóm tôi tham gia
            var joinedGroups = allGroups.Where(g => g.CreatorUserId != userId).ToList();
            var joinedGroupsPageSize = 6;
            var joinedGroupsTotalPages = (int)Math.Ceiling(joinedGroups.Count / (double)joinedGroupsPageSize);
            joinedGroupsPage = Math.Max(1, Math.Min(joinedGroupsPage, joinedGroupsTotalPages));
            var joinedGroupsPaged = joinedGroups.Skip((joinedGroupsPage - 1) * joinedGroupsPageSize).Take(joinedGroupsPageSize).ToList();

            // Truyền thông tin phân trang đến View
            ViewBag.MyGroupsPage = myGroupsPage;
            ViewBag.MyGroupsTotalPages = myGroupsTotalPages;
            ViewBag.JoinedGroupsPage = joinedGroupsPage;
            ViewBag.JoinedGroupsTotalPages = joinedGroupsTotalPages;
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.SearchString = searchString;

            // Truyền danh sách đã phân trang
            ViewBag.MyGroups = myGroupsPaged;
            ViewBag.JoinedGroups = joinedGroupsPaged;

            return View(allGroups);
        }

        public async Task<IActionResult> Details(int? id, string searchMember = null)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null) return NotFound();

            var group = await _context.Groups
                .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
                .FirstOrDefaultAsync(m => m.GroupId == id);

            if (group == null) return NotFound();

            // Lọc thành viên theo từ khóa nếu có
            if (!string.IsNullOrEmpty(searchMember))
            {
                group.GroupUsers = group.GroupUsers
                    .Where(gu => gu.User.Username != null && gu.User.Username.ToLower().Contains(searchMember.ToLower()))
                    .ToList();
            }

            // Danh sách user chưa thuộc group
            var userIdsInGroup = group.GroupUsers.Select(gu => gu.UserId).ToList();
            var usersNotInGroup = await _context.Users
                .Where(u => !userIdsInGroup.Contains(u.UserId) && u.Role != "Admin")
                .ToListAsync();

            ViewBag.UsersNotInGroup = new SelectList(usersNotInGroup, "UserId", "Username");
            ViewBag.CreatorUserId = group.CreatorUserId;
            ViewBag.CurrentUserId = CurrentUserID;
            ViewBag.SearchMember = searchMember;
            ViewBag.CurrentUserRole = CurrentUserRole;

            return View(group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToGroup(int groupId, int userId)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            var exists = await _context.GroupUsers
                .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == userId);
            if (!exists)
            {
                var groupUser = new GroupUser
                {
                    GroupId = groupId,
                    UserId = userId,
                    JoinedAt = DateTime.Now
                };

                _context.GroupUsers.Add(groupUser);
                await _context.SaveChangesAsync();

                // Create notification for the user who was added to the group
                var group = await _context.Groups
                    .FirstOrDefaultAsync(g => g.GroupId == groupId);

                if (group != null)
                {
                    var notification = new Notification
                    {
                        UserId = userId,
                        Title = "Thêm vào nhóm",
                        Content = $"Bạn đã được thêm vào nhóm: {group.GroupName}",
                        Type = 2, // 2 for group
                        GroupId = groupId,
                        Path = $"/Groups/Details/{groupId}",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Details", new { id = groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveUserFromGroup(int groupId, int userId)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            var groupUser = await _context.GroupUsers
                .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == userId);

            if (groupUser != null)
            {
                _context.GroupUsers.Remove(groupUser);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = groupId });
        }

        // GET: Groups/Create
        public IActionResult Create()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }
            // Lưu URL trang trước vào ViewBag
            ViewBag.ReturnUrl = Request.Headers["Referer"].ToString();
            return View();
        }

        // POST: Groups/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GroupId,GroupName,Description,CreatedAt")] Group @group, string ReturnUrl)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                // Lấy ID của người dùng hiện tại
                var currentUserId = CurrentUserID;

                if (!string.IsNullOrEmpty(currentUserId) && int.TryParse(currentUserId, out int userId))
                {
                    // Gán CreatorUserId cho nhóm
                    @group.CreatorUserId = userId;

                    _context.Add(@group);
                    await _context.SaveChangesAsync();

                    // Thêm người tạo vào nhóm làm thành viên
                    var groupUser = new GroupUser
                    {
                        GroupId = @group.GroupId,
                        UserId = userId,
                        JoinedAt = DateTime.Now
                    };
                    _context.GroupUsers.Add(groupUser);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Tạo nhóm thành công!"; // Có thể thêm thông báo
                    // Nếu có ReturnUrl và nó khác rỗng, chuyển về đó
                    if (!string.IsNullOrEmpty(ReturnUrl))
                        return Redirect(ReturnUrl);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // Xử lý trường hợp không lấy được User ID (có thể đăng nhập chưa hoàn chỉnh)
                    TempData["Error"] = "Không xác định được người dùng tạo nhóm.";
                    ModelState.AddModelError("", "Không xác định được người dùng tạo nhóm.");
                    return View(@group);
                }
            }
            return View(@group);
        }

        // GET: Groups/Edit/5
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

            var @group = await _context.Groups.FindAsync(id);
            if (@group == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền chỉnh sửa
            var currentUserId = CurrentUserID;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            // Cho phép Admin và Giảng viên chỉnh sửa tất cả nhóm
            if (CurrentUserRole != "Admin" && CurrentUserRole != "Lecturer" && @group.CreatorUserId != userId)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            // Lưu URL trang trước vào ViewBag
            ViewBag.ReturnUrl = Request.Headers["Referer"].ToString();

            return View(@group);
        }

        // POST: Groups/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GroupId,GroupName,Description,CreatedAt")] Group @group, string ReturnUrl)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id != @group.GroupId)
            {
                return NotFound();
            }

            // Kiểm tra quyền chỉnh sửa
            var currentUserId = CurrentUserID;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            var existingGroup = await _context.Groups.FindAsync(id);
            if (existingGroup == null)
            {
                return NotFound();
            }

            // Cho phép Admin và Giảng viên chỉnh sửa tất cả nhóm
            if (CurrentUserRole != "Admin" && CurrentUserRole != "Lecturer" && existingGroup.CreatorUserId != userId)
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật các trường cần thiết
                    existingGroup.GroupName = @group.GroupName;
                    existingGroup.Description = @group.Description;
                    existingGroup.CreatedAt = @group.CreatedAt;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupExists(@group.GroupId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["CurrentUserRole"] = CurrentUserRole;
                TempData["CurrentUserId"] = CurrentUserID;

                // Nếu có ReturnUrl và nó khác rỗng, chuyển về đó
                if (!string.IsNullOrEmpty(ReturnUrl))
                    return Redirect(ReturnUrl);

                return RedirectToAction(nameof(Index));
            }
            return View(@group);
        }

        // GET: Groups/Delete/5
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

            var @group = await _context.Groups
                .FirstOrDefaultAsync(m => m.GroupId == id);
            if (@group == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền xóa
            var currentUserId = CurrentUserID;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                TempData["Error"] = "Bạn không có quyền xóa nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            // Cho phép Admin và Giảng viên xóa tất cả nhóm
            if (CurrentUserRole != "Admin" && CurrentUserRole != "Lecturer" && @group.CreatorUserId != userId)
            {
                TempData["Error"] = "Bạn không có quyền xóa nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            return View(@group);
        }

        // POST: Groups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra quyền xóa
            var currentUserId = CurrentUserID;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                TempData["Error"] = "Bạn không có quyền xóa nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            var group = await _context.Groups.FindAsync(id);
            if (group == null)
            {
                return NotFound();
            }

            // Cho phép Admin và Giảng viên xóa tất cả nhóm
            if (CurrentUserRole != "Admin" && CurrentUserRole != "Lecturer" && group.CreatorUserId != userId)
            {
                TempData["Error"] = "Bạn không có quyền xóa nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            // Tìm tất cả các bản ghi GroupUser liên quan đến nhóm này
            var groupUsers = await _context.GroupUsers
                .Where(gu => gu.GroupId == id)
                .ToListAsync();

            // Xóa tất cả các bản ghi GroupUser liên quan
            if (groupUsers.Any())
            {
                _context.GroupUsers.RemoveRange(groupUsers);
            }

            // Tìm và xóa nhóm
            if (group != null)
            {
                _context.Groups.Remove(group);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Nhóm đã được xóa thành công!";
            return RedirectToAction(nameof(Index));
        }

        private bool GroupExists(int id)
        {
            return _context.Groups.Any(e => e.GroupId == id);
        }

        public IActionResult AddUsers(int id, string searchString, int page = 1)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            int pageSize = 10;
            var group = _context.Groups.Find(id);
            if (group == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền thêm thành viên
            var currentUserId = CurrentUserID;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId) || group.CreatorUserId != userId)
            {
                TempData["Error"] = "Bạn không có quyền thêm thành viên vào nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            var usersQuery = _context.Users
                .Where(u => !_context.GroupUsers.Any(gu => gu.UserId == u.UserId && gu.GroupId == id) && u.Role != "Admin");

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u => u.Username.Contains(searchString));
            }

            int totalUsers = usersQuery.Count();
            var usersNotInGroup = usersQuery
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Role
                })
                .ToList();

            ViewBag.UsersNotInGroup = usersNotInGroup;
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            return View(group);
        }

        [HttpPost]
        public IActionResult AddUsersToGroup(int groupId, List<int> userIds)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra quyền thêm thành viên
            var currentUserId = CurrentUserID;
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int currentUserIdInt))
            {
                TempData["Error"] = "Bạn không có quyền thêm thành viên vào nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            var group = _context.Groups.Find(groupId);
            if (group == null || group.CreatorUserId != currentUserIdInt)
            {
                TempData["Error"] = "Bạn không có quyền thêm thành viên vào nhóm này.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid || userIds == null || !userIds.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một người dùng để thêm vào nhóm!";
                return RedirectToAction(nameof(AddUsers), new { id = groupId });
            }

            foreach (var memberId in userIds)
            {
                if (!_context.GroupUsers.Any(gu => gu.UserId == memberId && gu.GroupId == groupId))
                {
                    var groupUser = new GroupUser
                    {
                        UserId = memberId,
                        GroupId = groupId,
                        JoinedAt = DateTime.Now
                    };
                    _context.GroupUsers.Add(groupUser);

                    // Create notification for the user who was added to the group
                    var notification = new Notification
                    {
                        UserId = memberId,
                        Title = "Thêm vào nhóm",
                        Content = $"Bạn đã được thêm vào nhóm: {group.GroupName}",
                        Type = 2, // 2 for group
                        GroupId = groupId,
                        Path = $"/Groups/Details/{groupId}",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.Notifications.Add(notification);
                }
            }

            try
            {
                _context.SaveChanges();
                TempData["Success"] = "Đã thêm người dùng vào nhóm thành công!";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }
            catch (Exception)
            {
                TempData["Error"] = "Có lỗi xảy ra khi thêm người dùng vào nhóm!";
                return RedirectToAction(nameof(AddUsers), new { id = groupId });
            }
        }

        // POST: Groups/LeaveGroup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LeaveGroup(int groupId)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = CurrentUserID; // Lấy ID người dùng hiện tại từ Session

            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId))
            {
                TempData["Error"] = "Không xác định được người dùng hiện tại.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra xem người dùng có phải là người tạo nhóm không
            var group = await _context.Groups.FindAsync(groupId);
            if (group != null && group.CreatorUserId == userId)
            {
                TempData["Error"] = "Người tạo nhóm không thể rời nhóm. Vui lòng xóa nhóm nếu muốn hủy.";
                return RedirectToAction(nameof(Index));
            }

            // Tìm bản ghi GroupUser cho nhóm và người dùng hiện tại
            var groupUser = await _context.GroupUsers
                .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == userId);

            if (groupUser != null)
            {
                _context.GroupUsers.Remove(groupUser);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã rời nhóm thành công!";
            }
            else
            {
                TempData["Error"] = "Bạn không phải là thành viên của nhóm này.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
