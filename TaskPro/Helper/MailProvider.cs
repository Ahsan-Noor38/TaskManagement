using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace TaskPro.Helper
{
    public class MailProvider
    {
        private readonly IConfiguration _config;

        public MailProvider(IConfiguration config)
        {
            _config = config;
        }

        public bool SenttoMail(string receiver, string password, string subject, string body)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                var email = _config["Email"]; // Fixed: Accessing _config as an instance field
                message.From = new MailAddress(receiver);
                message.To.Add(new MailAddress(email));
                message.Subject = subject;
                message.IsBodyHtml = true; //to make message body as html  
                message.Body = body;
                smtp.Port = 587;
                smtp.Host = "smtp.gmail.com"; //for gmail host  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(receiver, password);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool SendEmail(string receiver, string subject, string body)
        {
            try
            {
                var email = _config["Email"];
                var password = _config["Password"];

                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress(email);
                message.To.Add(new MailAddress(receiver));
                message.Subject = subject;
                message.IsBodyHtml = true; //to make message body as html  
                message.Body = body;
                smtp.Port = 587;
                smtp.Host = "smtp.gmail.com"; //for gmail host  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(email, password);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}