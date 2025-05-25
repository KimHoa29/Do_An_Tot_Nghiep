namespace Do_An_Tot_Nghiep.Services.Interfaces
{
    public interface IChatOllamaService
    {
        Task<string> ChatOllama(string message);
    }
}
