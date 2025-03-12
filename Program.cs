using System;
using Microsoft.Extensions.Configuration;
using LdapForNet;
using static LdapForNet.Native.Native;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Загружаем конфигурацию из appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            // Получаем параметры подключения из конфигурации
            var server = configuration["ActiveDirectory:Server"];
            var port = int.Parse(configuration["ActiveDirectory:Port"] ?? "389");
            var username = configuration["ActiveDirectory:Username"];
            var password = configuration["ActiveDirectory:Password"];

            TestConnection(server!, port, username!, password!);
        }
        catch (Exception ex)
        {
            WriteError($"Общая ошибка: {ex.Message}");
        }

        Console.WriteLine("\nНажмите любую клавишу для выхода...");
        Console.ReadKey();
    }

    static void TestConnection(string server, int port, string username, string password)
    {
        try
        {
            WriteInfo("Попытка подключения к серверу...");

            using (var connection = new LdapConnection())
            {
                connection.Connect(server, port);
                WriteInfo("Выполняем аутентификацию...");
                connection.Bind(LdapAuthMechanism.SIMPLE, username, password);

                WriteSuccess("\nУспешное подключение к Active Directory!");
                WriteSuccess($"Сервер: {server}");
                WriteSuccess($"Порт: {port}");

                var response = connection.Search(
                    "",
                    "(objectClass=*)",
                    new string[] { "namingContexts" }
                );

                if (response.Any())
                {
                    WriteSuccess($"Корневой каталог: {response.First().Dn}");
                }
            }
        }
        catch (LdapException ex)
        {
            WriteError($"Ошибка LDAP: {ex.Message}");
        }
        catch (Exception ex)
        {
            WriteError($"Ошибка: {ex.Message}");
        }
    }

    // Вспомогательные методы для вывода в консоль
    static void WriteSuccess(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    static void WriteError(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    static void WriteInfo(string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }
}
