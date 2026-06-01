# -*- coding: utf-8 -*-
"""
Cào việc làm IT từ TopCV bằng Scrapling Fetcher (HTTP thuần, không cần trình duyệt).

Trang danh sách của TopCV được server-render sẵn nên ``Fetcher.get`` lấy đủ
job mà không cần Playwright/StealthyFetcher. Module này cố ý độc lập với
FastAPI để có thể chạy trực tiếp (``python scraper_topcv.py``) khi cào thử.
"""
from __future__ import annotations

import re
import sys
from typing import Iterable

from scrapling.fetchers import Fetcher


def _log(*values) -> None:
    """In thông tin chẩn đoán ra stderr, giữ stdout sạch để xuất JSON."""
    print(*values, file=sys.stderr)

import os

# Trang việc làm Công nghệ thông tin của TopCV.
BASE_URL = os.environ.get("TOPCV_BASE_URL", "https://www.topcv.vn/tim-viec-lam-cong-nghe-thong-tin-cr257")

# Tỉ giá quy đổi USD -> VND xấp xỉ, chỉ dùng khi lương niêm yết bằng USD.
USD_TO_VND = 25_000

# Các selector đã được kiểm chứng thực tế trên HTML TopCV.
SEL_CARD = ".job-item-search-result"
SEL_TITLE_ANCHOR = "h3.title a"
SEL_COMPANY = ".company-name"
SEL_SALARY = ".title-salary"
SEL_SALARY_ALT = ".salary"
SEL_CITY = ".city-text"
SEL_EXP = ".exp"

# Các cụm từ thể hiện "lương thoả thuận" -> không có số cụ thể.
_NEGOTIABLE = (
    "thoả thuận", "thỏa thuận", "thoa thuan",
    "cạnh tranh", "thương lượng", "negotiable",
)


def _clean(text) -> str:
    """Gộp danh sách text con và làm sạch khoảng trắng thừa."""
    if text is None:
        return ""
    if isinstance(text, (list, tuple)):
        text = " ".join(str(t) for t in text)
    return re.sub(r"\s+", " ", str(text)).strip()


def _text_of(el) -> str:
    """Lấy toàn bộ text con của 1 element (đệ quy) và làm sạch."""
    if not el:
        return ""
    return _clean(el.css("::text").getall())


def _to_million_vnd(value, is_usd: bool):
    """Đưa một con số về đơn vị 'triệu VND'."""
    if value is None:
        return None
    vnd = value * USD_TO_VND if is_usd else value
    if is_usd:
        vnd = vnd / 1_000_000  # USD -> VND -> triệu
    return round(vnd, 2)


def parse_vietnamese_salary(raw: str):
    """
    Phân tích chuỗi lương tiếng Việt thành (text, min, max) theo đơn vị triệu VND.

    Ví dụ:
        "11 - 13 triệu"  -> ("11 - 13 triệu", 11, 13)
        "Từ 100 triệu"   -> ("Từ 100 triệu", 100, None)
        "Tới 85 triệu"   -> ("Tới 85 triệu", None, 85)
        "Tới 6,000 USD"  -> ("Tới 6,000 USD", None, 150.0)
        "Thoả thuận"     -> ("Thoả thuận", None, None)
    """
    text = _clean(raw)
    if not text:
        return (None, None, None)

    lowered = text.lower()
    if any(marker in lowered for marker in _NEGOTIABLE):
        return (text, None, None)

    is_usd = "usd" in lowered or "$" in lowered

    numbers = []
    for token in re.findall(r"\d[\d.,]*", lowered):
        cleaned = token.replace(",", "") if is_usd else token.replace(",", ".")
        try:
            numbers.append(float(cleaned))
        except ValueError:
            # Nhiều dấu chấm: coi dấu cuối là thập phân, còn lại là phân tách nghìn.
            parts = cleaned.split(".")
            try:
                numbers.append(float("".join(parts[:-1]) + "." + parts[-1]))
            except ValueError:
                continue

    if not numbers:
        return (text, None, None)

    smin = smax = None
    if len(numbers) >= 2:
        smin = _to_million_vnd(numbers[0], is_usd)
        smax = _to_million_vnd(numbers[1], is_usd)
    else:
        value = _to_million_vnd(numbers[0], is_usd)
        if "từ" in lowered or "from" in lowered:
            smin = value
        elif any(k in lowered for k in ("tới", "đến", "upto", "up to", "lên đến")):
            smax = value
        else:
            smin = value

    return (text, smin, smax)


