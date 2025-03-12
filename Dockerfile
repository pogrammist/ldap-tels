FROM mcr.microsoft.com/dotnet/sdk:9.0
WORKDIR /app

# Создаем новый проект
RUN dotnet new web

# Копируем исходный код
COPY ["Program.cs", "./"]

# Добавляем зависимости для работы с LDAP
RUN dotnet add package Novell.Directory.Ldap
RUN dotnet add package LdapForNet

# Восстанавливаем зависимости и собираем проект
RUN dotnet restore
RUN dotnet build -c Release

# Настраиваем переменные среды
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Открываем порты
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "run", "--no-launch-profile"]
