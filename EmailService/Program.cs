using EmailService.BackgroundServices;
using EmailService.ServiceLayer.Models;
using EmailService.ServiceLayer.Services;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.Configure<MailOptions>(builder.Configuration.GetSection(nameof(MailOptions)));
var mailOption = builder.Configuration.GetSection(nameof(MailOptions)).Get<MailOptions>();
mailOption.SenderPassword = builder.Configuration.GetValue<string>("SENDER_PASSWORD") ?? Environment.GetEnvironmentVariable("SENDER_PASSWORD");
builder.Services.AddSingleton(mailOption);

builder.Services.AddSingleton(new RabbitOptions
{
	Url = builder.Configuration.GetValue<string>("RABBIT_URL") ?? Environment.GetEnvironmentVariable("RABBIT_URL")
});

builder.Services.AddSingleton<IEmailService, EmailService.BuisinessLogic.EmailService>();
builder.Services.AddHostedService<EmailWorker>();
builder.Services.AddHostedService<HealthCheckBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
