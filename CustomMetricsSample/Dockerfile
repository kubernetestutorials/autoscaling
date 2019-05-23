FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["CustomMetricsSample/CustomMetricsSample.csproj", "CustomMetricsSample/"]
RUN dotnet restore "CustomMetricsSample/CustomMetricsSample.csproj"
COPY . .
WORKDIR "/src/CustomMetricsSample"
RUN dotnet build "CustomMetricsSample.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "CustomMetricsSample.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "CustomMetricsSample.dll"]