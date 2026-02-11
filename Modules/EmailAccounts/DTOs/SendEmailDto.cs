namespace MyApi.Modules.EmailAccounts.DTOs
{
    public class SendEmailDto
    {
        public List<string> To { get; set; } = new();
        public List<string> Cc { get; set; } = new();
        public List<string> Bcc { get; set; } = new();
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
    }

    public class SendEmailResultDto
    {
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }
    }
}
