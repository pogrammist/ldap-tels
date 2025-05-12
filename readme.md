# AD Tels - Телефонный справочник на основе данных из LDAP

Веб-приложение для создания и управления телефонным справочником на основе данных из LDAP-серверов (включая Active Directory). Приложение позволяет добавлять и удалять источники данных LDAP, синхронизировать контакты и осуществлять поиск по различным критериям.

## Технологии

- .NET 9.0
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- System.DirectoryServices.Protocols для работы с LDAP

## Функциональность

- Управление LDAP-источниками (добавление, редактирование, удаление)
- Автоматическая синхронизация контактов с LDAP-серверами
- Поиск контактов по различным критериям (имя, фамилия, телефон, отдел и т.д.)
- Фильтрация контактов по отделам
- REST API для интеграции с другими системами

## Структура проекта

- **Models** - модели данных
  - `LdapSource.cs` - модель для хранения настроек подключения к LDAP-серверу
  - `Contact.cs` - модель для хранения контактной информации

- **Data** - работа с базой данных
  - `ApplicationDbContext.cs` - контекст базы данных для Entity Framework Core

- **Services** - бизнес-логика
  - `LdapService.cs` - сервис для работы с LDAP-серверами
  - `ContactService.cs` - сервис для работы с контактами
  - `LdapSyncBackgroundService.cs` - фоновый сервис для автоматической синхронизации

- **Controllers** - API-контроллеры
  - `LdapSourceController.cs` - управление LDAP-источниками
  - `ContactController.cs` - поиск и просмотр контактов
  - `HomeController.cs` - информация об API и проверка работоспособности

## Запуск приложения

1. Клонировать репозиторий
2. Настроить строку подключения к базе данных в `appsettings.json`
3. Запустить приложение:

```bash
dotnet run --project ad_tels --urls http://localhost:5001/
```

4. Проверить работу API:

```bash
curl http://localhost:5001/api
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

## Настройка LDAP-источника

Для добавления LDAP-источника необходимо указать следующие параметры:

- `Name` - название источника
- `Server` - адрес LDAP-сервера
- `Port` - порт (по умолчанию 389)
- `BaseDn` - базовый DN для поиска
- `BindDn` - DN для аутентификации (опционально)
- `BindPassword` - пароль для аутентификации (опционально)
- `SearchFilter` - фильтр поиска (по умолчанию `(&(objectClass=person)(|(sn=*)(cn=*)))`)
- `UseSSL` - использовать SSL (по умолчанию `false`)

## Настройка автоматической синхронизации

В файле `appsettings.json` можно настроить параметры автоматической синхронизации:

```json
"LdapSync": {
  "IntervalMinutes": 60,
  "Enabled": true
}
```

- `IntervalMinutes` - интервал синхронизации в минутах
- `Enabled` - включить/отключить автоматическую синхронизацию
