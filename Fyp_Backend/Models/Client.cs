using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fyp_Backend.Models;

public partial class Client
{
    public int ClientId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Address { get; set; }

    public string? Picture { get; set; }
    [NotMapped]
    public IFormFile? PictureFile { get; set; }

    public virtual ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}
