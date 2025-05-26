using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class SavesController : BaseController
    {
        private readonly AppDbContext _context;

        public SavesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("Saves/ToggleSave/{type}/{id}")]
        public async Task<IActionResult> ToggleSave(string type, int id)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ToggleSave] IsLogin: {IsLogin}, CurrentUserID: {CurrentUserID}");
                if (!IsLogin)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện thao tác này.", needLogin = true });
                }
                if (string.IsNullOrEmpty(CurrentUserID) || !int.TryParse(CurrentUserID, out int userIdInt))
                {
                    return Json(new { success = false, message = "Không xác định được tài khoản đăng nhập." });
                }

                if (id == 0)
                {
                    return NotFound();
                }

                var existingSave = await _context.Saves
                    .FirstOrDefaultAsync(s => s.UserId == userIdInt && 
                        ((type == "post" && s.PostId == id) ||
                         (type == "document" && s.DocumentId == id) ||
                         (type == "topic" && s.TopicId == id)));

                if (existingSave != null)
                {
                    // Nếu đã lưu thì xóa
                    _context.Saves.Remove(existingSave);
                    await _context.SaveChangesAsync();

                    var saveCount = await _context.Saves
                        .CountAsync(s => ((type == "post" && s.PostId == id) ||
                                        (type == "document" && s.DocumentId == id) ||
                                        (type == "topic" && s.TopicId == id)));

                    return Json(new { success = true, isSaved = false, saveCount });
                }
                else
                {
                    // Nếu chưa lưu thì thêm mới
                    var save = new Save
                    {
                        UserId = userIdInt,
                        CreatedAt = DateTime.Now
                    };

                    switch (type.ToLower())
                    {
                        case "post":
                            save.PostId = id;
                            break;
                        case "document":
                            save.DocumentId = id;
                            break;
                        case "topic":
                            save.TopicId = id;
                            break;
                        default:
                            return Json(new { success = false, message = "Loại không hợp lệ." });
                    }

                    _context.Saves.Add(save);
                    await _context.SaveChangesAsync();

                    var saveCount = await _context.Saves
                        .CountAsync(s => ((type == "post" && s.PostId == id) ||
                                        (type == "document" && s.DocumentId == id) ||
                                        (type == "topic" && s.TopicId == id)));

                    return Json(new { success = true, isSaved = true, saveCount });
                }
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException != null ? ex.InnerException.Message : "";
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message + " -- Inner: " + inner });
            }
        }

        // Action để xem danh sách các item đã lưu
        public async Task<IActionResult> SavedItems()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }
            if (string.IsNullOrEmpty(CurrentUserID) || !int.TryParse(CurrentUserID, out int userIdInt))
            {
                return RedirectToAction("Login", "Account");
            }
            var savedItems = await _context.Saves
                .Include(s => s.Post)
                    .ThenInclude(p => p.User)
                .Include(s => s.Document)
                    .ThenInclude(d => d.User)
                .Include(s => s.Topic)
                    .ThenInclude(t => t.User)
                .Where(s => s.UserId == userIdInt)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
            return View(savedItems);
        }
    }
} 