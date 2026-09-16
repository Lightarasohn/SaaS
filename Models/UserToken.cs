using System;
using System.Collections.Generic;

namespace SaaS.Models;

public partial class UserToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public string TokenType { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool Used { get; set; }

    public DateTime CreateDate { get; set; }

    public string? Payload { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
