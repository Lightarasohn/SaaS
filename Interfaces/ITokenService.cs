using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SaaS.Models;

namespace SaaS.Interfaces
{
    public interface ITokenService
    {
        string CreateAccessToken(AppUser user);
        (string RawToken, string TokenHash) CreateRefreshToken();
    }
}