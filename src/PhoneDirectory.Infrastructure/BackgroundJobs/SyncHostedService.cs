using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using PhoneDirectory.Application.Interfaces;

namespace PhoneDirectory.Infrastructure.BackgroundJobs;

public class SyncHostedService : IHostedService, IDisposable
{
	private readonly ILogger<SyncHostedService> _logger;
	private readonly IServiceProvider _services;
	private Timer? _timer;

	public SyncHostedService(ILogger<SyncHostedService> logger, IServiceProvider services)
	{
		_logger = logger;
		_services = services;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_timer = new Timer(DoSync, null, TimeSpan.Zero, TimeSpan.FromHours(1));
		return Task.CompletedTask;
	}

	private void DoSync(object? state)
	{
		using var scope = _services.CreateScope();
		var contactService = scope.ServiceProvider.GetRequiredService<IContactService>();
		// Здесь будет логика синхронизации
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_timer?.Change(Timeout.Infinite, 0);
		return Task.CompletedTask;
	}

	public void Dispose()
	{
		_timer?.Dispose();
	}
}