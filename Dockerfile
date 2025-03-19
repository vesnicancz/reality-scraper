FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopírování souborů projektu a obnovení závislostí
COPY ["RealityScraper/RealityScraper.csproj", "RealityScraper/"]
RUN dotnet restore "RealityScraper/RealityScraper.csproj"

# Kopírování všech souborů a kompilace
COPY RealityScraper/. ./RealityScraper/
WORKDIR /src/RealityScraper
RUN dotnet build "RealityScraper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RealityScraper.csproj" -c Release -o /app/publish

# Použití obrazu s Chrome a .NET runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final

# Instalace Chrome a potřebných balíčků
RUN apt-get update && apt-get install -y \
    wget \
    gnupg \
    unzip \
    fonts-liberation \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libcups2 \
    libdbus-1-3 \
    libgbm1 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libx11-xcb1 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    xdg-utils \
    libxss1 \
    libxkbcommon0 \
    libpci3 \
    libcurl4 \
    --no-install-recommends

# Stažení a instalace Chrome
RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list \
    && apt-get update \
    && apt-get install -y google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

# Stažení ChromeDriver
RUN mkdir -p /app/drivers \
    && CHROME_VERSION=$(google-chrome --version | awk '{print $3}' | cut -d '.' -f 1-3) \
    && CHROMEDRIVER_VERSION=$(wget -qO- "https://chromedriver.storage.googleapis.com/LATEST_RELEASE_$CHROME_VERSION") \
    && wget -q --continue -P /tmp "https://chromedriver.storage.googleapis.com/$CHROMEDRIVER_VERSION/chromedriver_linux64.zip" \
    && unzip /tmp/chromedriver_linux64.zip -d /app/drivers \
    && chmod +x /app/drivers/chromedriver \
    && rm /tmp/chromedriver_linux64.zip

WORKDIR /app
COPY --from=publish /app/publish .

# Vytvoření adresáře pro data
RUN mkdir -p /app/data
VOLUME ["/app/data"]

# Kopírování konfiguračního souboru
COPY ["RealityScraper/appsettings.json", "./"]
COPY ["RealityScraper/appsettings.production.json", "./"]

# Nastavení pracovní složky pro databázi
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/reality-listings.db"

ENTRYPOINT ["dotnet", "RealityScraper.dll"]