using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("GroupUser")]
    public class GroupUser
    {
        [ForeignKey("Group")]
        [Column("group_id")]
        public int GroupId { get; set; }

        [ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("joined_at")]
        public DateTime JoinedAt { get; set; } = DateTime.Now;

        public virtual Group? Group { get; set; }
        public virtual User? User { get; set; }
    }

}
