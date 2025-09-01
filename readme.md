# Ldap Tels - Телефонный справочник на основе данных из LDAP

Веб-приложение для создания и управления телефонным справочником на основе данных из LDAP-серверов (включая Active Directory). Приложение позволяет добавлять и удалять источники данных LDAP, синхронизировать контакты и осуществлять поиск по различным критериям.

## Технологии

- .NET 8.0
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- System.DirectoryServices.Protocols для работы с LDAP

## Структура проекта

```
ldap-tels/
├── src/
│   └── ldap_tels/                 # Основной проект
│       ├── ldap_tels.csproj
│       ├── Program.cs
│       ├── Controllers/            # Контроллеры MVC
│       ├── Services/               # Бизнес-логика
│       ├── Models/                 # Модели данных
│       ├── ViewModels/             # Модели представлений
│       ├── Views/                  # Razor представления
│       ├── wwwroot/                # Статические файлы
│       ├── Data/                   # Контекст базы данных
│       ├── Migrations/             # Миграции EF Core
│       ├── Areas/                  # Области приложения
│       ├── Extensions/             # Расширения
│       ├── Configuration/          # Конфигурация
│       └── Properties/             # Настройки проекта
└── tests/
    └── ldap_tels.Tests/            # Тесты
        ├── ldap_tels.Tests.csproj
        ├── ContactServiceTests.cs   # Тесты ContactService
        ├── InfiniteScrollTests.cs   # Тесты пагинации
        └── LdapServiceTests.cs     # Тесты LdapService
```

## Функциональность

- Управление LDAP-источниками (добавление, редактирование, удаление)
- Автоматическая синхронизация контактов с LDAP-серверами
- Поиск контактов по различным критериям (имя, фамилия, телефон, отдел и т.д.)
- Фильтрация контактов по отделам, подразделениям и должностям
- Группировка контактов с весовой сортировкой
- Бесконечная прокрутка с пагинацией
- REST API для интеграции с другими системами

## Запуск приложения

### 1. Клонирование и настройка

```bash
git clone <repository-url>
cd ldap-tels
dotnet restore
```

### 2. Настройка базы данных

Настроить строку подключения к базе данных в `src/ldap_tels/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ldap_tels;Trusted_Connection=true;"
  }
}
```

### 3. Запуск разных окружений

#### Development (разработка)
```bash
dotnet run --project src/ldap_tels --environment Development
# или
dotnet run --project src/ldap_tels --launch-profile "Development"
```

#### Staging (тестирование)
```bash
dotnet run --project src/ldap_tels --environment Staging
# или
dotnet run --project src/ldap_tels --launch-profile "Staging"
```

#### Production (продакшн)
```bash
dotnet run --project src/ldap_tels --environment Production
# или
dotnet run --project src/ldap_tels --launch-profile "Production"
```

### 4. Использование скриптов запуска

В папке `src/ldap_tels/` доступны скрипты для разных платформ:

#### Windows
```bash
# Development
launch-dev.bat

# Staging  
launch-stage.bat

# Production
launch-prod.bat
```

#### Linux/macOS
```bash
# Development
./launch-dev.sh

# Staging
./launch-stage.sh

# Production
./launch-prod.sh
```

### 5. Проверка работы

Открыть браузер и перейти по адресу:
- **Development:** https://localhost:7155
- **Staging:** https://localhost:7156  
- **Production:** https://localhost:7157

## Запуск тестов

### 1. Сборка тестов
```bash
dotnet build tests/ldap_tels.Tests/ldap_tels.Tests.csproj
```

### 2. Запуск всех тестов
```bash
dotnet test tests/ldap_tels.Tests/ldap_tels.Tests.csproj
```

### 3. Запуск конкретного теста
```bash
dotnet test tests/ldap_tels.Tests/ldap_tels.Tests.csproj --filter "FullyQualifiedName~ContactServiceTests"
```

### 4. Запуск тестов с подробным выводом
```bash
dotnet test tests/ldap_tels.Tests/ldap_tels.Tests.csproj --verbosity normal
```

### 5. Запуск тестов из корня решения
```bash
dotnet test
```

## Сборка проекта

### 1. Сборка основного проекта
```bash
dotnet build src/ldap_tels/ldap_tels.csproj
```

### 2. Сборка тестов
```bash
dotnet build tests/ldap_tels.Tests/ldap_tels.Tests.csproj
```

### 3. Сборка всего решения
```bash
dotnet build
```

### 4. Очистка сборки
```bash
dotnet clean
```

## API Endpoints

### LDAP-источники

- `GET /api/ldapsource` - получить список всех LDAP-источников
- `GET /api/ldapsource/{id}` - получить LDAP-источник по ID
- `POST /api/ldapsource` - добавить новый LDAP-источник
- `PUT /api/ldapsource/{id}` - обновить LDAP-источник
- `DELETE /api/ldapsource/{id}` - удалить LDAP-источник
- `POST /api/ldapsource/{id}/sync` - синхронизировать контакты с LDAP-источником
- `POST /api/ldapsource/sync-all` - синхронизировать контакты со всеми LDAP-источниками

### Контакты

