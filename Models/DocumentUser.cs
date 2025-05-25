using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{    
    [Table("DocumentUser")]
    public class DocumentUser
    {

        [Column("document_id")]
        public int DocumentId { get; set; }
        public virtual Document? Document { get; set; }


        [Column("user_id")]
        public int UserId { get; set; }
        public virtual User? User { get; set; }
    }
}