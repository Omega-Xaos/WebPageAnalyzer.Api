FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src
EXPOSE 8080

CMD ["sh", "-c", "dotnet restore ./WebPageAnalyzer.Api.csproj && dotnet build ./WebPageAnalyzer.Api.csproj --no-restore && dotnet run --project ./WebPageAnalyzer.Api.csproj --no-build --no-launch-profile"]
