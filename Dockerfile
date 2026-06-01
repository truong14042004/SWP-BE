# Stage 1: Build the C# application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

# Stage 2: Create the runtime container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# --- Cài Python + Scrapling để chạy scraper TopCV như tiến trình con ---
# Image aspnet:8.0 dựa trên Debian; cài python3 + pip rồi cài thư viện scraper.
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-pip \
    && rm -rf /var/lib/apt/lists/*

# Thư viện hệ thống mà trình duyệt tàng hình (camoufox / Firefox) cần để chạy
# headless. Thiếu các lib này thì StealthyFetcher sẽ không khởi động được browser.
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgtk-3-0 libx11-xcb1 libasound2 libdbus-glib-1-2 libxtst6 \
    libxrandr2 libpangocairo-1.0-0 libgbm1 libxcomposite1 libxdamage1 \
    libxfixes3 libcups2 libnss3 libatk1.0-0 libatk-bridge2.0-0 \
    libxshmfence1 libxcb-shm0 libxcb1 fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

COPY SWP-Scraper/requirements.txt /scraper/requirements.txt
RUN pip3 install --no-cache-dir --break-system-packages -r /scraper/requirements.txt

# Tải binary trình duyệt (camoufox + chromium) vào image để StealthyFetcher dùng.
# Đặt vị trí cache cố định để tiến trình con (do .NET spawn) tìm thấy lúc chạy.
ENV CAMOUFOX_DOWNLOAD_PATH=/opt/camoufox
RUN scrapling install

COPY SWP-Scraper/ /scraper/

# Copy output .NET đã publish.
COPY --from=build /app .

# Set environment variable to bind to port 8080 (required by Cloud Run)
ENV ASPNETCORE_URLS=http://+:8080
# Trong container dùng python3 và script đặt tại /scraper (đường dẫn tuyệt đối).
ENV MarketPulse__Scrapling__PythonExecutable=python3
ENV MarketPulse__Scrapling__ScriptPath=/scraper/scraper_topcv.py
# Bật trình duyệt tàng hình để vượt 403 từ IP datacenter (đặt 0 để tắt).
ENV TOPCV_USE_STEALTH=1
EXPOSE 8080

ENTRYPOINT ["dotnet", "SWP-BE.dll"]
