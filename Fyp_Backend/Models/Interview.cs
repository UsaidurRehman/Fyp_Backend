using System;
using System.Collections.Generic;

namespace Fyp_Backend.Models;

public partial class Interview
{
    public int InterviewId { get; set; }

    public int? ClientId { get; set; }

    public int? WorkerId { get; set; }

    public DateTime? InterviewDate { get; set; }

    public string? Address { get; set; }

    public string? Status { get; set; }

    public string? HiringDecision { get; set; }

    public string? WorkerDecision { get; set; }

    public virtual Client? Client { get; set; }

    public virtual ICollection<Resignation> Resignations { get; set; } = new List<Resignation>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Termination> Terminations { get; set; } = new List<Termination>();

    public virtual Worker? Worker { get; set; }
}
