using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedcodeETLProcess.Model.MedcodeModel
{
    [Table("UMR_HISTORY_MEDCODEACTION", Schema = "dbo.UMR_HISTORY_MEDCODEACTION")]
    public class MedcodeActionHistory
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("CODE_TYPE")]
        public string CodeType { get; set; }

        [Column("MEDCODE")]
        public string MedCode { get; set; }

        [Column("CODE_VERSION")]
        public int CodeVersion { get; set; }

        [Column("ACTION")]
        public string Action { get; set; }

        [Column("TIME")]
        public DateTime Time { get; set; }
    }
}
