using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("PostUser")]
    public class PostUser
    {

        [Column("post_id")]
        public int PostId { get; set; }
        public virtual Post? Post { get; set; }


        [Column("user_id")]
        public int UserId { get; set; }
        public virtual User? User { get; set; }
    }
}
