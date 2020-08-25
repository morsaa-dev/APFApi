using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;

namespace UsersManager.Services
{
    public class EmailService : IIdentityMessageService
    {
        public async Task SendAsync(IdentityMessage message)
        {
            await configSendGridasync(message);
        }

        // Use NuGet to install SendGrid (Basic C# client lib) 
        private async Task configSendGridasync(IdentityMessage message)
        {
            int iPuerto = Convert.ToInt32(ConfigurationManager.AppSettings["emailService:Port"]);
            string sSmtp = ConfigurationManager.AppSettings["emailService:Smtp"];

            MailMessage myMessage = new MailMessage();
            //var myMessage = new SendGridMessage();
            SmtpClient SmtpServer = new SmtpClient(sSmtp);

            myMessage.To.Add(message.Destination);
            myMessage.From = new MailAddress(ConfigurationManager.AppSettings["emailService:Account"], ConfigurationManager.AppSettings["emailService:Name"]);
            myMessage.Subject = message.Subject;
            //myMessage.Body = message.Body;
            myMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message.Body, null, MediaTypeNames.Text.Html));


            SmtpServer.Port = iPuerto;
            SmtpServer.UseDefaultCredentials = false;
            SmtpServer.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["emailService:Account"],
                                                           ConfigurationManager.AppSettings["emailService:Password"]);
            SmtpServer.EnableSsl = true;

            SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
            ServicePointManager.ServerCertificateValidationCallback =
                           delegate (object s
                               , X509Certificate certificate
                               , X509Chain chain
                               , SslPolicyErrors sslPolicyErrors)
                           { return true; };

            // Send the email.
            if (ServicePointManager.ServerCertificateValidationCallback != null)
            {
                await SmtpServer.SendMailAsync(myMessage);
            }
            else
            {
                //Trace.TraceError("Failed to create Web transport.");
                await Task.FromResult(0);
            }
        }
    }
}