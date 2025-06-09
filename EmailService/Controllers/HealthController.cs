using Microsoft.AspNetCore.Mvc;
using static EmailService.Common.TimeZoneHelper;

namespace EmailService.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class HealthController : Controller
	{
		[HttpGet("/health")]
		public IActionResult Index()
		{
			return Ok($"App is running {DateTimeNow}");
		}
	}
}
