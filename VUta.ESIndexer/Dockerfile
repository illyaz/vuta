#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["VUta.ESIndexer/VUta.ESIndexer.csproj", "VUta.ESIndexer/"]
COPY ["VUta.Database/VUta.Database.csproj", "VUta.Database/"]
RUN dotnet restore "VUta.ESIndexer/VUta.ESIndexer.csproj"
COPY . .
WORKDIR "/src/VUta.ESIndexer"
RUN dotnet build "VUta.ESIndexer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VUta.ESIndexer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VUta.ESIndexer.dll"]