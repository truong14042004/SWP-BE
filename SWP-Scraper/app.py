# -*- coding: utf-8 -*-
"""
FastAPI microservice bọc Scrapling để cào việc làm cho backend SWP-BE.

Endpoint nội bộ, bảo vệ bằng header ``X-Internal-Token`` (so khớp biến môi
trường ``INTERNAL_TOKEN``). KHÔNG mở công khai ra Internet.
"""
from __future__ import annotations

import os

from fastapi import FastAPI, Header, HTTPException, Query
from fastapi.responses import JSONResponse

from scraper_topcv import scrape_topcv

app = FastAPI(
    title="SWP Scraper Service",
    description="Microservice cào việc làm bằng Scrapling (Fetcher).",
    version="1.0.0",
)

# Token nội bộ; nếu chưa cấu hình, service sẽ từ chối mọi request có bảo vệ.
INTERNAL_TOKEN = os.environ.get("INTERNAL_TOKEN", "")


def _check_token(provided: str | None) -> None:
    """Xác thực header X-Internal-Token; ném HTTPException nếu sai."""
    if not INTERNAL_TOKEN:
        raise HTTPException(status_code=503, detail="INTERNAL_TOKEN chưa được cấu hình.")
    if not provided or provided != INTERNAL_TOKEN:
        raise HTTPException(status_code=401, detail="Token nội bộ không hợp lệ.")


@app.get("/health")
def health():
    """Health check cho Cloud Run (không cần token)."""
    return {"status": "healthy", "token_configured": bool(INTERNAL_TOKEN)}


@app.get("/scrape/topcv")
def scrape_topcv_endpoint(
    max_jobs: int = Query(50, ge=1, le=200),
    max_pages: int = Query(5, ge=1, le=20),
    x_internal_token: str | None = Header(default=None),
):
    """Cào job IT từ TopCV, trả mảng JSON khớp schema ScrapedJob của backend."""
    _check_token(x_internal_token)

    jobs = scrape_topcv(max_jobs=max_jobs, max_pages=max_pages)
    return JSONResponse(
        content={
            "source": "TopCV",
            "count": len(jobs),
            "jobs": jobs,
        }
    )


if __name__ == "__main__":
    import uvicorn

    port = int(os.environ.get("PORT", "8000"))
    uvicorn.run(app, host="0.0.0.0", port=port)
