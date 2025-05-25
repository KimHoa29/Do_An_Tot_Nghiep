using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class NotificationController : BaseController
    {
        private readonly AppDbContext _context;

        public NotificationController(AppDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách thông báo của người dùng
        public async Task<IActionResult> Index()
        {
            var currentUser = _context.Users.FirstOrDefault(m => m.UserId.ToString().Equals(CurrentUserID));
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.UserId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        // Lấy danh sách thông báo gần đây
        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var currentUser = _context.Users.FirstOrDefault(m => m.UserId.ToString().Equals(CurrentUserID));
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
           
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.UserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Title,
                    n.Content,
                    n.CreatedAt,
                    n.IsRead
                })
                .ToListAsync();

            return Json(notifications);
        }

        // Đánh dấu thông báo đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Đánh dấu tất cả thông báo đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var currentUser = _context.Users.FirstOrDefault(m => m.UserId.ToString().Equals(CurrentUserID));
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.UserId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Lấy số lượng thông báo chưa đọc
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUser = _context.Users.FirstOrDefault(m => m.UserId.ToString().Equals(CurrentUserID));
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == currentUser.UserId && !n.IsRead);

            return Json(new { count });
        }
    }
} 