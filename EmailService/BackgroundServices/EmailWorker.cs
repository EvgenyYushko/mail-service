using System.Text;
using System.Text.Json;
using EmailService.ServiceLayer.Models;
using EmailService.ServiceLayer.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EmailService.BackgroundServices
{
	public sealed class EmailWorker : BackgroundService
	{
		private IEmailService _emailService;
		private string _rabbitUrl;

		public EmailWorker(IEmailService emailService, RabbitOptions rabbitOptions)
		{
			_emailService = emailService;
			_rabbitUrl = rabbitOptions.Url;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var factory = new ConnectionFactory()
			{
				Uri = new Uri(_rabbitUrl)
			};

			var connection = await factory.CreateConnectionAsync();
			var channel = await connection.CreateChannelAsync();

			await channel.QueueDeclareAsync(
					queue: "email_queue",
					durable: true,       // Сохраняем очередь после перезагрузки
					exclusive: false,
					autoDelete: false,
			arguments: null
			);

			// 2. Настраиваем QoS
			await channel.BasicQosAsync(
				prefetchSize: 0,
				prefetchCount: 1,
				global: false
			);

			var consumer = new AsyncEventingBasicConsumer(channel);
			consumer.ReceivedAsync += async (model, ea) =>
			{
				try
				{
					var json = Encoding.UTF8.GetString(ea.Body.Span);
					var message = JsonSerializer.Deserialize<MailMessageDto>(json);

					//var body = HtmlHelper.GetWeeklyReportHtml("много", 2, 3, 4, 5, "cahts", 6, 7, 8, 9, "aaa", "dsds");
					await _emailService.Send(message.Email, message.Body, message.Theme, message.IsBodyHtml, message.CopyAddress);

					Console.WriteLine($"Отправлено: {message.Email}");

					await channel.BasicAckAsync(
						deliveryTag: ea.DeliveryTag,
						multiple: false
					);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Ошибка: {ex.Message}");
					await channel.BasicNackAsync(
						deliveryTag: ea.DeliveryTag,
						multiple: false,
						requeue: true
					);
				}
			};

			// 3. Запускаем Consumer
			await channel.BasicConsumeAsync(
				queue: "email_queue",
				autoAck: false,
				consumer: consumer
			);

			Console.WriteLine("Worker запущен. Ожидание писем...");
			await Task.Delay(Timeout.Infinite); // Бесконечное ожидание
		}
	}
}
