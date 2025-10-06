using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using EVChargingSystem.WebAPI.Data.Models;
using System;
using System.Threading.Tasks;

namespace EVChargingSystem.WebAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options), "Email settings options are not configured.");

            _emailSettings = options.Value ?? throw new ArgumentNullException(nameof(options.Value), "EmailSettings section is missing in configuration.");
        }

        public async Task SendTemporaryPasswordAsync(string recipientEmail, string temporaryPassword)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", recipientEmail));
            message.Subject = "Welcome to EV Charging System - Your Temporary Password";

            message.Body = new BodyBuilder
            {
                HtmlBody = $@"
            <html>
              <body style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 0; margin: 0;'>
                <div style='max-width: 600px; margin: 30px auto; background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 10px rgba(0,0,0,0.1);'>
                  <div style='background-color: #00a86b; color: white; padding: 20px 30px; text-align: center;'>
                    <h2 style='margin: 0;'>EV Charging System</h2>
                    <p style='margin: 5px 0 0;'>Empowering your journey towards a sustainable future ⚡</p>
                  </div>

                  <div style='padding: 30px;'>
                    <p>Dear EV Owner,</p>
                    <p>We’re excited to let you know that your <strong>EV Charging System</strong> account has been successfully created.</p>

                    <div style='background-color: #f9f9f9; border-left: 4px solid #00a86b; padding: 15px 20px; margin: 20px 0;'>
                      <p style='margin: 0; font-size: 16px;'>Your temporary password:</p>
                      <p style='font-size: 22px; font-weight: bold; color: #00a86b; margin-top: 8px;'>{temporaryPassword}</p>
                    </div>

                    <p>Please use this password to log in for the first time. You’ll be prompted to change it for security reasons.</p>

                    <a href='https://evchargingsystem.com/login' 
                       style='display:inline-block; background-color:#00a86b; color:white; padding:10px 20px; border-radius:6px; text-decoration:none; margin-top:20px;'>
                       Go to Login
                    </a>

                    <p style='margin-top: 30px;'>Best regards,<br><strong>EV Charging System Team</strong></p>
                  </div>

                  <div style='background-color: #f0f0f0; color: #555; text-align: center; padding: 15px; font-size: 12px;'>
                    <p style='margin: 0;'>© {DateTime.Now.Year} EV Charging System. All rights reserved.</p>
                  </div>
                </div>
              </body>
            </html>"
            }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_emailSettings.Host, _emailSettings.Port, MailKit.Security.SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
