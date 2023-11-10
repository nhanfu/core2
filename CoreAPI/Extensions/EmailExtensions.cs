using Core.ViewModels;
using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Extensions
{
    public static class EmailExtensions
    {
        public static async Task<bool> SendMailAsync(this EmailVM email, string fromName, string fromAddress, string password, string server, int port, bool ssl, string webRoot)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName ?? "No-reply", fromAddress));
            email.ToAddresses.ForEach(address => message.To.Add(new MailboxAddress(address, address)));
            message.Subject = email.Subject;
            var textPart = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = email.Body,
            };
            if (email.Attachements.HasElement())
            {
                email.Attachements.Select(x => Path.Combine(webRoot, x)).SelectForeach(email.ServerAttachements.Add);
            }
            var attachments = email.ServerAttachements.Select(x => new MimePart(MimeTypes.GetMimeType(x))
            {
                Content = new MimeContent(File.OpenRead(x)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = Path.GetFileName(x)
            });
            var body = new Multipart("mixed")
            {
                textPart
            };
            attachments.SelectForeach(attached=> body.Add(attached));
            message.Body = body;
            try
            {
                using var smtpClient = new SmtpClient();
                smtpClient.Connect(server, port, ssl ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
                smtpClient.Authenticate(fromAddress, password);
                await smtpClient.SendAsync(message);
                smtpClient.Disconnect(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
    }
}
