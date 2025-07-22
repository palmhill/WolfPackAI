# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ReverseProxy/ReverseProxy.csproj ReverseProxy/
COPY OpenWebUILiteLLM.ServiceDefaults/OpenWebUILiteLLM.ServiceDefaults.csproj OpenWebUILiteLLM.ServiceDefaults/
RUN dotnet restore "ReverseProxy/ReverseProxy.csproj"
COPY ReverseProxy/. ./ReverseProxy/
COPY OpenWebUILiteLLM.ServiceDefaults/. ./OpenWebUILiteLLM.ServiceDefaults/
WORKDIR "/src/ReverseProxy"
RUN dotnet publish "ReverseProxy.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReverseProxy.dll"]
