using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{
    [Table("PostGroup")]
    public class PostGroup
    {
        [Column("post_id")]
        public int PostId { get; set; }
        public virtual Post? Post { get; set; }


        [Column("group_id")]
        public int GroupId { get; set; }
        public virtual Group? Group { get; set; }
    }
}
