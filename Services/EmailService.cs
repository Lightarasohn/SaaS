using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMS.Interfaces;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;

namespace CMS.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message, bool isHtml = true)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            string senderName = emailSettings["SystemName"]!;
            string senderEmail = emailSettings["SystemEmail"]!;
            string appPassword = emailSettings["AppPassword"]!;
            string smtpServer = emailSettings["SmtpServer"]!;
            int smtpPort = int.Parse(emailSettings["SmtpPort"]!); 

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);

                    await client.AuthenticateAsync(senderEmail, appPassword);

                    var email = new MimeMessage();
                    email.From.Add(new MailboxAddress(senderName, senderEmail));
                    email.To.Add(MailboxAddress.Parse(toEmail));
                    email.Subject = subject;
                    email.Body = new TextPart(isHtml ? TextFormat.Html : TextFormat.Plain)
                    {
                        Text = message
                    };

                    await client.SendAsync(email);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }

         public async Task SendEmailAsync(string toEmail, string subject, string message, CancellationToken cancellationToken, bool isHtml = true)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            string senderName = emailSettings["SystemName"]!;
            string senderEmail = emailSettings["SystemEmail"]!;
            string appPassword = emailSettings["AppPassword"]!;
            string smtpServer = emailSettings["SmtpServer"]!;
            int smtpPort = int.Parse(emailSettings["SmtpPort"]!); 

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);

                    await client.AuthenticateAsync(senderEmail, appPassword);

                    var email = new MimeMessage();
                    email.From.Add(new MailboxAddress(senderName, senderEmail));
                    email.To.Add(MailboxAddress.Parse(toEmail));
                    email.Subject = subject;
                    email.Body = new TextPart(isHtml ? TextFormat.Html : TextFormat.Plain)
                    {
                        Text = message
                    };

                    await client.SendAsync(email, cancellationToken);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
        }
    }
}