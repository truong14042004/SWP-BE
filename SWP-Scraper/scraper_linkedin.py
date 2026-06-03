# -*- coding: utf-8 -*-
import sys
import json
import argparse
from urllib.parse import urlparse

from scrapling.fetchers import Fetcher

def _log(*values) -> None:
    """In thông tin chẩn đoán ra stderr, giữ stdout sạch để xuất JSON."""
    print(*values, file=sys.stderr)

def main():
    parser = argparse.ArgumentParser(description="LinkedIn Jobs Scraper")
    parser.add_argument("--json", action="store_true", help="In kết quả dạng JSON ra stdout")
    parser.add_argument("--max-jobs", type=int, default=50, help="Số lượng job tối đa cần lấy")
    parser.add_argument("--max-pages", type=int, default=5, help="Số trang tối đa cần duyệt")
    parser.add_argument("--base-url", type=str, default="", help="Base URL")
    args = parser.parse_args()

    # API Guest LinkedIn trả về danh sách job dưới dạng HTML
    base_url = args.base_url or "https://www.linkedin.com/jobs-guest/jobs/api/seeMoreJobPostings/search?keywords=IT&location=Vietnam"

    jobs_data = []
    # Khởi tạo Fetcher của scrapling để lấy dữ liệu (hỗ trợ headers, rotation tự động)
    fetcher = Fetcher()
    
    start_offset = 0
    pages_scraped = 0

    while pages_scraped < args.max_pages and len(jobs_data) < args.max_jobs:
        url = f"{base_url}&start={start_offset}"
        _log(f"[LinkedIn] Đang tải trang {pages_scraped + 1}: {url}")
        
        try:
            page = fetcher.get(url)
            if page.status != 200:
                _log(f"Lỗi: HTTP {page.status} khi truy cập {url}")
                break

            items = page.css("li")
            if not items:
                _log("Không tìm thấy job nào (có thể hết dữ liệu hoặc bị block). Dừng cào.")
                break

            for item in items:
                if len(jobs_data) >= args.max_jobs:
                    break

                title_el = item.css("h3.base-search-card__title")
                company_el = item.css("h4.base-search-card__subtitle")
                loc_el = item.css("span.job-search-card__location")
                link_el = item.css("a.base-card__full-link")
                
                if not title_el:
                    continue
                    
                title = title_el[0].css("::text").get("").strip()
                company = company_el[0].css("::text").get("").strip() if company_el else ""
                location = loc_el[0].css("::text").get("").strip() if loc_el else ""
                
                source_url = ""
                external_id = ""
                if link_el:
                    # Lấy link gốc bỏ tham số tracking query params
                    source_url = link_el[0].attrib.get("href", "").strip().split('?')[0]
                    # URL chuẩn LinkedIn job có đoạn ID ở cuối: .../view/ten-job-ID
                    if "-" in source_url:
                        external_id = source_url.split("-")[-1]

                job_dict = {
                    "externalId": external_id,
                    "title": title,
                    "companyName": company,
                    "location": location,
                    "salaryText": None,
                    "salaryMinMillionVnd": None,
                    "salaryMaxMillionVnd": None,
                    "description": None,
                    "sourceUrl": source_url,
                    "postedAt": None
                }
                jobs_data.append(job_dict)

            start_offset += 25
            pages_scraped += 1
            
        except Exception as e:
            _log(f"Exception khi cào LinkedIn: {e}")
            break

    if args.json:
        out_dict = {
            "source": "LinkedIn",
            "count": len(jobs_data),
            "jobs": jobs_data
        }
        print(json.dumps(out_dict, ensure_ascii=False, indent=2))
    else:
        _log(f"Đã cào thành công {len(jobs_data)} jobs từ LinkedIn.")

if __name__ == "__main__":
    main()
