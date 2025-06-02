using Microsoft.AspNetCore.Mvc;
using Do_An_Tot_Nghiep.Services;
using Do_An_Tot_Nghiep.ViewModels;

namespace Do_An_Tot_Nghiep.Controllers
{
    public class ChatStatisticsController : BaseController
    {
        private readonly IGeminiService _geminiService;

        public ChatStatisticsController(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        public async Task<IActionResult> Index(int page = 1, string searchTerm = null)
        {
            if (!IsLogin || CurrentUserRole != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }
            var statistics = await _geminiService.GetChatStatisticsAsync(page, 10, searchTerm);
            return View(statistics);
        }
    }
} 