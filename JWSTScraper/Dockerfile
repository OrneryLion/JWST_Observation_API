FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["JWST_Blazor/JWST_Blazor.csproj", "JWST_Blazor/"]
COPY ["JWST_ClassLib/JWST_ClassLib.csproj", "JWST_ClassLib/"]
COPY ["JWSTScraper/JWSTScraper.csproj", "JWSTScraper/"]
RUN dotnet restore "JWSTScraper/JWSTScraper.csproj"
COPY . .
WORKDIR "/src/JWSTScraper"
RUN dotnet build "JWSTScraper.csproj" -c Release -o /app/build

FROM build AS publish
RUN apt-get update -yq \
    && apt-get install curl gnupg -yq \
    && curl -sL https://deb.nodesource.com/setup_14.x | bash \
    && apt-get install nodejs -yq
RUN dotnet publish "JWSTScraper.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JWSTScraper.dll"]
