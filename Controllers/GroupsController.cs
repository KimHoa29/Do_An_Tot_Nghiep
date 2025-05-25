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
            var groupsQuery = _context.Groups
                                    .Include(g => g.GroupUsers)
                                    .Include(g => g.CreatorUser)
                                    .AsQueryable();

            // Lấy vai trò và ID của người dùng hiện tại
            var currentUserRole = CurrentUserRole;
            var currentUserId = CurrentUserID;

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

            return View(groups);
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var group = await _context.Groups
                .Include(g => g.GroupUsers)
                .ThenInclude(gu => gu.User)
                .FirstOrDefaultAsync(m => m.GroupId == id);

            if (group == null) return NotFound();

            // Danh sách user chưa thuộc group
            var userIdsInGroup = group.GroupUsers.Select(gu => gu.UserId).ToList();
            var usersNotInGroup = await _context.Users
                .Where(u => !userIdsInGroup.Contains(u.UserId) && u.Role != "Admin")
                .ToListAsync();

            ViewBag.UsersNotInGroup = new SelectList(usersNotInGroup, "UserId", "Username");

            return View(group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddUserToGroup(int groupId, int userId)
        {
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
            return View();
        }

        // POST: Groups/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GroupId,GroupName,Description,CreatedAt")] Group @group)
        {
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
            if (id == null)
            {
                return NotFound();
            }

            var @group = await _context.Groups.FindAsync(id);
            if (@group == null)
            {
                return NotFound();
            }
            return View(@group);
        }

        // POST: Groups/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GroupId,GroupName,Description,CreatedAt")] Group @group)
        {
            if (id != @group.GroupId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@group);
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
                return RedirectToAction(nameof(Index));
            }
            return View(@group);
        }

        // GET: Groups/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
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

            return View(@group);
        }

        // POST: Groups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
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
            var @group = await _context.Groups.FindAsync(id);
            if (@group != null)
            {
                _context.Groups.Remove(@group);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Nhóm đã được xóa thành công!"; // Thêm thông báo xóa thành công
            return RedirectToAction(nameof(Index));
        }

        private bool GroupExists(int id)
        {
            return _context.Groups.Any(e => e.GroupId == id);
        }

        public IActionResult AddUsers(int id)
        {
            var group = _context.Groups.Find(id);
            if (group == null)
            {
                return NotFound();
            }

            // Lấy danh sách người dùng chưa trong nhóm và không phải là Admin
            var usersNotInGroup = _context.Users
                .Where(u => !_context.GroupUsers.Any(gu => gu.UserId == u.UserId && gu.GroupId == id) && u.Role != "Admin")
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Role
                })
                .ToList();

            ViewBag.UsersNotInGroup = usersNotInGroup;
            return View(group);
        }

        [HttpPost]
        public IActionResult AddUsersToGroup(int groupId, List<int> userIds)
        {
            if (!ModelState.IsValid || userIds == null || !userIds.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một người dùng để thêm vào nhóm!";
                return RedirectToAction(nameof(AddUsers), new { id = groupId });
            }

            var group = _context.Groups.Find(groupId);
            if (group == null)
            {
                return NotFound();
            }

            foreach (var userId in userIds)
            {
                if (!_context.GroupUsers.Any(gu => gu.UserId == userId && gu.GroupId == groupId))
                {
                    var groupUser = new GroupUser
                    {
                        UserId = userId,
                        GroupId = groupId,
                        JoinedAt = DateTime.Now
                    };
                    _context.GroupUsers.Add(groupUser);
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
            var currentUserId = CurrentUserID; // Lấy ID người dùng hiện tại từ Session

            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int userId)){
                 TempData["Error"] = "Không xác định được người dùng hiện tại.";
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
