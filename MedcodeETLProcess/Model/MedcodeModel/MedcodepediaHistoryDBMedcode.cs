using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MedcodeETLProcess.Model.MedcodeModel
{
    [Table("MHMEDCODE", Schema = "dbo.MHMEDCODE")]
    public class MedcodepediaHistoryDBMedcode
    {
        [Key]
        [Column("MHMEDCODE_ID")]
        public int MhMedCodeId { get; set; }

        [Column("UMR_HISTORY_USER_MEDCODES_ID")]
        public int? UmrHistoryUserMedcodesId { get; set; }

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

        [Column("PLACEMENT_HOLDER")]
        public string PlacementHolder { get; set; }

        [Column("TRIGGER")]
        public string Trigger { get; set; }

        [Column("MISCELLANEOUS")]
        public string Miscellaneous { get; set; }

    }
}