namespace EmailService.ServiceLayer.Services
{
	public interface IEmailService
	{
		Task Send(string toAddress, string body, string theme, bool isBodyHtml = false,
			List<string> copyAddress = null);
	}
}
