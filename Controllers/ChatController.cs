using Microsoft.AspNetCore.Mvc;
using Do_An_Tot_Nghiep.Services;

namespace Do_An_Tot_Nghiep.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : BaseController
    {
        private readonly IGeminiService _geminiService;

        public ChatController(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest("Message cannot be empty");
            }

            // Lấy thông tin người dùng từ session thông qua BaseController
            var userId = CurrentUserID ?? "0";
            var username = CurrentUserName ?? "guest";

            var response = await _geminiService.GenerateResponseAsync(message, userId, username);
            return Ok(new { response });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = CurrentUserID ?? "0";
            var history = await _geminiService.GetHistoryAsync(userId);
            return Ok(history.Select(x => new { role = x.Role, message = x.Message }));
        }

        [HttpGet("db-history")]
        public async Task<IActionResult> GetDBHistory()
        {
            var userId = CurrentUserID ?? "0";
            var history = await _geminiService.GetChatHistoryFromDBAsync(userId);
            return Ok(history.Select(x => new 
            { 
                id = x.Id,
                userId = x.UserId,
                username = x.Username,
                question = x.Question,
                answer = x.Answer,
                createdAt = x.CreatedAt
            }));
        }

        [HttpGet("paginated-history")]
        public async Task<IActionResult> GetPaginatedHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50; // Giới hạn số lượng bản ghi mỗi trang

            var userId = CurrentUserID ?? "0";
            var (items, totalCount) = await _geminiService.GetPaginatedChatHistoryAsync(userId, pageNumber, pageSize);

            return Ok(new
            {
                items = items.Select(x => new
                {
                    id = x.Id,
                    userId = x.UserId,
                    username = x.Username,
                    question = x.Question,
                    answer = x.Answer,
                    createdAt = x.CreatedAt
                }),
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        [HttpGet("search-history")]
        public async Task<IActionResult> SearchHistory([FromQuery] string searchTerm, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return BadRequest("Search term cannot be empty");

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var userId = CurrentUserID ?? "0";
            var (items, totalCount) = await _geminiService.SearchChatHistoryAsync(userId, searchTerm, pageNumber, pageSize);

            return Ok(new
            {
                items = items.Select(x => new
                {
                    id = x.Id,
                    userId = x.UserId,
                    username = x.Username,
                    question = x.Question,
                    answer = x.Answer,
                    createdAt = x.CreatedAt
                }),
                totalCount,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        [HttpDelete("history/{id}")]
        public async Task<IActionResult> DeleteHistory(int id)
        {
            var userId = CurrentUserID ?? "0";
            var result = await _geminiService.DeleteChatHistoryAsync(id, userId);
            
            if (!result)
                return NotFound("Chat history not found or you don't have permission to delete it");

            return Ok(new { message = "Chat history deleted successfully" });
        }

        [HttpDelete("history")]
        public async Task<IActionResult> DeleteAllHistory()
        {
            var userId = CurrentUserID ?? "0";
            var result = await _geminiService.DeleteAllChatHistoryAsync(userId);
            
            if (!result)
                return NotFound("No chat history found");

            return Ok(new { message = "All chat history deleted successfully" });
        }
    }
} 