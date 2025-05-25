using System;
using System.Collections.Generic;
using Do_An_Tot_Nghiep.Models;

namespace Do_An_Tot_Nghiep.ViewModels
{
    public class ChatStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalAnswers { get; set; }
        public List<ChatHistory> RecentChats { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public string UserMessage { get; set; }
        public string BotResponse { get; set; }
        public string SearchTerm { get; set; }
    }
} 