using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("document")]
    public class Document
    {
    [Key]
    [Column("document_id")]
    [DisplayName("Mã tài liệu")]
    public int DocumentId { get; set; }

    [Required]
    [Column("title")]
    [DisplayName("Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    [DisplayName("Nội dung")]
    public string? Content { get; set; }  // nullable để tránh lỗi nếu null từ DB

    [Required]
    [Column("visibility_type")]
    [DisplayName("Loại hiển thị")]
    public string VisibilityType { get; set; } = "public";

    [Column("created_at")]
    [DisplayName("Ngày tạo")]
    public DateTime? CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    [DisplayName("Ngày cập nhật")]
    public DateTime? UpdatedAt { get; set; } = DateTime.Now;

    [Column("image_url")]
    [DisplayName("Ảnh")]
    public string? ImageUrl { get; set; }

    [Column("file_url")]
    [DisplayName("Tệp đính kèm")]
    public string? FileUrl { get; set; }

    [Required]
    [Column("user_id")]
    [DisplayName("Người dùng")]
    public int UserId { get; set; }

    // Navigation property
    [ForeignKey("UserId")]
    public User? User { get; set; }

    public virtual ICollection<DocumentGroup>? DocumentGroups { get; set; }

    public virtual ICollection<DocumentUser>? DocumentUsers { get; set; }

    public virtual ICollection<CommentDocument>? CommentDocuments { get; set; }

    public virtual ICollection<LikeDocument>? LikeDocuments { get; set; }
    }
}