- `GET /api/contact` - получить список контактов (с пагинацией)
- `GET /api/contact/{id}` - получить контакт по ID
- `GET /api/contact/search?query={searchTerm}` - поиск контактов
- `GET /api/contact/departments` - получить список всех отделов
- `GET /api/contact/department/{department}` - получить контакты по отделу
- `GET /api/contact/division/{division}` - получить контакты по подразделению
- `GET /api/contact/title/{title}` - получить контакты по должности

## Настройка LDAP-источника

Для добавления LDAP-источника необходимо указать следующие параметры:

- `Name` - название источника
- `Server` - адрес LDAP-сервера
- `Port` - порт (по умолчанию 389)
- `BaseDn` - базовый DN для поиска
- `BindDn` - DN для аутентификации (опционально)
- `BindPassword` - пароль для аутентификации (опционально)
- `SearchFilter` - фильтр поиска (по умолчанию `(& (objectCategory=person) (objectClass=user) (!(userAccountControl:1.2.840.113556.1.4.803:=2)) (telephoneNumber=*))`)
- `UseSSL` - использовать SSL (по умолчанию `false`)

## Настройка автоматической синхронизации

В файле `src/ldap_tels/appsettings.json` можно настроить параметры автоматической синхронизации:

```json
"LdapSync": {
  "IntervalMinutes": 60,
  "Enabled": true
}
```

- `IntervalMinutes` - интервал синхронизации в минутах
- `Enabled` - включить/отключить автоматическую синхронизацию

## Конфигурация окружений

### Development (разработка)
Файл: `src/ldap_tels/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ldap_tels_dev;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "LdapSync": {
    "IntervalMinutes": 5,
    "Enabled": true
  },
  "LdapSettings": {
    "Server": "localhost",
    "Port": 389,
    "UseSsl": false,
    "Domain": "test.com",
    "SearchBase": "dc=test,dc=com",
    "BindUsername": "admin",
    "BindPassword": "admin"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### Staging (тестирование)
Файл: `src/ldap_tels/appsettings.Staging.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=staging-server\\SqlExpress;Database=ldap_tels_stage;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
  },
  "LdapSync": {
    "IntervalMinutes": 30,
    "Enabled": true
  },
  "LdapSettings": {
    "Server": "staging-dc.company.com",
    "Port": 389,
    "UseSsl": false,
    "Domain": "company.com",
    "SearchBase": "dc=company,dc=com",
    "BindUsername": "service_account",
    "BindPassword": "your_secure_password_here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "System": "Information"
      }
    }
  }
}
```

### Production (продакшн)
Файл: `src/ldap_tels/appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server\\SqlExpress;Database=ldap_tels_prod;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False"
  },
  "LdapSync": {
    "IntervalMinutes": 60,
    "Enabled": true
  },
  "LdapSettings": {
    "Server": "prod-dc.company.com",
    "Port": 389,
    "UseSsl": false,
    "Domain": "company.com",
    "SearchBase": "dc=company,dc=com",
    "BindUsername": "prod_service_account",
    "BindPassword": "your_production_password_here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Важные замечания по безопасности:

1. **Никогда не коммитьте реальные пароли** в репозиторий
2. **Используйте переменные окружения** для секретных данных в продакшне
3. **Настройте .gitignore** для исключения файлов с секретами
4. **Используйте User Secrets** для локальной разработки
5. **Шифруйте строки подключения** в продакшн-средах

### Настройка User Secrets (для разработки):

```bash
dotnet user-secrets init --project src/ldap_tels
dotnet user-secrets set "LdapSettings:BindPassword" "your_password" --project src/ldap_tels
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your_connection_string" --project src/ldap_tels
```

### Переменные окружения для продакшна:

```bash
# Windows
setx LdapSettings__BindPassword "your_production_password"
setx ConnectionStrings__DefaultConnection "your_production_connection"

# Linux/macOS
export LdapSettings__BindPassword="your_production_password"
export ConnectionStrings__DefaultConnection="your_production_connection"
```

## Логирование

Логи приложения сохраняются в папке `logs/` в корне проекта. Для настройки логирования используйте `appsettings.{Environment}.json`.

## Развертывание

### IIS
1. Опубликовать проект: `dotnet publish src/ldap_tels/ldap_tels.csproj -c Release`
2. Скопировать содержимое папки `publish/` в папку сайта IIS
3. Настроить пул приложений для .NET Core
4. Настроить привязки HTTPS

### Docker (планируется)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY src/ldap_tels/bin/Release/net8.0/publish/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "ldap_tels.dll"]
```

## Разработка

### Добавление новых тестов
1. Создать новый файл в `tests/ldap_tels.Tests/`
2. Унаследовать от базового класса тестов
3. Использовать in-memory базу данных для изоляции тестов

### Структура тестов
- **ContactServiceTests** - тестирование логики группировки и сортировки контактов
- **InfiniteScrollTests** - тестирование пагинации и загрузки данных
- **LdapServiceTests** - тестирование LDAP-синхронизации

## Поддержка

При возникновении проблем:
1. Проверить логи в папке `logs/`
2. Убедиться в корректности настроек подключения к базе данных
3. Проверить доступность LDAP-серверов
4. Запустить тесты для проверки работоспособности компонентов
