using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Interfaces
{
    public interface ICurrentUser
    {
        Guid? CompanyId { get; }
        Guid? UserId { get; }
        string? Role { get; }
    }
}