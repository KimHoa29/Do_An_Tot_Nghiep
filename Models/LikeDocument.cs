using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("LikeDocument")]
    public class LikeDocument
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("likedocument_id")]
        public int LikeDocumentId { get; set; }

        [Required]
        [Display(Name = "Mã người dùng")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Display(Name = "Mã bài viết")]
        [Column("document_id")]
        public int DocumentId { get; set; }

        [Required]
        [Display(Name = "Ngày thích")]
        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Liên kết rõ ràng với khóa ngoại
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("DocumentId")]
        public Document? Document { get; set; }
    }
}
