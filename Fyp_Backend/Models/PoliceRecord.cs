using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fyp_Backend.Models
{
    public class PoliceRecord
    {
        [Key]
        public int RecordID { get; set; }

        [Required]
        public int WorkerID { get; set; }

        [Required]
        public int? PoliceID { get; set; }

        [Required]
        [StringLength(50)]
        public string FIRNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string OffenseCategory { get; set; }

        [Required]
        public DateTime OffenseDate { get; set; }

        public bool IsFlagged { get; set; } = true;

        public bool IsBlocked { get; set; } = false;

        public string CaseDetails { get; set; }

        public DateTime FiledDate { get; set; } = DateTime.Now;

        [ForeignKey("PoliceID")]
        public virtual PoliceOfficer PoliceOfficer { get; set; }

        [ForeignKey("WorkerID")]
        public virtual Worker Worker { get; set; }
    }
}