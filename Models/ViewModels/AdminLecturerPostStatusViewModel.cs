using System;
using System.Collections.Generic;

namespace Do_An_Tot_Nghiep.Models.ViewModels
{
    public class AdminLecturerPostStatusViewModel
    {
        public int PostId { get; set; }
        public string PostTitle { get; set; }
        public DateTime PostCreatedAt { get; set; }
        public int LecturerId { get; set; }
        public string LecturerName { get; set; }
        public string LecturerDepartment { get; set; }
        public bool HasResponded { get; set; }
        public DateTime? ResponseTime { get; set; }
    }
} 