using System;
using System.Collections.Generic;

namespace Do_An_Tot_Nghiep.Models.ViewModels
{
    public class LecturerPostStatisticsViewModel
    {
        public int LecturerId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public int TotalAssignedPosts { get; set; }
        public int RespondedPosts { get; set; }
        public int PendingPosts { get; set; }
        public List<PostStatistics> PostDetails { get; set; } = new List<PostStatistics>();
    }

    public class PostStatistics
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasResponded { get; set; }
        public DateTime? ResponseTime { get; set; }
    }
} 