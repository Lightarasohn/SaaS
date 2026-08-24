using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.Utils
{
    public sealed record TokenPair(string AccessToken, string RefreshToken, DateTime RefreshExpiresAt);
}