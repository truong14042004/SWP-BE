# SWP-Scraper

Microservice cào việc làm bằng [Scrapling](https://github.com/D4Vinci/Scrapling) (`Fetcher` — HTTP thuần, không cần trình duyệt), đóng gói FastAPI để backend **SWP-BE** (C#) gọi sang. Đây là phần "cào" trong kiến trúc 2B; phần lưu DB / trích kỹ năng / API vẫn do backend C# đảm nhiệm.

## Cấu trúc

| File | Vai trò |
|---|---|
| `scraper_topcv.py` | Cào TopCV bằng Scrapling `Fetcher` + parser lương tiếng Việt |
| `app.py` | FastAPI: `GET /scrape/topcv`, `GET /health`, bảo vệ bằng `X-Internal-Token` |
| `requirements.txt` | `scrapling[fetchers]`, `fastapi`, `uvicorn` |
| `Dockerfile` | Image cho Cloud Run (lắng nghe `$PORT`) |

## Schema trả về

`GET /scrape/topcv` trả JSON khớp với `ScrapedJob` của backend:

```json
{
  "source": "TopCV",
  "count": 50,
  "jobs": [
    {
      "externalId": "2002623",
      "title": "Lập Trình Viên Backend (Python)",
      "companyName": "CÔNG TY TNHH EAERA VIỆT NAM",
      "location": "Hà Nội",
      "salaryText": "600 - 900 USD",
      "salaryMinMillionVnd": 15.0,
      "salaryMaxMillionVnd": 22.5,
      "description": "Lập Trình Viên Backend (Python)",
      "experience": "2 năm",
      "postedAt": null,
      "sourceUrl": "https://www.topcv.vn/viec-lam/.../2002623.html"
    }
  ]
}
```

---

## 1. Chạy & test ở local

```powershell
# Cài dependency
pip install -r requirements.txt

# Đặt token nội bộ (PowerShell)
$env:INTERNAL_TOKEN = "doi-thanh-mot-chuoi-bi-mat-dai"

# Chạy service
python app.py
# hoặc: uvicorn app:app --host 0.0.0.0 --port 8000
```

Test bằng cửa sổ PowerShell khác:

```powershell
# Health (không cần token)
curl http://localhost:8000/health

# Cào (cần token đúng)
curl -H "X-Internal-Token: doi-thanh-mot-chuoi-bi-mat-dai" `
  "http://localhost:8000/scrape/topcv?max_jobs=10"
```

Sai/thiếu token sẽ trả `401`. Chưa cấu hình `INTERNAL_TOKEN` trả `503`.

---

## 2. Build & chạy bằng Docker (local)

```powershell
docker build -t swp-scraper .

docker run --rm -p 8000:8080 -e INTERNAL_TOKEN="chuoi-bi-mat" swp-scraper
# Mở http://localhost:8000/health
```

---

## 3. Deploy lên Cloud Run

> [!IMPORTANT]
> Thay `PROJECT_ID` và `REGION` (ví dụ `asia-northeast1`) cho đúng dự án của bạn.

### Cách A — Deploy thẳng từ source (đơn giản nhất)

Cloud Run tự build bằng buildpacks/Dockerfile, không cần Docker local:

```powershell
gcloud run deploy swp-scraper `
  --source . `
  --project PROJECT_ID `
  --region REGION `
  --memory 4Gi `
  --no-allow-unauthenticated `
  --ingress internal `
  --set-env-vars "INTERNAL_TOKEN=chuoi-bi-mat-dai-32-ky-tu"
```

Giải thích các cờ quan trọng:
- `--memory 4Gi` — đúng mức RAM bạn muốn (thừa sức cho `Fetcher`).
- `--no-allow-unauthenticated` — **không** cho gọi ẩn danh từ Internet.
- `--ingress internal` — chỉ nhận traffic nội bộ trong VPC/project (backend gọi vào). Đây là lớp bảo vệ chính.
- `--set-env-vars INTERNAL_TOKEN=...` — token nội bộ. Xem mục bảo mật bên dưới để dùng Secret Manager thay vì để lộ ở đây.

### Cách B — Build image rồi deploy

```powershell
# Build & push image bằng Cloud Build
gcloud builds submit --tag REGION-docker.pkg.dev/PROJECT_ID/swp/swp-scraper --project PROJECT_ID

# Deploy image
gcloud run deploy swp-scraper `
  --image REGION-docker.pkg.dev/PROJECT_ID/swp/swp-scraper `
  --project PROJECT_ID --region REGION `
  --memory 4Gi --no-allow-unauthenticated --ingress internal `
  --set-env-vars "INTERNAL_TOKEN=chuoi-bi-mat-dai-32-ky-tu"
```

---

## 4. Bảo mật token bằng Secret Manager (khuyến nghị)

Tránh để token thẳng trong lệnh deploy:

```powershell
# Tạo secret
echo "chuoi-bi-mat-dai-32-ky-tu" | gcloud secrets create swp-scraper-token --data-file=- --project PROJECT_ID

# Gắn secret vào Cloud Run (thay --set-env-vars ở trên bằng dòng này)
gcloud run deploy swp-scraper --source . --project PROJECT_ID --region REGION `
  --memory 4Gi --no-allow-unauthenticated --ingress internal `
  --update-secrets "INTERNAL_TOKEN=swp-scraper-token:latest"
```

---

## 5. Backend C# gọi vào (giai đoạn 2)

Sau khi deploy, lấy URL service (dạng `https://swp-scraper-xxxx.a.run.app`). Backend sẽ cấu hình:

```json
"MarketPulse": {
  "ScraplingApi": {
    "Enabled": true,
    "BaseUrl": "https://swp-scraper-xxxx.a.run.app",
    "TimeoutSeconds": 60
  }
}
```

Token đặt qua biến môi trường/secret của backend (không ghi vào file). Vì service để `--ingress internal`, backend phải nằm trong cùng project/VPC mới gọi được.

> [!NOTE]
> **Trigger theo lịch.** Timer nền trong tiến trình C# không đáng tin trên Cloud Run (CPU bị throttle khi idle). Dùng **Cloud Scheduler** gọi `POST /api/market/internal/scrape` của backend theo giờ; backend sẽ gọi tiếp sang service này.

---

## Nâng cấp khi bị chặn

Nếu sau này IP Cloud Run bị TopCV chặn (hiện listing chạy tốt bằng `Fetcher`), đổi sang trình duyệt tàng hình trong `scraper_topcv.py`:

```python
from scrapling.fetchers import StealthyFetcher
response = StealthyFetcher.fetch(url, headless=True, network_idle=True)
```

Khi đó cần bổ sung cài browser vào `Dockerfile` (`RUN scrapling install`) và tăng RAM — image sẽ nặng hơn đáng kể.
