using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace CassandraScheduledTask.Services
{
    internal class SendEmail
    {
        public  string SendEmailAlert(string strHost, string strFrom, string strToAll, string strCcAll, string strBccAll, string strSubject, string strMessage, string strAttachments)
        {
            try
            {
                MailMessage mailMessage = new MailMessage();
                SmtpClient smtpClient = new SmtpClient(strHost);

                mailMessage.From = new MailAddress(strFrom);
                if (!string.IsNullOrEmpty(strToAll))
                {
                    string[] strSplitToAll = strToAll.Split('|');
                    foreach (string strTo in strSplitToAll)
                    {
                        mailMessage.To.Add(strTo);
                    }
                }
                if (!string.IsNullOrEmpty(strCcAll))
                {
                    string[] strSplitCcAll = strCcAll.Split('|');
                    foreach (string strCc in strSplitCcAll)
                    {
                        mailMessage.CC.Add(strCc);
                    }
                }
                if (!string.IsNullOrEmpty(strBccAll))
                {
                    string[] strSplitBccAll = strBccAll.Split('|');
                    foreach (string strBcc in strSplitBccAll)
                    {
                        mailMessage.Bcc.Add(strBcc);
                    }
                }
                mailMessage.Subject = strSubject;
                mailMessage.Body = strMessage;
                if (!string.IsNullOrEmpty(strAttachments))
                {
                    string[] strFiles = strAttachments.Split('|');
                    foreach (string strFileName in strFiles)
                    {
                        mailMessage.Attachments.Add(new Attachment(strFileName));
                    }
                }
                mailMessage.IsBodyHtml = true;
                smtpClient.Send(mailMessage);

                return "SUCCESS";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

    }
}
