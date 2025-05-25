using Microsoft.EntityFrameworkCore;
using Do_An_Tot_Nghiep.Models;

namespace Do_An_Tot_Nghiep.Services
{
    public interface IDocumentService
    {
        Task<List<Document>> SearchDocumentsAsync(string searchTerm);
        Task<List<Document>> GetAllDocumentsAsync();
    }

    public class DocumentService : IDocumentService
    {
        private readonly AppDbContext _context;

        public DocumentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Document>> SearchDocumentsAsync(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return new List<Document>();

            return await _context.Documents
                .Where(d => d.Title.Contains(searchTerm) || 
                           d.Content.Contains(searchTerm))
                .Take(5) // Giới hạn 5 kết quả để tránh quá tải
                .ToListAsync();
        }

        public async Task<List<Document>> GetAllDocumentsAsync()
        {
            return await _context.Documents.ToListAsync();
        }
    }
} 