using System.ComponentModel.DataAnnotations;

namespace PIMS3.ViewModels

{
    public class DataImportVm
    {
        [Required]
        public string ImportFilePath { get; set; }
        [Required]
        public bool IsRevenueData { get; set; }
        public int? RecordsSaved { get; set; }
        public decimal? AmountSaved { get; set; }
    }

}
