using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMS.Interfaces
{
    public interface IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string message, bool isHtml = true);
        public Task SendEmailAsync(string toEmail, string subject, string message, CancellationToken cancellationToken, bool isHtml = true);
    }
}