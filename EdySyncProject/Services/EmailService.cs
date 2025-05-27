using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace EdySyncProject.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }
        public async Task SendPasswordResetEmailAsync(string toEmail, string name, string resetLink)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("EduSync", _config["EmailSettings:From"]));
            message.To.Add(new MailboxAddress(name, toEmail));
            message.Subject = "Password Reset for EduSync";

            var htmlBody = $@"
        <p>Hello {name},</p>
        <p>You requested a password reset. Click below:</p>
        <p><a href='{resetLink}'>Reset Password</a></p>
        <p>If you did not request this, you can ignore this email.</p>";

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = $"Hello {name},\n\nYou requested a password reset. Click the link below or ignore if you didn't request this:\n{resetLink}"
            };

            message.Body = builder.ToMessageBody();

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_config["EmailSettings:SmtpServer"], int.Parse(_config["EmailSettings:Port"]), true);
                await client.AuthenticateAsync(_config["EmailSettings:Username"], _config["EmailSettings:Password"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                Console.WriteLine("Password reset email sent successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send reset email: " + ex.Message);
                throw;
            }
        }


        public async Task SendWelcomeEmailAsync(string toEmail, string name)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("EduSync", _config["EmailSettings:From"]));
            message.To.Add(new MailboxAddress(name, toEmail));
            message.Subject = "Welcome to EduSync!";

            var htmlBody = $@"
        <table width='100%' cellpadding='0' cellspacing='0' border='0' style='font-family: Arial, sans-serif; background: #f6f6f6;'>
            <tr>
                <td align='center'>
                    <table width='600' cellpadding='20' cellspacing='0' border='0' style='background: #fff; border-radius: 10px; box-shadow: 0 2px 10px #eee;'>
                        <tr>
                            <td align='center' style='padding-bottom: 0;'>
                                <img src='https://yourplatform.com/logo.png' width='120' alt='EduSync Logo' style='display: block; margin-bottom: 20px;'/>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <h2 style='color: #337ab7; margin: 0 0 16px 0;'>Welcome to EduSync, {name}!</h2>
                                <p style='margin: 0 0 20px 0;'>
                                    We're excited to have you join our learning community.<br/>
                                    Start exploring your courses today!
                                </p>
                                <p style='margin: 0 0 24px 0;'>
                                    <a href='https://yourplatform.com/login'
                                       style='background: #337ab7; color: #fff; text-decoration: none; padding: 12px 30px; border-radius: 5px; font-size: 16px; display: inline-block;'>
                                        Login Now
                                    </a>
                                </p>
                                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                                <footer>
                                    <small style='color: #999;'>&copy; 2025 EduSync | <a href='https://yourplatform.com/privacy' style='color: #337ab7;'>Privacy Policy</a></small>
                                </footer>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    ";

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = $"Hello {name},\n\nWelcome to EduSync! We're excited to have you as a part of our learning community.\n\nBest Regards,\nThe EduSync Team"
            };

            message.Body = builder.ToMessageBody();

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_config["EmailSettings:SmtpServer"], int.Parse(_config["EmailSettings:Port"]), true);
                await client.AuthenticateAsync(_config["EmailSettings:Username"], _config["EmailSettings:Password"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email: " + ex.Message);
            }
        }

    }
}
