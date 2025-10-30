using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedcodeETLProcess.Model.MedcodeModel
{
    [Table("UMR_MEDCODES", Schema ="dbo.UMR_MEDCODES")]
    public class MedicalCode
    {
        [Key]
        [Column("GUID")]
        public Guid Guid { get; set; }

        [Column("CODE_TYPE")]
        public string CodeType { get; set; }

        [Column("MEDCODE")]
        public string MedCode { get; set; }

        [Column("SEX_TYPE")]
        public string SexType { get; set; }

        [Column("CODE_VERSION")]
        public string CodeVersion { get; set; }

        [Column("DETAIL")]
        public string Detail { get; set; }

        [Column("MEDICAL_CONTENT_INDEX")]
        public string MedicalContentIndex { get; set; }

        [Column("MEDCODE_VALUE")]
        public string MedCodeValue { get; set; }

        [Column("MEDCODE_STATUS")]
        public string MedCodeStatus { get; set; }

        [Column("OASIS_DEMOGRAPHICS")]
        public string OasisDemographics { get; set; }

        [Column("SECONDARY_STATUS")]
        public string SecondaryStatus { get; set; }

        [Column("IS_CODING_COMPLETED")]
        public bool? IsCodingCompleted { get; set; }

        [Column("FUTURE_VERSION")]
        public string FutureVersion { get; set; }

        [Column("OWNER_ID")]
        public string OwnerId { get; set; }

        [Column("COMMENT")]
        public string Comment { get; set; }

        [Column("IS_RECYCLED_EVER")]
        public bool? IsRecycledEver { get; set; }

        [Column("PLACEMENT_HOLDER")]
        public string PlacementHolder { get; set; }

        [Column("TRIGGER")]
        public string Trigger { get; set; }

        [Column("MISCELLANEOUS")]
        public string Miscellaneous { get; set; }

        [Column("NO_METRIC")]
        public bool? NoMetric { get; set; }
    }
}
