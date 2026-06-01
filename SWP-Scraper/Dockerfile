# Image cào TopCV bằng Scrapling Fetcher (HTTP thuần) cho Cloud Run.
# Chỉ dùng Fetcher nên KHÔNG cần cài browser binaries -> image nhẹ.
FROM python:3.12-slim

# Không ghi .pyc, log ra stdout ngay (hợp với Cloud Run logging).
ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

# Cài dependency trước để tận dụng layer cache.
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy mã nguồn.
COPY app.py scraper_topcv.py ./

# Cloud Run cấp cổng qua biến môi trường $PORT (mặc định 8080).
ENV PORT=8080
EXPOSE 8080

# Dùng shell form để $PORT được nội suy đúng lúc chạy.
CMD exec uvicorn app:app --host 0.0.0.0 --port ${PORT}
