using System.ComponentModel.DataAnnotations;

namespace PIMS3.Data.Entities
{
    // Receptacle for overdue revenue, utilized via "Income due".

    public class DelinquentIncome
    {
        [Required]
        [Key]
        public string PositionId { get; set; }

        [Required]
        [Key]
        public string MonthDue { get; set; }

        [Required]
        public string InvestorId { get; set; }

        [Required]
        public string TickerSymbol { get; set; }

        [Required]
        public string AccountTypeDesc { get; set; }


        [Required]
        public string DividendFreq { get; set; }


    }
}
