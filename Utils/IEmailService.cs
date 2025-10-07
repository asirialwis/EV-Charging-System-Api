public interface IEmailService 
{
    Task SendTemporaryPasswordAsync(string recipientEmail, string temporaryPassword);
}