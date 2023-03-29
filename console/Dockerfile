#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["console/csharprover.csproj", "console/"]
COPY ["roverlib/roverlib.csproj", "roverlib/"]
RUN dotnet restore "console/csharprover.csproj"
COPY . .
WORKDIR "/src/console"
RUN dotnet build "csharprover.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "csharprover.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "csharprover.dll"]