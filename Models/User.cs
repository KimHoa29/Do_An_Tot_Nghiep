using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    public class User
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("username")]
        public string Username { get; set; } = string.Empty; // Sửa lỗi CS8618

        [Required]
        [Column("password")]
        public string Password { get; set; } = string.Empty; // Sửa lỗi CS8618

        [Required]
        [Column("email")]
        public string Email { get; set; } = string.Empty; // Sửa lỗi CS8618

        [Required]
        [Column("role")]
        public string Role { get; set; } = string.Empty; // Sửa lỗi CS8618

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Column("avatar")]
        public String? Avatar { get; set; } 

        [Column("reset_password_token")]
        public string? ResetPasswordToken { get; set; }

        [Column("reset_password_token_expiry")]
        public DateTime? ResetPasswordTokenExpiry { get; set; }

        public virtual ICollection<GroupUser>? GroupUsers { get; set; }

        public virtual ICollection<TopicUser>? TopicUsers { get; set; }

        public virtual ICollection<DocumentUser>? DocumentUsers { get; set; }
        public virtual ICollection<LikePost>? LikePosts { get; set; }
        public virtual ICollection<LikeTopic>? LikeTopics { get; set; }
        public virtual ICollection<LikeDocument>? LikeDocuments { get; set; }

    }
}
