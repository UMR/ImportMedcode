using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedcodeETLProcess.Model.MedcodeModel
{
    [Table("UMR_HISTORY_USER_MEDCODES", Schema = "dbo.UMR_HISTORY_USER_MEDCODES")]
    public class MedicalCodeHistory
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("UserId")]
        public string UserId { get; set; }

        [Column("Date")]
        public DateTime Date { get; set; }

        [Column("GUID")]
        public Guid Guid { get; set; }

        [Column("Action")]
        public string Action { get; set; }

        [Column("CODE_TYPE")]
        public string CodeType { get; set; }

        [Column("MEDCODE")]
        public string MedCode { get; set; }

        [Column("SEX_TYPE")]
        public string SexType { get; set; }

        [Column("CODE_VERSION")]
        public int CodeVersion { get; set; }

        [Column("OLD_CODE_VERSION")]
        public int OldCodeVersion { get; set; }

        [Column("UMED_SYS_STAT")]
        public string UmedSysStat { get; set; }

        [Column("SECONDARY_STATUS")]
        public string SecondaryStatus { get; set; }

        [Column("IS_CODING_COMPLETED")]
        public bool? IsCodingCompleted { get; set; }

        [Column("COMMENTS")]
        public string Comments { get; set; }
    }
}
