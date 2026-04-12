using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fyp_Backend.Models;

public partial class Worker
{
    public int WorkerId { get; set; }

    public string Name { get; set; } = null!;

    public string Cnic { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public decimal? Salary { get; set; }

    public string? Address { get; set; }

    public string Picture { get; set; } = "worker_default.jpg";

    [NotMapped]
    public IFormFile? PictureFile { get; set; }

    public bool? AvailableStatus { get; set; }

    public int? CategoryId { get; set; }

    public int? Age { get; set; }

    public string Password { get; set; } = null!;

    public string? Gender { get; set; }

    public string? Bio { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<Experience> Experiences { get; set; } = new List<Experience>();

    public virtual ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}
