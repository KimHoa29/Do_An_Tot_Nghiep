using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("LecturerStudents")] // Đảm bảo đúng tên bảng
    public class LecturerStudent
    {
        [Column("lecturer_id")]  // Nếu SQL Server có tên khác, sửa lại đây
        public int LecturerId { get; set; }

        [Column("student_id")]  // Nếu SQL Server có tên khác, sửa lại đây
        public int StudentId { get; set; }

        public virtual Lecturer Lecturer { get; set; }
        public virtual Student Student { get; set; }

    }
}
