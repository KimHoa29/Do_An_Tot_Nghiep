namespace Do_An_Tot_Nghiep.Models
{
    public class AddStudentViewModel
    {
        public int LecturerId { get; set; }  // Map với lecturer_id
        public string FullName { get; set; } // Map với full_name
        public PaginatedList<Student> Students { get; set; }
        public List<int> SelectedStudentIds { get; set; } = new List<int>();
    }
}
