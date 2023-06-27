#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Service/SystemMonitor.MonitoringService/SystemMonitor.MonitoringService.csproj", "Service/SystemMonitor.MonitoringService/"]
RUN dotnet restore "Service/SystemMonitor.MonitoringService/SystemMonitor.MonitoringService.csproj"
COPY . .
WORKDIR "/src/Service/SystemMonitor.MonitoringService"
RUN dotnet build "SystemMonitor.MonitoringService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SystemMonitor.MonitoringService.csproj" -c Release -o /app/publish /p:UseAppHost=false

EXPOSE 1234/udp

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SystemMonitor.MonitoringService.dll"]