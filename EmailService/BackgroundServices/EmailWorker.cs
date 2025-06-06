using System.Collections;
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
			Console.WriteLine($"Запуск");

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

					await _emailService.Send(message.Email, message.Body, message.Theme, message.IsBodyHtml, message.CopyAddress);

					Console.WriteLine($"Отправлено: {message.Email}");

					await channel.BasicAckAsync(
						deliveryTag: ea.DeliveryTag,
						multiple: false
					);
				}
				catch (JsonException ex)
				{
					Console.WriteLine($"Ошибка десериализации: {ex.Message}");
					// Если сообщение битое - нет смысла его пересылать
					await channel.BasicNackAsync(
						deliveryTag: ea.DeliveryTag,
						multiple: false,
						requeue: false // не возвращаем в очередь
					);
				}
				catch (Exception ex) when (IsTransientError(ex))
				{
					Console.WriteLine($"Временная ошибка: {ex.Message}");

					// Добавляем задержку перед повторной попыткой
					await Task.Delay(GetDelayMilliseconds(ea.Redelivered));

					await channel.BasicNackAsync(
						deliveryTag: ea.DeliveryTag,
						multiple: false,
						requeue: true // возвращаем в очередь для повторной попытки
					);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Критическая ошибка: {ex.Message}");
					// Для неисправимых ошибок не возвращаем сообщение в очередь
					await channel.BasicNackAsync(
						deliveryTag: ea.DeliveryTag,
						multiple: false,
						requeue: false
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

		// Вспомогательные методы
		private static bool IsTransientError(Exception ex)
		{
			// Определяем, является ли ошибка временной (например, таймаут сети)
			return ex is TimeoutException or IOException;
		}

		private static int GetDelayMilliseconds(bool isRedelivered)
		{
			// Экспоненциальная задержка: 5 сек при первой ошибке, 20 сек при второй и т.д.
			const int baseDelay = 5000;
			const int maxDelay = 300000; // 5 минут
			var attempts = isRedelivered ? 2 : 1;
			var delay = (int)Math.Min(baseDelay * Math.Pow(4, attempts - 1), maxDelay);
			return delay;
		}
	}
}
