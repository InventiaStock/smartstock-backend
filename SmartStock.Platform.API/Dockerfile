FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY SmartStock.Platform.API/SmartStock.Platform.API.csproj SmartStock.Platform.API/
RUN dotnet restore SmartStock.Platform.API/SmartStock.Platform.API.csproj
COPY . .
RUN dotnet publish SmartStock.Platform.API/SmartStock.Platform.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartStock.Platform.API.dll"]
