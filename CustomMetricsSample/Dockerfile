FROM microsoft/dotnet:2.1-sdk AS build
FROM build AS publish
WORKDIR /src
COPY . .
RUN dotnet build "CustomMetricsSample.csproj" -c Release -o /app
RUN dotnet publish "CustomMetricsSample.csproj" -c Release -o /app

FROM microsoft/dotnet
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CustomMetricsSample.dll"]