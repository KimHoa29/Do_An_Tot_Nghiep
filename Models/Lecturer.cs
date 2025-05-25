using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("lecturer")]
    public class Lecturer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("lecturer_id")]
        public int LecturerId { get; set; }

        [Display(Name = "Mã người dùng")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Display(Name = "Họ và tên")]
        [Column("full_name")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "Chức vụ")]
        [Column("position")]
        [StringLength(100)]
        public string Position { get; set; }

        [Required]
        [Display(Name = "Khoa")]
        [Column("department")]
        [StringLength(100)]
        public string Department { get; set; }

        [Display(Name = "Chuyên môn")]
        [Column("specialization")]
        [StringLength(200)]
        public string? Specialization { get; set; }

        [Display(Name = "Số điện thoại")]
        [Column("phone")]
        [StringLength(15)]
        public string? Phone { get; set; }

        [Display(Name = "Email")]
        [Column("email")]
        [StringLength(100)]
        public string Email { get; set; }

        [Display(Name = "Ngày tạo")]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("student_count")]
        public int StudentCount { get; set; }

        public ICollection<LecturerStudent>? LecturerStudents { get; set; } = new List<LecturerStudent>();

        public User?  User { get; set; }
    }
}
