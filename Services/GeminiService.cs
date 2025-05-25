using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Do_An_Tot_Nghiep.Services;
using Do_An_Tot_Nghiep.Models;
using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.ViewModels;

namespace Do_An_Tot_Nghiep.Services
{
    public interface IGeminiService
    {
        Task<string> GenerateResponseAsync(string prompt, string userId, string username);
        Task<List<(string Role, string Message)>> GetHistoryAsync(string userId);
        Task<List<ChatHistory>> GetChatHistoryFromDBAsync(string userId);
        Task<(List<ChatHistory> Items, int TotalCount)> GetPaginatedChatHistoryAsync(string userId, int pageNumber, int pageSize);
        Task<(List<ChatHistory> Items, int TotalCount)> SearchChatHistoryAsync(string userId, string searchTerm, int pageNumber, int pageSize);
        Task<bool> DeleteChatHistoryAsync(int chatId, string userId);
        Task<bool> DeleteAllChatHistoryAsync(string userId);
        Task<ChatStatisticsViewModel> GetChatStatisticsAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null);
    }

    public class GeminiService : IGeminiService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private readonly IDocumentService _documentService;
        private readonly AppDbContext _dbContext;
        // Lưu lịch sử chat theo userId
        private static ConcurrentDictionary<string, List<(string Role, string Message)>> _userChatHistory = new();

        public GeminiService(IDocumentService documentService, AppDbContext dbContext)
        {
            _apiKey = "AIzaSyBGSlOFGQO930yPW4vXsmgbIj5tOif68iY";
            _httpClient = new HttpClient();
            _documentService = documentService;
            _dbContext = dbContext;
        }

        public async Task<List<(string Role, string Message)>> GetHistoryAsync(string userId)
        {
            if (_userChatHistory.TryGetValue(userId, out var history))
                return history;
            return new List<(string, string)>();
        }

        public async Task<List<ChatHistory>> GetChatHistoryFromDBAsync(string userId)
        {
            return await _dbContext.ChatHistories
                .Where(ch => ch.UserId.ToString() == userId)
                .OrderByDescending(ch => ch.CreatedAt)
                .ToListAsync();
        }

        public async Task<(List<ChatHistory> Items, int TotalCount)> GetPaginatedChatHistoryAsync(string userId, int pageNumber, int pageSize)
        {
            var query = _dbContext.ChatHistories
                .Where(ch => ch.UserId.ToString() == userId)
                .OrderByDescending(ch => ch.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<ChatHistory> Items, int TotalCount)> SearchChatHistoryAsync(string userId, string searchTerm, int pageNumber, int pageSize)
        {
            var query = _dbContext.ChatHistories
                .Where(ch => ch.UserId.ToString() == userId &&
                           (ch.Question.Contains(searchTerm) || ch.Answer.Contains(searchTerm)))
                .OrderByDescending(ch => ch.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> DeleteChatHistoryAsync(int chatId, string userId)
        {
            var chatHistory = await _dbContext.ChatHistories
                .FirstOrDefaultAsync(ch => ch.Id == chatId && ch.UserId.ToString() == userId);

            if (chatHistory == null)
                return false;

            _dbContext.ChatHistories.Remove(chatHistory);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAllChatHistoryAsync(string userId)
        {
            var chatHistories = await _dbContext.ChatHistories
                .Where(ch => ch.UserId.ToString() == userId)
                .ToListAsync();

            _dbContext.ChatHistories.RemoveRange(chatHistories);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<string> GenerateResponseAsync(string prompt, string userId, string username)
        {
            try
            {
                if (!_userChatHistory.ContainsKey(userId))
                    _userChatHistory[userId] = new List<(string, string)>();
                _userChatHistory[userId].Add(("user", prompt));

                // Tìm kiếm thông tin từ CSDL
                var relatedDocs = await _documentService.SearchDocumentsAsync(prompt);
                
                // Tạo context từ CSDL
                var contextFromDB = "";
                if (relatedDocs.Any())
                {
                    contextFromDB = "Thông tin từ cơ sở dữ liệu:\n";
                    foreach (var doc in relatedDocs)
                    {
                        contextFromDB += $"- Tiêu đề: {doc.Title}\n";
                        contextFromDB += $"  Nội dung: {doc.Content}\n";
                        contextFromDB += "\n";
                    }
                }

                // Tạo prompt với context từ CSDL
                var enhancedPrompt = "";
                if (!string.IsNullOrEmpty(contextFromDB))
                {
                    enhancedPrompt = $"{contextFromDB}\n\nDựa trên thông tin trên, hãy trả lời câu hỏi sau: {prompt}";
                }
                else
                {
                    // Trả về thông báo nếu không có dữ liệu trong CSDL
                    var notFoundMsg = "Chúng tôi chưa ghi nhận dữ liệu.";
                    _userChatHistory[userId].Add(("bot", notFoundMsg));
                    return notFoundMsg;
                }

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = enhancedPrompt }
                            }
                        }
                    }
                };
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(
                    $"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-pro:generateContent?key={_apiKey}",
                    content);
                if (!response.IsSuccessStatusCode)
                {
                    string errorMsg;
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        errorMsg = "Bạn đã gửi quá nhiều yêu cầu. Vui lòng chờ một lát rồi thử lại!";
                    }
                    else
                    {
                        errorMsg = $"Error: {response.StatusCode}";
                    }
                    _userChatHistory[userId].Add(("bot", errorMsg));
                    return errorMsg;
                }
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var botMsg = responseObject.GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                _userChatHistory[userId].Add(("bot", botMsg));

                // Lưu lịch sử chat vào database
                var chatHistory = new ChatHistory
                {
                    UserId = int.Parse(userId),
                    Username = username,
                    Question = prompt,
                    Answer = botMsg,
                    CreatedAt = DateTime.Now
                };
                _dbContext.ChatHistories.Add(chatHistory);
                await _dbContext.SaveChangesAsync();

                return botMsg;
            }
            catch (Exception ex)
            {
                if (!_userChatHistory.ContainsKey(userId))
                    _userChatHistory[userId] = new List<(string, string)>();
                _userChatHistory[userId].Add(("bot", $"Error: {ex.Message}"));
                return $"Error: {ex.Message}";
            }
        }

        public async Task<ChatStatisticsViewModel> GetChatStatisticsAsync(int pageNumber = 1, int pageSize = 10, string searchTerm = null)
        {
            var totalUsers = await _dbContext.ChatHistories.Select(ch => ch.UserId).Distinct().CountAsync();
            var totalQuestions = await _dbContext.ChatHistories.CountAsync();
            var totalAnswers = totalQuestions; // Mỗi câu hỏi có một câu trả lời

            var query = _dbContext.ChatHistories.AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(ch => ch.Username.Contains(searchTerm) || ch.Question.Contains(searchTerm) || ch.Answer.Contains(searchTerm));
            }
            query = query.OrderByDescending(ch => ch.CreatedAt);

            var totalItems = await query.CountAsync();
            var recentChats = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ChatStatisticsViewModel
            {
                TotalUsers = totalUsers,
                TotalQuestions = totalQuestions,
                TotalAnswers = totalAnswers,
                RecentChats = recentChats,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                TotalItems = totalItems,
                SearchTerm = searchTerm
            };
        }
    }
} 