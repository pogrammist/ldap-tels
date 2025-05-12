using ad_tels.Services;

namespace ad_tels.Services;

public class LdapSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LdapSyncBackgroundService> _logger;
    private readonly TimeSpan _syncInterval;
    private readonly bool _enabled;

    public LdapSyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<LdapSyncBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Получаем интервал синхронизации из конфигурации или используем значение по умолчанию (1 час)
        var intervalMinutes = configuration.GetValue<int>("LdapSync:IntervalMinutes", 60);
        _syncInterval = TimeSpan.FromMinutes(intervalMinutes);
        
        // Проверяем, включена ли синхронизация
        _enabled = configuration.GetValue<bool>("LdapSync:Enabled", true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Служба синхронизации LDAP отключена в настройках");
            return;
        }
        
        _logger.LogInformation("Служба синхронизации LDAP запущена с интервалом {Interval} минут", 
            _syncInterval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncLdapSourcesAsync();
                
                _logger.LogInformation("Следующая синхронизация LDAP запланирована через {Interval} минут", 
                    _syncInterval.TotalMinutes);
                
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение при остановке службы
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении фоновой синхронизации LDAP");
                
                // Ждем некоторое время перед повторной попыткой
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Служба синхронизации LDAP остановлена");
    }

    private async Task SyncLdapSourcesAsync()
    {
        _logger.LogInformation("Начало фоновой синхронизации LDAP");
        
        // Создаем новый scope для получения сервисов
        using var scope = _serviceProvider.CreateScope();
        var ldapService = scope.ServiceProvider.GetRequiredService<LdapService>();
        
        await ldapService.SyncAllSourcesAsync();
        
        _logger.LogInformation("Фоновая синхронизация LDAP завершена");
    }
}
