FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["BlazorBrain.Client/BlazorBrain.Client.csproj", "BlazorBrain.Client/"]
RUN dotnet restore "BlazorBrain.Client/BlazorBrain.Client.csproj"
COPY . .
WORKDIR "/src/BlazorBrain.Client"
RUN dotnet build "BlazorBrain.Client.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlazorBrain.Client.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorBrain.Client.dll"]
