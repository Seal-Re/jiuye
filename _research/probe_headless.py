# -*- coding: utf-8 -*-
"""Smoke test: confirm python-playwright launches chromium HEADLESS (no foreground window)."""
import sys
sys.stdout.reconfigure(encoding="utf-8")
try:
    from playwright.sync_api import sync_playwright
except Exception as e:
    print("IMPORT_FAIL:", e); sys.exit(2)

try:
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()
        page.goto("https://example.com", timeout=20000)
        print("TITLE:", page.title())
        browser.close()
    print("HEADLESS_OK")
except Exception as e:
    print("LAUNCH_FAIL:", repr(e)); sys.exit(3)
