FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["BlazorBrain.Api.Web/BlazorBrain.Api.Web.csproj", "BlazorBrain.Api.Web/"]
RUN dotnet restore "BlazorBrain.Api.Web/BlazorBrain.Api.Web.csproj"
COPY . .
WORKDIR "/src/BlazorBrain.Api.Web"
RUN dotnet build "BlazorBrain.Api.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlazorBrain.Api.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorBrain.Api.Web.dll"]
