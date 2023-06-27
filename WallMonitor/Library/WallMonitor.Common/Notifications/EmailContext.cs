namespace WallMonitor.Common.Notifications
{
    public class EmailContext
    {
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsEmailConfirmed { get; set; }
        public string? TemplateName { get; set; }
        public IDictionary<string, string> TemplateData { get; set; } = new Dictionary<string, string>();
        public string? Body { get; set; }
        public string? Subject { get; set; }

        public EmailContext() { }

        public EmailContext(string email, string subject, string body)
        {
            if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
            if (string.IsNullOrEmpty(body)) throw new ArgumentNullException(nameof(body));
            Email = email;
            Subject = subject;
            Body = body;
        }

        public EmailContext(string email, string name, string subject, string body)
        {
            if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
            if (string.IsNullOrEmpty(body)) throw new ArgumentNullException(nameof(body));
            Email = email;
            Name = name;
            Subject = subject;
            Body = body;
        }

        public EmailContext(string email, string subject, string templateName, IDictionary<string, string> templateData)
        {
            if (string.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (string.IsNullOrEmpty(subject)) throw new ArgumentNullException(nameof(subject));
            if (string.IsNullOrEmpty(templateName)) throw new ArgumentNullException(nameof(templateName));
            if (templateData == null || !templateData.Any()) throw new ArgumentNullException(nameof(templateData));
            Email = email;
            Subject = subject;
            TemplateName = templateName;
            TemplateData = templateData;
        }

    }
}
