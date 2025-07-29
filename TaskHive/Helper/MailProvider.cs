using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace TaskHive.Helper
{
    public class MailProvider
    {
        // ndobqpvzswepjttr
        public static bool SenttoMail(string receiver, string password, string subject, string body)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress(receiver);
                message.To.Add(new MailAddress(ConfigurationManager.AppSettings["email"]));
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
        public static bool SentfromMail(string receiver, string subject, string body)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress(ConfigurationManager.AppSettings["email"]);
                message.To.Add(new MailAddress(receiver));
                message.Subject = subject;
                message.IsBodyHtml = true; //to make message body as html  
                message.Body = body;
                smtp.Port = 587;
                smtp.Host = "smtp.gmail.com"; //for gmail host  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["email"], ConfigurationManager.AppSettings["password"]);
                // smtp.Credentials = new NetworkCredential("readrix28@gmail.com", "ejinwqithimbfxxs");

                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool Sentforgotpassmail(string receiveemail, int code)
        {
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress(ConfigurationManager.AppSettings["email"]);
                message.To.Add(new MailAddress(receiveemail));
                message.Subject = "Forgot Password Code";
                message.IsBodyHtml = true; //to make message body as html  
                message.Body = "your code for forgot password is " + code;
                smtp.Port = 587;
                smtp.Host = "smtp.gmail.com"; //for gmail host  
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["email"], ConfigurationManager.AppSettings["password"]);
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