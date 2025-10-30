using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedcodeETLProcess.Model.MedcodeModel
{
    [Table("UMR_HISTORY_USER_MEDCODES_ACTIONS", Schema = "dbo.UMR_HISTORY_USER_MEDCODES_ACTIONS")]
    public class MedicalCodesAction
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("UserId")]
        public string UserId { get; set; }

        [Column("Date")]
        public DateTime Date { get; set; }

        [Column("GUID")]
        public Guid? Guid { get; set; }

        [Column("Action")]
        public string Action { get; set; }

        [Column("UMR_HISTORY_USER_MEDCODES_ID")]
        public int? UMRHistoryUserMedcodesId { get; set; }
    }
}
