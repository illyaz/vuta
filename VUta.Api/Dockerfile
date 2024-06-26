#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["VUta.Api/VUta.Api.csproj", "VUta.Api/"]
COPY ["VUta.Database/VUta.Database.csproj", "VUta.Database/"]
COPY ["VUta.Transport/VUta.Transport.csproj", "VUta.Transport/"]
RUN dotnet restore "VUta.Api/VUta.Api.csproj"
COPY . .
WORKDIR "/src/VUta.Api"
RUN dotnet build "VUta.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VUta.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

# OpenCV dependencies
RUN apt update && \
	apt -y install libgeotiff-dev libdc1394-dev libavcodec-dev libswscale-dev libavformat-dev libgtk-3-dev libvtk9 && \
	rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VUta.Api.dll"]