using System;
using System.Collections.Generic;

namespace SaaS.Models;

public partial class AppUser
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public int CompanyId { get; set; }

    public int RoleId { get; set; }

    public int? DistributorId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? RecoveryKeyHash { get; set; }

    public bool IsVerified { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? PasswordChangedAt { get; set; }

    public int? CreateUser { get; set; }

    public DateTime CreateDate { get; set; }

    public int? UpdateUser { get; set; }

    public DateTime? UpdateDate { get; set; }

    public int? DeleteUser { get; set; }

    public DateTime? DeleteDate { get; set; }

    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual AppRole Role { get; set; } = null!;

    public virtual ICollection<UserToken> UserTokens { get; set; } = new List<UserToken>();
}
