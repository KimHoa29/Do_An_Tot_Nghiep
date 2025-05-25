using System.ComponentModel.DataAnnotations.Schema;

namespace Do_An_Tot_Nghiep.Models
{ 
    [Table("DocumentGroup")]
    public class DocumentGroup
    {
        [Column("document_id")]
        public int DocumentId { get; set; }
        public virtual Document? Document { get; set; }


        [Column("group_id")]
        public int GroupId { get; set; }
        public virtual Group? Group { get; set; }
    }
}
