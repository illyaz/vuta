#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["YoutubeExplode/NuGet.config", "YoutubeExplode/"]
COPY ["VUta.Worker/VUta.Worker.csproj", "VUta.Worker/"]
COPY ["VUta.Database/VUta.Database.csproj", "VUta.Database/"]
COPY ["VUta.Transport/VUta.Transport.csproj", "VUta.Transport/"]
COPY ["YoutubeExplode/YoutubeExplode/YoutubeExplode.csproj", "YoutubeExplode/YoutubeExplode/"]
RUN dotnet restore "VUta.Worker/VUta.Worker.csproj"
COPY . .
WORKDIR "/src/VUta.Worker"
RUN dotnet build "VUta.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VUta.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VUta.Worker.dll"]