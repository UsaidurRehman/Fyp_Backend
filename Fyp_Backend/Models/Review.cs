using System;
using System.Collections.Generic;

namespace Fyp_Backend.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int? InterviewId { get; set; }

    /// <summary>
    /// Overall score, 1.00-5.00.
    /// For Client -> Worker reviews this is the AVERAGE of the common-criterion
    /// scores in ReviewCriteriaRatings. ReviewSkillRatings are independent and
    /// never contribute to this value. Worker -> Client reviews still write a
    /// whole number here.
    /// </summary>
    public decimal? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? ReviewDate { get; set; }

    public string? ReviewerRole { get; set; }

    public virtual Interview? Interview { get; set; }

    public virtual ICollection<ReviewCriteriaRating> ReviewCriteriaRatings { get; set; } = new List<ReviewCriteriaRating>();

    /// <summary>Client-to-worker scores for the skills selected on this interview.</summary>
    public virtual ICollection<ReviewSkillRating> ReviewSkillRatings { get; set; } = new List<ReviewSkillRating>();
}
