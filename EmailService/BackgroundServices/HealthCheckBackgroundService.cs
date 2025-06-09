using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.Extensions.Options;

namespace EmailService.BackgroundServices
{
	public class HealthCheckBackgroundService : BackgroundService
	{
		private const string HEALTH_URL = "/health";
		private readonly HttpClient _httpClient;

		public HealthCheckBackgroundService()
		{
			_httpClient = new HttpClient();
		}

		/// <inheritdoc />
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var response = await _httpClient.GetAsync("https://mail-service-eu04.onrender.com" + HEALTH_URL, stoppingToken);
					if (response.IsSuccessStatusCode)
					{
						var content = await response.Content.ReadAsStringAsync(stoppingToken);
						Console.WriteLine(content);
					}
					else
					{
						Console.WriteLine($"Health check failed: {response.StatusCode}");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error in health check: {ex.Message}");
				}

				await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
			}
		}

		public override void Dispose()
		{
			_httpClient.Dispose();
			base.Dispose();
		}
	}
}
