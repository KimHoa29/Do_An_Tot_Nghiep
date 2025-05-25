// Models/CommentDocument.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("CommentDocument")]
    public class CommentDocument
    {
        [Key]
        [Column("comment_document_id")]
        public int CommentDocumentId { get; set; }

        [ForeignKey("Document")]
        [Column("document_id")]
        public int DocumentId { get; set; }
        public virtual Document? Document { get; set; }

        [ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }
        public virtual User? User { get; set; }

        [Required]
        [Column("content")]
        public string? Content { get; set; }

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
      
        [ForeignKey("ParentComment")]
        public int? ParentCommentId { get; set; }
        public virtual CommentDocument? ParentComment { get; set; }

        public virtual ICollection<CommentDocument> Replies { get; set; } = new List<CommentDocument>();
    }
}