def _parse_card(card) -> dict:
    """Trích 1 job card thành dict khớp schema ScrapedJob của backend C#."""
    external_id = (card.attrib.get("data-job-id") or "").strip()

    anchors = card.css(SEL_TITLE_ANCHOR)
    anchor = anchors[0] if anchors else None
    url = (anchor.attrib.get("href") or "").strip() if anchor else ""
    clean_url = url.split("?")[0] if url else ""

    title = _text_of(anchor) if anchor else ""
    if not title and anchor:
        title = _clean(anchor.attrib.get("title"))

    company_els = card.css(SEL_COMPANY)
    company = _text_of(company_els[0]) if company_els else ""

    salary_els = card.css(SEL_SALARY) or card.css(SEL_SALARY_ALT)
    salary_raw = _text_of(salary_els[0]) if salary_els else ""

    city_els = card.css(SEL_CITY)
    location = _text_of(city_els[0]) if city_els else ""

    exp_els = card.css(SEL_EXP)
    experience = _text_of(exp_els[0]) if exp_els else ""

    salary_text, salary_min, salary_max = parse_vietnamese_salary(salary_raw)

    return {
        "externalId": external_id,
        "title": title,
        "companyName": company or None,
        "location": location or None,
        "salaryText": salary_text,
        "salaryMinMillionVnd": salary_min,
        "salaryMaxMillionVnd": salary_max,
        # Trang danh sách không có mô tả dài; tạm dùng tiêu đề để SkillExtractor
        # vẫn có tín hiệu. Bật fetch_details sau nếu cần mô tả đầy đủ.
        "description": title or None,
        "sourceUrl": clean_url,
        "experience": experience or None,
        "postedAt": None,
    }


def scrape_topcv(
    max_jobs: int = 50,
    max_pages: int = 5,
    base_url: str = BASE_URL,
    existing_ids: Iterable[str] | None = None,
    timeout: int = 30,
) -> list[dict]:
    """
    Cào tối đa ``max_jobs`` job MỚI từ TopCV, quét tối đa ``max_pages`` trang.

    ``existing_ids``: tập external id đã có để bỏ qua (truyền từ backend).
    """
    existing = {str(x) for x in existing_ids} if existing_ids else set()
    jobs: list[dict] = []
    seen: set[str] = set()

    for page in range(1, max_pages + 1):
        sep = "&" if "?" in base_url else "?"
        url = f"{base_url}{sep}page={page}"

        try:
            response = Fetcher.get(url, timeout=timeout)
        except Exception as exc:  # noqa: BLE001 - cào thất bại thì dừng an toàn
            _log(f"[scrape_topcv] Lỗi tải trang {page}: {exc!r}")
            break

        if response.status != 200:
            _log(f"[scrape_topcv] Trang {page} trả về status {response.status}, dừng.")
            break

        cards = response.css(SEL_CARD)
        if not cards:
            _log(f"[scrape_topcv] Trang {page} không có job card, dừng.")
            break

        for card in cards:
            job = _parse_card(card)
            jid = job["externalId"]
            if not jid or jid in seen or jid in existing:
                continue
            if not job["title"]:
                continue
            seen.add(jid)
            jobs.append(job)
            if len(jobs) >= max_jobs:
                return jobs

    return jobs


if __name__ == "__main__":
    import argparse
    import json

    try:
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    except Exception:
        pass

    parser = argparse.ArgumentParser(description="Cào việc làm IT từ TopCV.")
    parser.add_argument(
        "--json", action="store_true",
        help="Xuất JSON ra stdout (dùng khi backend gọi subprocess).",
    )
    parser.add_argument("--max-jobs", type=int, default=50)
    parser.add_argument("--max-pages", type=int, default=5)
    args = parser.parse_args()

    results = scrape_topcv(max_jobs=args.max_jobs, max_pages=args.max_pages)

    if args.json:
        # CHỈ in JSON ra stdout; mọi log khác đã đẩy sang stderr để C# parse sạch.
        json.dump(
            {"source": "TopCV", "count": len(results), "jobs": results},
            sys.stdout, ensure_ascii=False,
        )
    else:
        _log(f"\nĐã cào {len(results)} job từ TopCV.\n" + "=" * 90)
        for i, j in enumerate(results[:15], 1):
            _log(f"{i:>2}. {j['title'][:48]:48} | {(j['companyName'] or '')[:24]:24} | {j['salaryText'] or ''}")
        _log("=" * 90)
        with open("topcv_sample.json", "w", encoding="utf-8") as f:
            json.dump(results, f, ensure_ascii=False, indent=2)
        _log(f"Đã lưu {len(results)} job vào topcv_sample.json")
