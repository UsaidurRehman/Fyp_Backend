using System;
using System.Collections.Generic;

namespace Fyp_Backend.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();

    public virtual ICollection<Worker> Workers { get; set; } = new List<Worker>();
}
