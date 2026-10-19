FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore CMS_Scrappers.csproj

COPY . .
RUN dotnet publish CMS_Scrappers.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:5201
EXPOSE 5201

ENTRYPOINT ["dotnet", "CMS_Scrappers.dll"]