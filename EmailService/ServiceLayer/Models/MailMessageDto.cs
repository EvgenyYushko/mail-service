namespace EmailService.ServiceLayer.Models
{
	public class MailMessageDto
	{
		public string Email { get; set; }
		public string Body { get; set; }
		public string Theme { get; set; }
		public bool IsBodyHtml { get; set; }
		public List<string> CopyAddress { get; set; } = null;
	}
}
