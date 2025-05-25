namespace Do_An_Tot_Nghiep.Models
{
    public class AddStudentViewModel
    {
        public int LecturerId { get; set; }  // Map với lecturer_id
        public string FullName { get; set; } // Map với full_name
        public List<Student> Students { get; set; } = new List<Student>();
        public List<int> SelectedStudentIds { get; set; } = new List<int>();
    }
}
