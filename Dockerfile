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
# Dùng --break-system-packages vì Debian mới khoá môi trường pip hệ thống.
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-pip \
    && rm -rf /var/lib/apt/lists/*
COPY SWP-Scraper/requirements.txt /scraper/requirements.txt
RUN pip3 install --no-cache-dir --break-system-packages -r /scraper/requirements.txt
COPY SWP-Scraper/ /scraper/

# Copy output .NET đã publish.
COPY --from=build /app .

# Set environment variable to bind to port 8080 (required by Cloud Run)
ENV ASPNETCORE_URLS=http://+:8080
# Trong container dùng python3 và script đặt tại /scraper (đường dẫn tuyệt đối).
ENV MarketPulse__Scrapling__PythonExecutable=python3
ENV MarketPulse__Scrapling__ScriptPath=/scraper/scraper_topcv.py
EXPOSE 8080

ENTRYPOINT ["dotnet", "SWP-BE.dll"]
