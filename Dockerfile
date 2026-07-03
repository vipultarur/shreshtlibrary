FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["WebApplication1.csproj", "./"]
RUN dotnet restore "WebApplication1.csproj"

COPY . .
WORKDIR "/src/"
RUN dotnet build "WebApplication1.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApplication1.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose port for Render
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_SYSTEM_NET_DISABLEIPV6=1

# Install fonts for QuestPDF
RUN apt-get update && apt-get install -y fontconfig fonts-liberation && rm -rf /var/lib/apt/lists/*

# L2: Run as non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "WebApplication1.dll"]
