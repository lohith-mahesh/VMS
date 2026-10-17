from __future__ import annotations

import argparse
import csv
import io
import json
import os
import re
import sqlite3
import threading
import uuid
import webbrowser
import zipfile
from copy import deepcopy
from datetime import datetime, timedelta, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlsplit
from xml.sax.saxutils import escape as xml_escape


APP_TITLE = "RR Visitor Management System"
HOST = "127.0.0.1"
PORT = 8000
LOCK = threading.RLock()
IST = timezone(timedelta(hours=5, minutes=30), name="IST")
APP_DIRECTORY = Path(__file__).resolve().parent
DATABASE_PATH = Path(os.environ.get("VISITOR_DATABASE_PATH", str(APP_DIRECTORY / "visitor_management.sqlite3")))
BRAND_LOGO_DATA_URI = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHhtbG5zOnhsaW5rPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hsaW5rIiB2ZXJzaW9uPSIxLjEiIGlkPSJMYXllcl8xIiB4PSIwcHgiIHk9IjBweCIgdmlld0JveD0iLTQ2MyA0NTAuMSAxNTEuMiAxODAuOSIgc3R5bGU9ImVuYWJsZS1iYWNrZ3JvdW5kOm5ldyAtNDYzIDQ1MC4xIDE1MS4yIDE4MC45OyIgeG1sOnNwYWNlPSJwcmVzZXJ2ZSI+CiA8c3R5bGUgdHlwZT0idGV4dC9jc3MiPgogIC5zdDB7ZmlsbDojZmZmO30KIDwvc3R5bGU+CiA8ZyB0cmFuc2Zvcm09InRyYW5zbGF0ZSgwLjAwMDAwMCwyNjAuMDAwMDAwKSBzY2FsZSgwLjEwMDAwMCwtMC4xMDAwMDApIj4KICA8cGF0aCBjbGFzcz0ic3QwIiBkPSJNLTQ2MjguMi0xOTE0LjRjMC05LDYtMTYsMTMtMTZjMTgsMCw1My0zOSw2Mi02OGM0LTEzLDctMzI2LDctNjk2YzAtNjA4LTEtNjc2LTE2LTcwOGMtOS0xOS0yOC00MS00MS00OCAgIGMtMjQtMTMtMzMtMjktMTctMzFjNC0xLDc2LTIsMTU5LTNjMTE4LTEsMTU0LDEsMTU4LDEyYzIsNy02LDE3LTIwLDIyYy0xMyw1LTMyLDI0LTQxLDQyYy0xNSwyOS0xOCw2OS0xOSwzMTZsLTEsMjgybDc1LDEgICBjNDIsMSw4NSwzLDk2LDRjNTUsNiwxMjUsMTcsMTcyLDI3YzY1LDE0LDcyLDksMTMxLTk1Yzc3LTEzNCwxMjYtMjk1LDEzOC00NTNjNi04MCw2LTgxLTI1LTExNmMtMTctMjAtMjgtMzgtMjQtNDAgICBjMTgtOSwzMjctMiwzMzAsN2MxLDYtMTMsMjAtMzIsMzJjLTQ5LDMyLTYwLDUzLTk1LDE4M2MtNTUsMjA0LTEyOSwzNzAtMjEwLDQ3OGMtMjIsMjktNDAsNTQtNDAsNTZzMjUsMTgsNTYsMzYgICBjMjE0LDEyMCwyOTUsMzUxLDE5NCw1NDljLTUxLDEwMC0xMzUsMTcwLTI1OSwyMTVjLTUxLDE4LTg3LDIwLTQwMywyNEMtNDU5Mi4yLTE4OTkuNC00NjI4LjItMTkwMC40LTQ2MjguMi0xOTE0LjR6ICAgIE0tMzk0MC4yLTIwMTcuNGMxMTctNDcsMTg3LTE0MCwxOTYtMjYyYzE2LTIyNi0xMzgtMzgyLTQyOS00MzZjLTcyLTEzLTE5Ny0xOC0yMDQtN2MtNSw4LTcsNjYyLTIsNzIxYzEsMTEsMzYsMTIsMTk0LDkgICBDLTQwMjAuMi0xOTk2LjQtMzk4Ni4yLTE5OTkuNC0zOTQwLjItMjAxNy40eiI+CiAgPC9wYXRoPgogIDxwYXRoIGNsYXNzPSJzdDAiIGQ9Ik0tNDI1NC4yLTIxMjkuNGMtOS05LTMtMTgsMjAtMjljNTYtMjUsNjEtNDgsNjItMjc0YzAtMTE0LDMtMjExLDctMjE0YzgtOCw5MCwxMywxMjksMzNsMjgsMTNsMiwxOTNsMywxOTIgICBsOTMtMWMxMDMtMSwxMDMtMSw2MCw2MWwtMjAsMzBoLTE5MEMtNDE2NC4yLTIxMjUuNC00MjUxLjItMjEyNy40LTQyNTQuMi0yMTI5LjR6Ij4KICA8L3BhdGg+CiAgPHBhdGggY2xhc3M9InN0MCIgZD0iTS0zNTQ0LjItMjE1Ni40YzQtMTEsMTItNDAsMTgtNjVjOC0zMywxOS01MSw0MC02NWM2NS00MiwxMTgtMTUzLDExOC0yNDhjMC0xNTAtODYtMjc1LTI0MS0zNDlsLTY3LTMybDE2LTM3ICAgYzEwLTIyLDI0LTM4LDM0LTM4YzIzLDAsODEtODMsMTI4LTE4M2M1My0xMTQsODktMjUxLDk3LTM2OWM1LTk0LDUtOTctMjEtMTIzYy0xNC0xNC0yNi0zMC0yNi0zNWMwLTYsNjctMTAsMTY1LTEwICAgYzE3NywwLDE5NSw2LDEzMiw0NWMtNDMsMjctNzIsODMtOTcsMTg5Yy00MSwxNjktMTI4LDM2Ni0yMTEsNDc1Yy0yMiwyOS0zOSw1NC0zNyw1NmMyLDEsMjgsMTUsNTgsMzFzODAsNTQsMTExLDg1ICAgYzE0MywxNDQsMTY2LDM0Niw1OCw1MDVjLTU2LDgyLTE2NSwxNjAtMjQ4LDE3OUMtMzU0Ny4yLTIxMzkuNC0zNTUwLjItMjE0MC40LTM1NDQuMi0yMTU2LjR6Ij4KICA8L3BhdGg+CiAgPHBhdGggY2xhc3M9InN0MCIgZD0iTS00MDUzLjItMjg0MS40Yy0xNi00LTUwLTEwLTc1LTEzbC00NS02di0zNzNjLTEtMzQ2LTItMzc0LTE5LTQwNGMtMTEtMTctMzAtMzYtNDMtNDJjLTUzLTI0LTIxLTMxLDE0Mi0zMSAgIGM5MSwwLDE2NSw0LDE2NSw5cy0xMSwxNi0yNSwyNWMtNTEsMzMtNTQsNTItNTYsMzUwYy0xLDE1MiwwLDI3OCwyLDI4MWMzLDMsNTcsMTEsMTAxLDE0YzExLDEtMTMsNTQtMzAsNjhjLTgsNy0yNyw5LTQzLDYgICBjLTMxLTYtMzQsMS0yOSw4M0MtNDAwNi4yLTI4MzYuNC00MDEzLjItMjgzMS40LTQwNTMuMi0yODQxLjR6Ij4KICA8L3BhdGg+CiA8L2c+Cjwvc3ZnPg=="
INDEX_TEMPLATE = r"""
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<meta name="theme-color" content="#10069F">
<link rel="icon" type="image/svg+xml" href="__BRAND_LOGO__">
<title>RR Visitor Management System</title>
<style>
:root{--brand:#10069F;--brand-dark:#02003d;--action:#0067EF;--action-dark:#0054C7;--action-soft:#EAF3FF;--white:#fff;--title:#171717;--ink:#171717;--muted:#525252;--surface:#f5f5f7;--silver:#e5e7eb;--success:#819C00;--danger:#9F0000;--warning:#d99800;font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;color:var(--ink);background:var(--surface);font-synthesis:none;text-rendering:optimizeLegibility;-webkit-font-smoothing:antialiased}
*{box-sizing:border-box}body{margin:0;min-width:320px;background:var(--surface)}button,input,select,textarea{font:inherit}button:not(:disabled),a,select,summary{cursor:pointer}button:disabled{cursor:not-allowed}a{color:inherit;text-decoration:none}h1,h2,h3,p{margin-top:0}h1,h2,h3{color:var(--title)}.display{font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif}.hidden{display:none!important}.app{min-height:100vh}.login-page{min-height:100vh;display:flex;align-items:center;justify-content:center;padding:48px 24px}.login-card{width:100%;max-width:448px;border:1px solid var(--silver);background:#fff;box-shadow:0 4px 18px rgba(20,32,48,.08)}.login-body{padding:32px}.eyebrow{color:var(--brand);font-size:11px;font-weight:700;letter-spacing:.18em;text-transform:none}.login-body h2{color:var(--title);font-size:24px;margin:8px 0 0}.muted{color:var(--muted)}.small{font-size:13px}.tiny{font-size:11px}.primary-button,.secondary-button,.success-button,.danger-button,.warning-button{min-height:40px;border-radius:4px;padding:10px 16px;font-size:14px;font-weight:650;border:1px solid transparent}.primary-button{background:var(--action);color:#fff}.primary-button:hover{background:var(--action-dark)}.secondary-button{background:#fff;border-color:var(--action);color:var(--action)}.secondary-button:hover{background:var(--action-soft)}.success-button{background:var(--success);color:var(--title)}.danger-button{background:var(--danger);color:#fff}.warning-button{background:#ffc107;color:var(--title)}.wide{width:100%}.user-list{display:grid;gap:8px;margin-top:14px}.user-choice{display:flex;width:100%;align-items:center;justify-content:space-between;border:1px solid var(--silver);background:#fff;padding:12px 16px;text-align:left}.user-choice:hover{background:var(--surface)}.user-choice strong,.user-choice span{display:block}.shell{display:flex;min-height:100vh}.sidebar{width:256px;flex:0 0 256px;background:var(--brand);color:#fff;display:flex;flex-direction:column}.sidebar-heading{height:80px;display:flex;align-items:center;padding:0 24px;border-bottom:1px solid var(--brand-dark);font-weight:700;font-size:14px}.sidebar-nav{padding:26px 0;flex:1}.nav-label{padding:10px 20px 7px;color:#fff;font-size:10px;font-weight:700;letter-spacing:.22em;text-transform:none}.nav-link{display:flex;align-items:center;border-left:2px solid transparent;padding:10px 20px;color:#fff;font-size:14px;transition:.15s}.nav-link:hover{background:var(--brand-dark);color:#fff}.nav-link.active{border-left-color:#fff;background:#e9eef6;color:var(--brand);font-weight:650}.workspace{display:flex;min-width:0;flex:1;flex-direction:column}.topbar{height:80px;display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid var(--silver);background:#fff;padding:0 32px;position:relative}.search{width:320px;border:1px solid var(--silver);padding:10px 12px;font-size:13px}.top-actions{display:flex;align-items:center;gap:20px}.text-button{border:1px solid transparent;border-radius:4px;background:transparent;color:var(--action);font-size:14px;font-weight:650;padding:9px 13px}.text-button:hover{background:var(--action-soft)}.content{padding:36px 32px 56px;min-width:0}.page{max-width:1440px;margin:0 auto}.page-header{display:flex;align-items:flex-end;justify-content:space-between;gap:16px;margin-bottom:24px}.page-header h1{color:var(--title);font-size:38px;margin:8px 0 0}.page-header p:last-child{margin:8px 0 0}.status-chip{display:inline-block;white-space:nowrap;border-radius:999px;background:#e9eef6;padding:5px 9px;color:var(--brand);font-size:11px;font-weight:700;text-transform:none}.status-live{border-color:var(--success);background:#F3F6E6;color:var(--title)}.status-banner{display:flex;align-items:center;justify-content:space-between;gap:16px;border:1px solid #e6d29a;background:#fff9e8;padding:12px 16px;color:#705b18;font-size:13px;margin-bottom:24px}.summary-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px;margin-bottom:24px}.summary-card,.panel,.info-card{border:1px solid var(--silver);background:#fff}.summary-card{padding:16px}.summary-card p{min-height:34px;color:var(--muted);font-size:11px;font-weight:700;line-height:1.25;text-transform:none;margin:0}.summary-card strong{display:block;margin-top:8px;color:var(--brand);font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;font-size:32px}.panel,.info-card{padding:20px;margin-bottom:24px;overflow:hidden}.panel-heading{display:flex;align-items:center;justify-content:space-between;gap:16px;border-bottom:1px solid var(--silver);padding-bottom:16px}.panel-heading h2,.info-card h2{color:var(--title);font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;font-size:20px;margin:4px 0 0}.split{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:24px}.metric-number{font-size:32px;color:var(--brand);font-weight:700;margin:16px 0 6px}.table-wrap{overflow-x:auto}.data-table{width:100%;border-collapse:collapse;text-align:left;font-size:13px}.data-table th{border-bottom:1px solid var(--silver);padding:13px;color:var(--muted);font-size:10px;text-transform:none;letter-spacing:.05em}.data-table td{border-bottom:1px solid var(--silver);padding:13px;color:var(--muted);vertical-align:top}.data-table td.strong{color:var(--ink);font-weight:650}.data-table tr:last-child td{border-bottom:0}.data-table tbody tr:hover{background:#fafafa}.link{color:var(--brand);font-weight:650}.empty{padding:24px 0;color:var(--muted);font-size:14px}.error-box{display:flex;align-items:center;justify-content:space-between;gap:16px;border:1px solid #e1b5b5;background:#fff4f4;color:var(--danger);padding:16px;font-size:14px;margin-bottom:20px}.form-page{max-width:896px;margin:0 auto}.form-section{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px;border:1px solid var(--silver);background:#fff;padding:24px;margin-bottom:24px}.form-section.single{display:block}.field{display:block;font-size:13px;font-weight:650;color:var(--ink)}.field input,.field select,.field textarea{display:block;width:100%;margin-top:8px;border:1px solid var(--silver);background:#fff;padding:10px 12px;font-weight:400;color:var(--ink);outline:none}.field input:focus,.field select:focus,.field textarea:focus{border-color:var(--brand);box-shadow:0 0 0 2px rgba(4,1,90,.08)}.field-error{display:block;color:var(--danger);font-size:11px;font-weight:400;margin-top:5px}.check-row{display:flex;align-items:center;gap:12px;border:1px solid var(--silver);background:#fff;padding:16px;margin-bottom:12px;font-size:14px;font-weight:650}.date-row,.asset-row{display:grid;grid-template-columns:1fr 1fr 1fr auto;gap:12px;border:1px solid var(--silver);padding:12px;margin-top:12px;align-items:end}.button-row{display:flex;flex-wrap:wrap;gap:12px;align-items:center}.review-box{border:1px solid var(--brand);background:#f4f7fb;padding:20px;margin-bottom:24px}.review-box h2{color:var(--title);font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;margin:0}.classification-grid{display:grid;grid-template-columns:repeat(2,1fr);gap:12px;margin:16px 0}.classification{display:flex;align-items:center;gap:12px;border:1px solid var(--silver);background:#fff;padding:12px;font-size:13px;font-weight:650}.swatch{width:12px;height:12px;border-radius:50%}.reception-actions{display:flex;flex-wrap:wrap;align-items:center;gap:12px;border:1px solid var(--brand);background:#e9eef6;padding:20px;margin-bottom:24px}.day-picker{border:1px solid var(--silver);background:#fff;padding:9px 10px}.info-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:24px}.info-card{margin:0}.info-card p{font-size:13px;margin:0 0 9px}.info-card p:last-child{margin-bottom:0}.info-card .record{border-bottom:1px solid var(--silver);padding-bottom:10px;margin-bottom:10px}.info-card .record:last-child{border-bottom:0;margin-bottom:0}.detail-stack{display:grid;gap:24px}.back-link{display:inline-block;margin-bottom:24px}.modal-backdrop{position:fixed;inset:0;z-index:80;display:flex;align-items:center;justify-content:center;background:rgba(0,0,0,.5);padding:16px}.modal{width:100%;max-width:520px;border:1px solid var(--silver);background:#fff;padding:24px;box-shadow:0 20px 60px rgba(0,0,0,.25)}.modal h2{font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;color:var(--title);margin:0}.modal-actions{display:flex;justify-content:flex-end;gap:12px;margin-top:24px}.mobile-menu{display:none}.mobile-records{display:none}.toast{position:fixed;right:24px;bottom:24px;z-index:100;background:var(--title);color:#fff;padding:13px 16px;box-shadow:0 8px 28px rgba(0,0,0,.25);font-size:13px;max-width:360px}.toast.error{background:var(--danger)}.loading{color:var(--muted);font-size:14px}
.required-mark{color:var(--danger);font-weight:800}.required-note{margin:-10px 0 14px;color:var(--muted);font-size:12px;text-align:right}.section-span{grid-column:1/-1;border-bottom:1px solid var(--silver);padding-bottom:14px;margin-bottom:2px}.section-span h2{margin:5px 0 0;color:var(--title);font-size:21px}.schedule-card{border:1px solid var(--silver);background:#fff;padding:24px;margin-bottom:24px}.schedule-heading{display:flex;align-items:flex-start;justify-content:space-between;gap:20px;border-bottom:1px solid var(--silver);padding-bottom:18px}.schedule-heading h2{margin:5px 0 6px;color:var(--title);font-size:24px}.timezone-pill{white-space:nowrap;border-radius:999px;background:#e9eef6;color:var(--brand);padding:7px 11px;font-size:11px;font-weight:700}.schedule-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:14px;margin-top:20px}.requested-field{border-left:4px solid var(--warning);background:#fff8e1;padding:10px}.phone-fields{display:grid;grid-template-columns:minmax(210px,1fr) minmax(180px,1fr);gap:12px}.phone-input{position:relative}.phone-prefix{position:absolute;left:12px;bottom:11px;color:var(--muted);font-size:13px}.phone-input input{padding-left:58px}.field-note{display:block;margin-top:5px;color:var(--muted);font-size:11px;font-weight:400}.checkbox-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:8px;margin-top:12px}.checkbox-grid .check-row{margin:0}.summary-link{display:block;position:relative}.summary-link:after{content:'View →';display:block;margin-top:10px;color:var(--brand);font-size:11px;font-weight:700}.summary-link:hover{border-color:var(--brand);box-shadow:0 4px 14px rgba(4,1,90,.08)}.verification-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px;margin:18px 0}.verification-step{border:1px solid #cfd5df;border-radius:8px;background:#fff;padding:16px}.verification-step h3{font-size:14px;margin:0}.verification-step .button-row{margin-top:14px}.badge-panel{display:grid;grid-template-columns:minmax(180px,1fr) minmax(220px,2fr);gap:14px;align-items:end;border:1px solid #cfd5df;border-radius:8px;background:#fff;padding:16px;margin-bottom:14px}.badge-type{display:inline-block;border-radius:999px;padding:7px 11px;color:#fff;font-size:12px;font-weight:750}.badge-orange{background:#a64b00}.badge-red{background:var(--danger)}.completion-checks{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.completion-check{display:flex;align-items:center;gap:10px;border:1px solid var(--brand);border-radius:8px;background:#fff;padding:14px;font-weight:700}.completion-check input{width:19px;height:19px}
:focus-visible{outline:3px solid var(--brand);outline-offset:3px}.skip-link{position:fixed;left:16px;top:-60px;z-index:120;border-radius:6px;background:#fff;color:var(--brand);padding:10px 14px;font-weight:700;box-shadow:0 4px 16px rgba(0,0,0,.2)}.skip-link:focus{top:16px}.primary-button,.secondary-button,.success-button,.danger-button,.warning-button,.text-button,.user-choice,.reception-person{transition:background-color .15s,border-color .15s,box-shadow .15s,transform .15s}.primary-button:active,.secondary-button:active,.success-button:active,.danger-button:active,.warning-button:active,.text-button:active,.user-choice:active,.reception-person:active{transform:translateY(1px)}.summary-card,.panel,.info-card,.form-section,.schedule-card,.review-box,.reception-actions,.login-card{border-radius:10px}.field input,.field select,.field textarea,.search{border-radius:7px;min-height:42px}.compact-button{display:inline-block;min-height:34px;padding:7px 10px}.form-record{display:flex;justify-content:space-between;align-items:center;gap:12px}.selection-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px;margin:18px 0}.selection-card{display:grid;grid-template-columns:auto minmax(0,1fr) auto auto;align-items:center;gap:10px;border:1px solid #cfd5df;border-radius:8px;background:#fff;padding:12px;cursor:pointer}.selection-card:has(input:checked){border-color:var(--brand);box-shadow:0 0 0 2px rgba(4,1,90,.1)}.selection-card small{display:block;color:var(--muted);margin-top:3px}.tag{display:inline-block;border-radius:999px;padding:5px 9px;color:#fff;font-size:11px;font-weight:750}.tag-vendor{background:#a64b00}.tag-visitor{background:var(--danger)}.reception-actions{display:block}.reception-queue{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px;margin-bottom:18px}.reception-person{display:flex;align-items:center;justify-content:space-between;gap:12px;border:1px solid #cfd5df;border-radius:8px;background:#fff;padding:12px;text-align:left}.reception-person small{display:block;color:var(--muted);margin-top:4px}.reception-person.active{border-color:var(--brand);box-shadow:0 0 0 2px rgba(4,1,90,.12);background:#f7f6ff}.reception-controls{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px;margin-bottom:14px}.modal{max-height:min(720px,calc(100vh - 32px));overflow-y:auto;border-radius:12px}.toast{border-radius:8px}.mobile-reception{display:none}
.content{padding:24px 24px 40px}.page{max-width:1280px}.page-header{margin-bottom:18px}.page-header h1{font-size:32px}.summary-grid{gap:10px;margin-bottom:16px}.summary-card{padding:13px}.summary-card p{min-height:auto}.summary-card strong{font-size:27px}.panel,.info-card{padding:16px;margin-bottom:16px}.form-section,.schedule-card{padding:18px;margin-bottom:16px}.privacy-notice{border:1px solid #b8c5d8;border-radius:8px;background:#f4f7fb;color:var(--muted);padding:10px 12px;font-size:12px;line-height:1.45;margin-bottom:16px}.filter-grid{display:grid;grid-template-columns:repeat(4,minmax(150px,1fr));gap:10px;margin-bottom:14px}.filter-grid .field input,.filter-grid .field select{margin-top:5px}.compact-table td,.compact-table th{padding:9px}.details-card summary{display:flex;align-items:center;justify-content:space-between;gap:12px;list-style:none;cursor:pointer}.details-card summary::-webkit-details-marker{display:none}.details-card summary:after{content:'Show';color:var(--brand);font-size:11px;font-weight:700}.details-card[open] summary:after{content:'Hide'}.details-card[open] summary{border-bottom:1px solid var(--silver);padding-bottom:12px}.name-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px;grid-column:1/-1}.verification-checklist{display:grid;gap:10px;margin:14px 0}.verification-check{display:grid;grid-template-columns:auto minmax(0,1fr);gap:10px;align-items:start;border:1px solid var(--silver);border-radius:8px;padding:12px}.verification-check input[type="checkbox"]{width:19px;height:19px}.asset-serial-check{display:grid;grid-template-columns:minmax(0,1fr) minmax(180px,.7fr);gap:10px;align-items:end;border:1px solid var(--silver);border-radius:8px;padding:12px}.analytics-results{margin-top:14px}.inline-meta{display:flex;flex-wrap:wrap;gap:8px 16px;color:var(--muted);font-size:12px}.file-box{grid-column:1/-1;border:1px dashed #9aa7ba;border-radius:8px;background:#fafbfc;padding:14px}.file-box .field{margin-top:10px}
.sidebar{transition:width .2s ease,flex-basis .2s ease}.sidebar-heading{justify-content:flex-end;padding:0 18px}.sidebar-toggle{display:grid;place-items:center;width:40px;height:40px;border:1px solid rgba(255,255,255,.35);border-radius:8px;background:transparent;color:#fff}.hamburger{display:grid;gap:4px;width:18px}.hamburger span{display:block;height:2px;border-radius:2px;background:currentColor}.sidebar-nav{padding:14px 0}.nav-link{gap:12px}.nav-icon{width:24px;flex:0 0 24px;text-align:center;font-size:15px;font-weight:800}.nav-text{white-space:nowrap}.sidebar-privacy{margin:14px;padding:12px;border-top:1px solid rgba(255,255,255,.24);color:#fff;font-size:10px;line-height:1.45}.shell.sidebar-collapsed .sidebar{width:72px;flex-basis:72px}.shell.sidebar-collapsed .sidebar-heading{justify-content:center;padding:0}.shell.sidebar-collapsed .nav-link{justify-content:center;padding:12px 22px}.shell.sidebar-collapsed .nav-text,.shell.sidebar-collapsed .sidebar-privacy{display:none}.request-list{display:grid;gap:8px}.request-row{border:1px solid var(--silver);border-radius:8px;background:#fff;overflow:hidden}.request-row>summary{display:grid;grid-template-columns:minmax(180px,1.2fr) minmax(110px,.55fr) minmax(165px,.85fr) minmax(160px,.9fr) minmax(135px,.65fr) auto;gap:12px;align-items:center;padding:12px;list-style:none}.request-row>summary::-webkit-details-marker{display:none}.request-row>summary:hover{background:#fafafa}.request-row[open]>summary{border-bottom:1px solid var(--silver);background:#fafbff}.request-summary-label{display:none;color:var(--muted);font-size:10px}.request-expand{color:var(--brand);font-size:12px;font-weight:700}.visitor-row-wrap{padding:8px 12px 12px}.visitor-row-table{min-width:820px}.audit-list{display:grid;gap:8px;margin-top:14px}.audit-event{display:grid;grid-template-columns:150px minmax(0,1fr) auto;gap:14px;align-items:start;border-left:3px solid var(--brand);border-radius:0 7px 7px 0;background:#f8f9fc;padding:11px 12px}.audit-event p{margin:0}.audit-category{font-size:11px;font-weight:750;color:var(--brand)}.audit-time{white-space:nowrap;color:var(--muted);font-size:11px}.serial-value{display:block;margin-top:4px;color:var(--ink);font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;font-size:13px;font-weight:750}.date-range-control{display:grid;grid-template-columns:minmax(180px,1fr) minmax(180px,1fr);gap:12px;margin-bottom:14px}
@media(max-width:1000px){.summary-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.search{width:240px}.info-grid{grid-template-columns:1fr}.date-row,.asset-row{grid-template-columns:1fr 1fr}.date-row button,.asset-row button{align-self:end}.schedule-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.filter-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.request-row>summary{grid-template-columns:repeat(2,minmax(0,1fr))}.request-summary-label{display:block}.request-expand{grid-column:1/-1}}
@media(max-width:760px){.sidebar{display:none}.mobile-menu{display:block;position:fixed;left:16px;top:16px;z-index:60}.mobile-menu summary{list-style:none;border:1px solid var(--brand-dark);border-radius:7px;background:var(--brand);color:#fff;padding:10px 12px;font-size:12px;font-weight:700;box-shadow:0 4px 14px rgba(20,32,48,.12)}.mobile-menu summary::-webkit-details-marker{display:none}.mobile-nav-panel{width:min(272px,calc(100vw - 32px));margin-top:6px;border-radius:8px;background:var(--brand);padding:10px 0;box-shadow:0 8px 20px rgba(20,32,48,.2)}.topbar{height:68px;padding:0 16px 0 76px}.top-actions{margin-left:auto}.top-actions .small{display:none}.search{display:none}.content{padding:24px 16px 44px}.page-header{align-items:flex-start;flex-direction:column}.page-header h1{font-size:30px}.form-section{grid-template-columns:1fr;padding:18px}.date-row,.asset-row{grid-template-columns:1fr}.split{grid-template-columns:1fr}.classification-grid,.selection-grid,.reception-queue,.reception-controls,.verification-grid,.badge-panel,.completion-checks{grid-template-columns:1fr}.desktop-only,.reception-desktop{display:none}.mobile-records,.mobile-reception{display:block}.mobile-record,.mobile-reception-card{border:1px solid var(--silver);border-radius:9px;background:#fff;padding:16px;margin-bottom:12px}.mobile-record dl,.mobile-reception-card dl{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.mobile-record dt,.mobile-reception-card dt{font-size:10px;color:var(--muted);text-transform:none}.mobile-record dd,.mobile-reception-card dd{margin:3px 0 0;font-size:13px;overflow-wrap:anywhere}.schedule-heading{flex-direction:column}.schedule-grid{grid-template-columns:1fr}.phone-fields{grid-template-columns:1fr}.checkbox-grid{grid-template-columns:1fr}.form-record{align-items:flex-start;flex-direction:column}.form-record>span:last-child{display:flex;flex-wrap:wrap;gap:8px}.modal{padding:20px}.selection-card{grid-template-columns:auto minmax(0,1fr) auto}.selection-card .tag{grid-column:2/-1;justify-self:start}}
@media(max-width:760px){.content{padding:18px 12px 36px}.page-header h1{font-size:28px}.form-section{padding:14px}.filter-grid,.name-grid,.asset-serial-check,.date-range-control{grid-template-columns:1fr}.panel,.info-card{padding:14px}.request-row>summary{grid-template-columns:1fr}.request-expand{grid-column:auto}.audit-event{grid-template-columns:1fr}.audit-time{white-space:normal}}
@media(max-width:480px){.summary-grid{grid-template-columns:1fr 1fr;gap:10px}.summary-card{padding:13px}.summary-card strong{font-size:27px}.top-actions{gap:8px}.status-banner{align-items:flex-start;flex-direction:column}.button-row>*{width:100%}.login-page{padding:20px}.login-body{padding:24px}.mobile-record dl,.mobile-reception-card dl{grid-template-columns:1fr}.page-header h1{font-size:27px}}
.shell.sidebar-collapsed .mobile-menu .nav-text{display:inline}.shell.sidebar-collapsed .mobile-menu .sidebar-privacy{display:block}.shell.sidebar-collapsed .mobile-menu .nav-link{justify-content:flex-start;padding:10px 20px}
.app-shell{min-height:100vh;display:flex;flex-direction:column}.site-header{position:sticky;top:0;z-index:50;background:var(--brand);color:#fff;box-shadow:0 2px 10px rgba(20,32,48,.18)}.header-main{width:100%;max-width:1440px;margin:0 auto;display:grid;grid-template-columns:auto minmax(0,1fr) auto;gap:24px;align-items:center;padding:10px 24px}.brand{display:flex;align-items:center;min-width:max-content}.brand-logo{display:block;width:38px;height:46px;object-fit:contain}.horizontal-nav{display:flex;align-items:stretch;gap:2px;min-width:0;overflow-x:auto}.horizontal-nav .nav-link{flex:0 0 auto;border:0;border-bottom:3px solid transparent;padding:14px 13px;color:#fff;font-weight:650;white-space:nowrap}.horizontal-nav .nav-link:hover{background:rgba(255,255,255,.09);color:#fff}.horizontal-nav .nav-link.active{border-bottom-color:#fff;background:rgba(255,255,255,.14);color:#fff}.header-actions{display:flex;align-items:center;gap:10px}.header-actions .search{width:260px;border-color:rgba(255,255,255,.45);background:#fff}.header-actions .secondary-button{border-color:#fff;color:var(--action)}.app-main{flex:1}.site-footer{margin-top:auto;background:var(--brand);color:#fff}.footer-inner{width:100%;max-width:1440px;margin:0 auto;display:flex;align-items:center;justify-content:space-between;gap:20px;padding:14px 24px;font-size:11px;line-height:1.45}.footer-privacy{max-width:760px;text-align:right;color:#fff}.login-shell{min-height:100vh;display:flex;flex-direction:column}.login-brand-bar{background:var(--brand);color:#fff;padding:10px 24px}.login-brand-bar .brand{width:max-content}.login-page{flex:1;min-height:0}.request-overview{width:100%;margin-bottom:16px}.request-field-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px 20px;margin-top:14px}.request-field-grid p{margin:0;padding:8px 0;border-bottom:1px solid #edf0f4}.visitor-details-widget{margin-bottom:16px}.visitor-detail-list{display:grid;gap:8px;margin-top:12px}.visitor-detail-row{border:1px solid var(--silver);border-radius:8px;background:#fff;overflow:hidden}.visitor-detail-row>summary{display:grid;grid-template-columns:minmax(180px,1.3fr) minmax(180px,1.2fr) minmax(110px,.7fr) minmax(115px,.7fr) minmax(70px,.35fr) auto;gap:12px;align-items:center;padding:12px;list-style:none}.visitor-detail-row>summary::-webkit-details-marker{display:none}.visitor-detail-row>summary:hover{background:#fafbff}.visitor-detail-row[open]>summary{border-bottom:1px solid var(--silver);background:#f7f8fc}.visitor-detail-row .row-label{display:none;color:var(--muted);font-size:10px}.visitor-detail-toggle{white-space:nowrap;color:var(--brand);font-size:12px;font-weight:750}.visitor-detail-row[open] .visitor-detail-toggle:after{content:'Less'}.visitor-detail-row:not([open]) .visitor-detail-toggle:after{content:'View Details'}.visitor-detail-body{padding:14px}.visitor-information-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px 18px}.visitor-information-grid p{margin:0;padding:7px 0;border-bottom:1px solid #edf0f4}.selection-grid{grid-template-columns:1fr}.nowrap{white-space:nowrap}
.request-list-index,.visitor-list-index{display:grid;gap:12px;margin-top:12px;padding:0 12px 8px;color:var(--muted);font-size:10px;font-weight:750;letter-spacing:.05em;text-transform:uppercase}.request-list-index{grid-template-columns:minmax(180px,1.2fr) minmax(110px,.55fr) minmax(165px,.85fr) minmax(160px,.9fr) minmax(135px,.65fr) auto}.visitor-list-index{grid-template-columns:minmax(180px,1.3fr) minmax(180px,1.2fr) minmax(110px,.7fr) minmax(115px,.7fr) minmax(70px,.35fr) auto}.table-section-heading{margin:18px 0 10px;color:var(--title);font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;font-size:18px}
@media(max-width:1100px){.header-main{grid-template-columns:auto 1fr}.horizontal-nav{grid-column:1/-1;grid-row:2}.header-actions{justify-self:end}.request-field-grid{grid-template-columns:repeat(3,minmax(0,1fr))}.visitor-detail-row>summary{grid-template-columns:repeat(3,minmax(0,1fr))}.visitor-detail-row .row-label{display:block}.visitor-detail-toggle{justify-self:start}.request-list-index,.visitor-list-index{display:none}}
@media(max-width:760px){.header-main{grid-template-columns:1fr auto;gap:10px;padding:8px 12px}.brand-logo{width:30px;height:38px}.header-actions .search,.header-actions .small{display:none}.horizontal-nav{grid-column:1/-1;padding-bottom:2px}.horizontal-nav .nav-link{padding:11px 10px}.content{padding-top:20px}.footer-inner{align-items:flex-start;flex-direction:column}.footer-privacy{text-align:left}.request-field-grid,.visitor-information-grid,.visitor-detail-row>summary{grid-template-columns:1fr}.visitor-detail-toggle{justify-self:start}}
@media(prefers-reduced-motion:reduce){*{scroll-behavior:auto!important;transition:none!important}}
.sr-only{position:absolute!important;width:1px!important;height:1px!important;padding:0!important;margin:-1px!important;overflow:hidden!important;clip:rect(0,0,0,0)!important;white-space:nowrap!important;border:0!important}.modal-open{overflow:hidden}.breadcrumb{margin:0 0 12px}.breadcrumb ol{display:flex;flex-wrap:wrap;align-items:center;gap:7px;margin:0;padding:0;list-style:none;color:var(--muted);font-size:12px}.breadcrumb li{display:flex;align-items:center;gap:7px}.breadcrumb li:not(:last-child):after{content:'›';color:#9aa3b2}.breadcrumb a{color:var(--brand);font-weight:700}.status-chip{position:relative;display:inline-flex;align-items:center;gap:6px;border:1px solid transparent}.status-chip:before{content:'';width:7px;height:7px;flex:0 0 7px;border-radius:50%;background:currentColor}.status-success{border-color:var(--success);background:#F3F6E6;color:var(--title)}.status-live:before,.status-success:before{background:var(--success)}.status-danger{border-color:var(--danger);background:#FFF0F0;color:var(--danger)}.status-warning{border-color:#ead08a;background:#fff8df;color:#7a5a00}.status-information{border-color:#b9c9e2;background:#eef4fc;color:#224d86}.status-neutral{border-color:#d5d9df;background:#f5f6f8;color:var(--muted)}.tooltip-popup{position:fixed;z-index:200;width:max-content;max-width:min(300px,calc(100vw - 24px));border-radius:6px;background:#151a23;color:#fff;padding:7px 9px;box-shadow:0 6px 20px rgba(0,0,0,.25);font-size:11px;line-height:1.4;pointer-events:none}.filter-summary{display:flex;align-items:center;justify-content:space-between;gap:14px;border:1px solid #ccd5e2;border-radius:9px;background:#f8faff;padding:12px 14px;margin-bottom:16px}.filter-chipset{display:flex;flex-wrap:wrap;align-items:center;gap:7px}.filter-chip{display:inline-flex;align-items:center;gap:7px;min-height:30px;border:1px solid #9daecc;border-radius:999px;background:#fff;color:var(--brand);padding:5px 10px;font-size:11px;font-weight:750}.filter-chip button{font:inherit}.filter-chip span{font-size:16px;line-height:1}.filter-clear{color:var(--brand);font-size:12px;font-weight:700}.analytics-filter-chips{min-height:30px;margin:12px 0}.empty-state{display:grid;justify-items:center;max-width:560px;margin:24px auto;padding:28px;text-align:center}.empty-state-icon{display:grid;place-items:center;width:52px;height:52px;border:2px solid #9aa7ba;border-radius:50%;color:var(--brand);font-size:29px}.empty-state h2{margin:14px 0 6px;color:var(--title);font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;font-size:20px}.empty-state p{margin:0;color:var(--muted);font-size:13px;line-height:1.5}.error-state{display:grid;grid-template-columns:auto minmax(0,640px);justify-content:center;align-items:center;gap:24px;min-height:52vh;padding:40px 20px}.error-state-code{display:grid;place-items:center;width:72px;height:72px;border-radius:50%;background:#fff0f1;color:var(--danger);font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;font-size:44px;font-weight:800}.error-state h1{margin:0;color:var(--title)}.error-state p{margin:10px 0 22px;color:var(--muted);line-height:1.55}.skeleton-state{padding:4px 0}.skeleton{border-radius:8px;background:linear-gradient(90deg,#e8ebef 25%,#f6f7f9 50%,#e8ebef 75%);background-size:200% 100%;animation:skeleton-shimmer 1.25s ease-in-out infinite}.skeleton-title{width:min(360px,70%);height:38px;margin-bottom:22px}.skeleton-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px}.skeleton-card{height:92px}.skeleton-panel{height:260px;margin-top:18px}@keyframes skeleton-shimmer{from{background-position:200% 0}to{background-position:-200% 0}}.workflow-progress{border:1px solid var(--silver);border-radius:10px;background:#fff;padding:16px 18px;margin-bottom:16px}.workflow-progress-heading{display:flex;align-items:center;justify-content:space-between;gap:12px}.workflow-progress-heading h2{margin:0;color:var(--title);font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',Arial,sans-serif;font-size:18px}.workflow-progress ol{display:grid;grid-template-columns:repeat(6,minmax(0,1fr));margin:18px 0 0;padding:0;list-style:none}.progress-step{position:relative;display:grid;justify-items:center;gap:7px;color:var(--muted);font-size:11px;font-weight:700;text-align:center}.progress-step:not(:last-child):after{content:'';position:absolute;z-index:0;top:8px;left:calc(50% + 10px);width:calc(100% - 20px);height:2px;background:#d7dce4}.progress-marker{position:relative;z-index:1;width:18px;height:18px;border:2px solid #b7c0cc;border-radius:50%;background:#fff}.progress-complete{color:var(--title)}.progress-complete .progress-marker{border-color:var(--success);background:var(--success)}.progress-complete .progress-marker:after{content:'✓';position:absolute;inset:-3px;color:var(--title);font-size:12px}.progress-complete:not(:last-child):after{background:var(--success)}.progress-active{color:var(--brand)}.progress-active .progress-marker{border:5px solid var(--brand)}.progress-error{color:var(--danger)}.progress-error .progress-marker{border-color:var(--danger);background:var(--danger)}.progress-error .progress-marker:after{content:'×';position:absolute;inset:-4px;color:#fff;font-size:14px}.profile-menu{position:relative}.profile-menu summary{list-style:none}.profile-menu summary::-webkit-details-marker{display:none}.profile-summary{display:flex;align-items:center;gap:9px;border:1px solid rgba(255,255,255,.35);border-radius:999px;padding:4px 10px 4px 5px;cursor:pointer}.avatar{display:grid;place-items:center;width:34px;height:34px;flex:0 0 34px;border-radius:50%;background:#fff;color:var(--brand);font-size:11px;font-weight:800}.profile-copy{display:grid;line-height:1.2}.profile-copy strong{font-size:12px}.profile-copy small,.profile-panel-header small{color:#fff;font-size:10px}.profile-panel{position:absolute;right:0;top:calc(100% + 8px);z-index:90;width:240px;border:1px solid var(--silver);border-radius:9px;background:#fff;color:var(--ink);box-shadow:0 12px 34px rgba(0,0,0,.22);padding:10px}.profile-panel-header{display:flex;align-items:center;gap:10px;border-bottom:1px solid var(--silver);padding:7px 7px 12px}.profile-panel-header span:last-child{display:grid;gap:3px}.profile-panel-header small{color:var(--muted)}.profile-action{width:100%;border:0;border-radius:6px;background:#fff;color:var(--brand);padding:10px;text-align:left;font-weight:700}.profile-action:hover{background:#f4f3ff}.mobile-navigation{display:none;position:relative}.mobile-navigation summary{display:flex;align-items:center;gap:7px;border:1px solid rgba(255,255,255,.35);border-radius:7px;padding:9px;color:#fff;font-size:12px;font-weight:700;list-style:none}.mobile-navigation summary::-webkit-details-marker{display:none}.menu-lines{display:grid;gap:3px;width:15px}.menu-lines span{height:2px;border-radius:2px;background:#fff}.mobile-navigation-panel{position:absolute;top:calc(100% + 8px);left:0;z-index:90;width:min(270px,calc(100vw - 24px));border-radius:9px;background:var(--brand);box-shadow:0 12px 32px rgba(0,0,0,.24);padding:8px}.mobile-navigation-panel .nav-link{border-left:0;border-radius:6px}.footer-top{white-space:nowrap;color:#fff;font-weight:700}.upload-dropzone{display:flex;align-items:center;gap:12px;border:1px dashed #8292aa;border-radius:8px;background:#fff;padding:14px;margin:12px 0;cursor:pointer}.upload-dropzone:hover{border-color:var(--brand);background:#f8f7ff}.upload-dropzone input{max-width:100%;margin-left:auto}.upload-dropzone small{display:block;margin-top:3px;color:var(--muted);font-weight:400}.upload-icon{display:grid;place-items:center;width:36px;height:36px;flex:0 0 36px;border-radius:50%;background:#eceaff;color:var(--brand);font-size:20px;font-weight:800}.upload-progress{height:7px;overflow:hidden;border-radius:999px;background:#e4e8ee;margin:10px 0}.upload-progress span{display:block;width:45%;height:100%;border-radius:inherit;background:var(--brand);animation:upload-progress 1s ease-in-out infinite alternate}@keyframes upload-progress{from{transform:translateX(-20%)}to{transform:translateX(145%)}}
@media(max-width:900px){.profile-copy{display:none}.header-actions .search{width:210px}.skeleton-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}
@media(max-width:760px){.header-main{grid-template-columns:auto minmax(0,1fr) auto}.horizontal-nav{display:none}.mobile-navigation{display:block;justify-self:start}.header-actions{justify-self:end}.workflow-progress ol{grid-template-columns:1fr;gap:0;margin-top:14px}.progress-step{grid-template-columns:22px 1fr;justify-items:start;align-items:center;min-height:38px;text-align:left}.progress-step:not(:last-child):after{top:20px;left:8px;width:2px;height:calc(100% - 2px)}.progress-marker{grid-column:1}.progress-step>span:last-child{grid-column:2}.filter-summary{align-items:flex-start;flex-direction:column}.error-state{grid-template-columns:1fr;justify-items:center;text-align:center}.skeleton-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.footer-top{margin-top:2px}}
@media(max-width:480px){.skeleton-grid{grid-template-columns:1fr 1fr}.skeleton-card{height:76px}.profile-summary{padding-right:5px}.filter-chipset{width:100%}.upload-dropzone{align-items:flex-start;flex-wrap:wrap}.upload-dropzone input{width:100%;margin-left:0}}
@media(prefers-reduced-motion:reduce){.skeleton,.upload-progress span{animation:none}}
</style>
</head>
<body>
<div id="app" class="app"></div>
<div id="modal-root"></div>
<div id="toast-root" aria-live="polite" aria-atomic="true"></div>
<div id="tooltip-root" class="tooltip-popup" role="tooltip" hidden></div>
<script>
const USERS = __USERS__
const BRAND_LOGO='__BRAND_LOGO__'
const STATUS_LABELS={DRAFT:'Draft',VISITOR_FORM_PENDING:'Visitor Details Pending',VISITOR_FORM_SUBMITTED:'Visitor Details Saved',REVISION_REQUIRED:'Revision Required',HOST_REVIEW:'Host Review',PENDING_EC_REVIEW:'Pending Export Control Review',EC_REVIEW:'Export Control Review',PENDING_DOCUMENTATION:'Pending Information',DOCUMENTATION_SUBMITTED:'Information Submitted',EC_RE_REVIEW_REQUIRED:'Export Control Re-Review Required',APPROVED:'Approved',PARTIALLY_APPROVED:'Partially Approved',REJECTED:'Rejected',CANCELLED:'Cancelled',VERIFICATION_IN_PROGRESS:'Verification in Progress',RECEPTION_VERIFICATION:'Ready for Check-In',CHECKED_IN:'Checked In',COMPLETED:'Checked Out',NO_SHOW:'No-Show',ENTRY_REJECTED:'Entry Rejected',VISIT_PROCESS_COMPLETED:'Visit Completed',UPCOMING:'Expected',SUBMITTED:'Saved',PENDING:'Pending',NOT_APPLICABLE:'Not Applicable',RESOLVED:'Resolved',NotVerified:'Not Verified',Verified:'Verified',Rejected:'Rejected'}
const STATUS_HELP={DRAFT:'Not started or still being prepared.',VISITOR_FORM_PENDING:'One or more visitor forms still need to be saved.',VISITOR_FORM_SUBMITTED:'All visitor details are saved and ready for host review.',REVISION_REQUIRED:'Export Control requested changes to this visitor record.',HOST_REVIEW:'The host is reviewing the completed visitor information.',PENDING_EC_REVIEW:'Waiting for Export Control screening.',PENDING_DOCUMENTATION:'Additional visitor information has been requested.',DOCUMENTATION_SUBMITTED:'Requested information was submitted for review.',EC_RE_REVIEW_REQUIRED:'Updated information requires another Export Control review.',APPROVED:'All visitors in this request are approved.',PARTIALLY_APPROVED:'At least one visitor is approved and at least one is rejected.',REJECTED:'Export Control rejected this request or visitor.',CANCELLED:'The host cancelled this request.',VERIFICATION_IN_PROGRESS:'Security verification has started.',RECEPTION_VERIFICATION:'Identity and declared assets are verified for check-in.',CHECKED_IN:'The visitor is currently inside the site.',COMPLETED:'The visitor checked out and returned the badge.',NO_SHOW:'The visitor did not arrive for the scheduled visit.',ENTRY_REJECTED:'Security rejected entry during verification.',VISIT_PROCESS_COMPLETED:'Every scheduled visitor record has reached a final outcome.',UPCOMING:'The visit is scheduled but reception processing has not started.',SUBMITTED:'The visitor information has been saved.',PENDING:'A decision or action is still required.',NOT_APPLICABLE:'This check does not apply to the record.',RESOLVED:'The requested update has been completed.',NotVerified:'The declared asset has not been verified.',Verified:'The declared asset was verified.',Rejected:'The declared asset was rejected.'}
const STATUS_TONES={APPROVED:'success',VISIT_PROCESS_COMPLETED:'success',COMPLETED:'success',CHECKED_IN:'success',SUBMITTED:'success',RESOLVED:'success',Verified:'success',REJECTED:'danger',CANCELLED:'danger',ENTRY_REJECTED:'danger',Rejected:'danger',NO_SHOW:'warning',PARTIALLY_APPROVED:'warning',PENDING_DOCUMENTATION:'warning',REVISION_REQUIRED:'warning',EC_RE_REVIEW_REQUIRED:'warning',PENDING_EC_REVIEW:'information',DOCUMENTATION_SUBMITTED:'information',EC_REVIEW:'information',VERIFICATION_IN_PROGRESS:'information',RECEPTION_VERIFICATION:'information',VISITOR_FORM_SUBMITTED:'information',VISITOR_FORM_PENDING:'information',UPCOMING:'information',DRAFT:'neutral',PENDING:'neutral',NOT_APPLICABLE:'neutral',NotVerified:'neutral'}
const COUNTRIES=__COUNTRIES__
const REQUEST_FIELD_LABELS={firstName:'First Name',middleName:'Middle Name',lastName:'Last Name',designation:'Designation/Position Held',citizenship:'Citizenship',companyName:'Full Name of Visitor Company',companyAddress:'Company Address',officeCity:'Company City',officeCountry:'Company Country',phoneCountry:'Phone Country Code',telephone:'Phone Number',email:'Email Address',idType:'ID Type',otherIdType:'Government-Issued ID Type',assets:'Declared Assets'}
const app=document.getElementById('app')
const modalRoot=document.getElementById('modal-root')
const toastRoot=document.getElementById('toast-root')
const tooltipRoot=document.getElementById('tooltip-root')
let user=USERS.find(item=>item.id===localStorage.getItem('visitor.session'))||null
let viewToken=0
let visitorDraft=null
let modalReturnFocus=null

const h=value=>String(value??'').replace(/[&<>'"]/g,char=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[char]))
const label=value=>STATUS_LABELS[value]||String(value||'').replaceAll('_',' ').toLowerCase().replace(/\b\w/g,char=>char.toUpperCase())
const fmtDate=value=>value?new Date(value.length===10?value+'T00:00:00':value).toLocaleDateString(undefined,{year:'numeric',month:'short',day:'numeric'}):'Not scheduled'
const fmtTime=value=>value?new Date(value).toLocaleString():'Pending'
const fmtIst=value=>value?new Date(value).toLocaleString(undefined,{timeZone:'Asia/Kolkata',year:'numeric',month:'short',day:'numeric',hour:'2-digit',minute:'2-digit'})+' IST':'Not scheduled'
const fmtDateRange=(start,end)=>!start?'Not Scheduled':!end||start===end?fmtDate(start):fmtDate(start)+' – '+fmtDate(end)
const fmtBytes=value=>value>=1048576?(value/1048576).toFixed(1)+' MB':Math.max(1,Math.round(value/1024))+' KB'
const chip=value=>{const tone=STATUS_TONES[value]||'neutral',help=STATUS_HELP[value]||label(value);return'<span class="status-chip status-'+tone+'" data-tooltip="'+h(help)+'" aria-label="'+h(label(value)+': '+help)+'">'+h(label(value))+'</span>'}
function breadcrumbMarkup(){const path=location.pathname,items=[['Dashboard','/dashboard']];if(path.startsWith('/visitor-requests')||path.startsWith('/visitor-forms'))items.push(['Visitor Requests','/visitor-requests']);if(path==='/visitor-requests/new')items.push(['Create Request','']);else if(/^\/visitor-requests\/[^/]+$/.test(path))items.push(['Request Details','']);else if(path.startsWith('/visitor-forms/'))items.push(['Visitor Information','']);else if(path==='/visitor-history')items.push(['Visitor History','']);else if(path==='/pending-actions')items.push(['Pending Actions','']);else if(path==='/reports')items.push(['Reports','']);if(items.length===1&&path==='/dashboard')return'';return'<nav class="breadcrumb" aria-label="Breadcrumb"><ol>'+items.map((item,index)=>'<li>'+(item[1]&&index<items.length-1?'<a href="'+item[1]+'" data-route>'+h(item[0])+'</a>':'<span aria-current="page">'+h(item[0])+'</span>')+'</li>').join('')+'</ol></nav>'}
const pageHeader=(eyebrow,title,subtitle='',actions='')=>breadcrumbMarkup()+'<header class="page-header"><div>'+(eyebrow?'<p class="eyebrow">'+h(eyebrow)+'</p>':'')+'<h1 class="display">'+h(title)+'</h1>'+(subtitle?'<p class="muted small">'+h(subtitle)+'</p>':'')+'</div>'+actions+'</header>'
const errorBox=message=>'<div class="error-box" role="status"><span>'+h(message)+'</span><button class="text-button" data-render>Retry</button></div>'
const requiredMark=required=>required?'<span class="required-mark" aria-hidden="true"> *</span>':''
const field=(name,title,value='',type='text',required=false,extraClass='')=>'<label class="field '+h(extraClass)+'">'+h(title)+requiredMark(required)+'<input name="'+h(name)+'" type="'+h(type)+'" value="'+h(value)+'" '+(required?'required aria-required="true"':'')+'></label>'
const textareaField=(name,title,value='',required=false,extraClass='')=>'<label class="field '+h(extraClass)+'">'+h(title)+requiredMark(required)+'<textarea name="'+h(name)+'" rows="4" '+(required?'required aria-required="true"':'')+'>'+h(value)+'</textarea></label>'
const selectField=(name,title,value,options,required=false,extraClass='')=>'<label class="field '+h(extraClass)+'">'+h(title)+requiredMark(required)+'<select name="'+h(name)+'" '+(required?'required aria-required="true"':'')+'>'+options.map(option=>'<option value="'+h(option.value??option)+'" '+((option.value??option)===value?'selected':'')+'>'+h(option.label??option)+'</option>').join('')+'</select></label>'
const infoField=(title,value,strong=false)=>'<p><strong>'+h(title)+':</strong> <span '+(strong?'class="link"':'')+'>'+h(value||'N/A')+'</span></p>'
const recordList=(items,render,empty)=>items&&items.length?items.map(render).join(''):'<p class="muted small">'+h(empty)+'</p>'
const privacyNotice=()=>'<aside class="privacy-notice" role="note"><strong>Privacy notice:</strong> Use visitor information only for authorized screening, access, safety, and audit purposes. Do not copy or disclose it outside the approved workflow.</aside>'
const brandMarkup=(linked=true)=>(linked?'<a href="/dashboard" data-route class="brand" aria-label="Rolls-Royce home">':'<div class="brand">')+'<img class="brand-logo" src="'+BRAND_LOGO+'" alt="Rolls-Royce">'+(linked?'</a>':'</div>')
const footerMarkup=()=>'<footer class="site-footer"><div class="footer-inner"><span>© '+new Date().getFullYear()+' Rolls-Royce plc</span><span class="footer-privacy">Visitor information is available only to authorized personnel for approved visitor-processing purposes.</span><a class="footer-top" href="#app">Back to top <span aria-hidden="true">↑</span></a></div></footer>'
const initials=value=>String(value||'').split(/[\s/]+/).filter(Boolean).slice(0,2).map(item=>item[0]).join('').toUpperCase()
const emptyState=(title,message,action='')=>'<div class="empty-state"><span class="empty-state-icon" aria-hidden="true">◇</span><h2>'+h(title)+'</h2><p>'+h(message)+'</p>'+action+'</div>'
const errorState=(title,message)=>'<section class="error-state" role="alert"><span class="error-state-code" aria-hidden="true">!</span><div><h1 class="display">'+h(title)+'</h1><p>'+h(message)+'</p><a href="/dashboard" data-route class="primary-button">Return to Dashboard</a></div></section>'
const loadingState=labelText=>'<div class="skeleton-state" role="status" aria-live="polite" aria-busy="true"><span class="sr-only">'+h(labelText)+'</span><div class="skeleton skeleton-title"></div><div class="skeleton-grid"><div class="skeleton skeleton-card"></div><div class="skeleton skeleton-card"></div><div class="skeleton skeleton-card"></div><div class="skeleton skeleton-card"></div></div><div class="skeleton skeleton-panel"></div></div>'
const filterChip=(text,name='')=>name?'<button type="button" class="filter-chip" data-clear-filter="'+h(name)+'">'+h(text)+'<span aria-hidden="true">×</span></button>':'<span class="filter-chip">'+h(text)+'</span>'

async function api(path,options={}){
  const headers={...(options.body?{'Content-Type':'application/json'}:{}),...(options.headers||{})}
  if(user)headers['X-Visitor-Role']=user.id
  const response=await fetch(path,{...options,headers})
  if(response.status===401&&user){user=null;localStorage.removeItem('visitor.session');go('/login');throw new Error('Your session ended. Please sign in again.')}
  const contentType=response.headers.get('content-type')||''
  const data=contentType.includes('application/json')?await response.json():await response.text()
  if(!response.ok)throw new Error(data.error||data||'The request could not be completed.')
  return data
}

function go(path){history.pushState({},'',path);render().then(()=>document.getElementById('main-content')?.focus())}
function toast(message,type=''){toastRoot.innerHTML='<div class="toast '+h(type)+'">'+h(message)+'</div>';setTimeout(()=>{toastRoot.innerHTML=''},3200)}
function showTooltip(target){const message=target.dataset.tooltip;if(!message)return;tooltipRoot.textContent=message;tooltipRoot.hidden=false;const targetBox=target.getBoundingClientRect(),tooltipBox=tooltipRoot.getBoundingClientRect(),left=Math.max(12,Math.min(window.innerWidth-tooltipBox.width-12,targetBox.left+(targetBox.width-tooltipBox.width)/2)),above=targetBox.top-tooltipBox.height-8;tooltipRoot.style.left=left+'px';tooltipRoot.style.top=(above>=8?above:targetBox.bottom+8)+'px'}
function hideTooltip(){tooltipRoot.hidden=true;tooltipRoot.textContent=''}
function closeModal(){modalRoot.innerHTML='';modalRoot.onkeydown=null;app.removeAttribute('inert');document.body.classList.remove('modal-open');if(modalReturnFocus?.isConnected)modalReturnFocus.focus();modalReturnFocus=null}
function modal(title,body,submitLabel,onSubmit,tone='primary'){
  modalReturnFocus=document.activeElement
  app.setAttribute('inert','');document.body.classList.add('modal-open')
  modalRoot.innerHTML='<div class="modal-backdrop" role="presentation"><section class="modal" role="dialog" aria-modal="true" aria-labelledby="modal-title"><h2 id="modal-title">'+h(title)+'</h2><div style="margin-top:18px">'+body+'</div><div class="modal-actions"><button class="secondary-button" id="modal-cancel">Cancel</button><button class="'+tone+'-button" id="modal-submit">'+h(submitLabel)+'</button></div></section></div>'
  document.getElementById('modal-cancel').onclick=closeModal
  modalRoot.querySelector('.modal-backdrop').onclick=event=>{if(event.target===event.currentTarget)closeModal()}
  document.getElementById('modal-submit').onclick=async event=>{const button=event.currentTarget;button.disabled=true;try{await onSubmit();closeModal()}catch(reason){button.disabled=false;toast(reason.message,'error')}}
  modalRoot.onkeydown=event=>{if(event.key!=='Tab')return;const controls=[...modalRoot.querySelectorAll('button:not(:disabled),input:not(:disabled),select:not(:disabled),textarea:not(:disabled),a[href]')].filter(item=>item.offsetParent!==null);if(!controls.length)return;const first=controls[0],last=controls[controls.length-1];if(event.shiftKey&&document.activeElement===first){event.preventDefault();last.focus()}else if(!event.shiftKey&&document.activeElement===last){event.preventDefault();first.focus()}}
  const first=modalRoot.querySelector('input,textarea,select');(first||document.getElementById('modal-submit')).focus()
}

function navigation(){
  const byRole={
    HOST_REQUESTER:[['Dashboard','/dashboard'],['Visitor Requests','/visitor-requests'],['Visitor History','/visitor-history']],
    EXPORT_CONTROL:[['Dashboard','/dashboard'],['Pending Actions','/pending-actions'],['Visitor History','/visitor-history'],['Reports','/reports']],
    RECEPTION:[['Dashboard','/dashboard'],['Visitor History','/visitor-history'],['Reports','/reports']]
  }
  const path=location.pathname
  return byRole[user.role].map(item=>'<a href="'+item[1]+'" data-route class="nav-link '+(path===item[1]?'active':'')+'" '+(path===item[1]?'aria-current="page"':'')+'>'+h(item[0])+'</a>').join('')
}

function shell(){
  const nav=navigation()
  app.innerHTML='<a class="skip-link" href="#main-content">Skip to Content</a><div class="app-shell"><header class="site-header"><div class="header-main">'+brandMarkup()+'<nav class="horizontal-nav" aria-label="Primary navigation">'+nav+'</nav><details class="mobile-navigation"><summary aria-label="Open navigation"><span class="menu-lines" aria-hidden="true"><span></span><span></span><span></span></span>Menu</summary><nav class="mobile-navigation-panel" aria-label="Mobile navigation">'+nav+'</nav></details><div class="header-actions"><input class="search" id="global-search" placeholder="Search visitors or requests" aria-label="Search visitors or requests"><details class="profile-menu"><summary class="profile-summary" aria-label="Open user menu"><span class="avatar" aria-hidden="true">'+h(initials(user.name))+'</span><span class="profile-copy"><strong>'+h(user.name)+'</strong><small>'+h(label(user.role))+'</small></span></summary><div class="profile-panel"><div class="profile-panel-header"><span class="avatar" aria-hidden="true">'+h(initials(user.name))+'</span><span><strong>'+h(user.name)+'</strong><small>'+h(label(user.role))+'</small></span></div><button class="profile-action" id="logout" type="button">Log Out</button></div></details></div></div></header><main class="content app-main" id="main-content" tabindex="-1"><div id="page" class="page">'+loadingState('Loading visitor records')+'</div></main>'+footerMarkup()+'</div>'
  document.getElementById('logout').onclick=()=>{user=null;localStorage.removeItem('visitor.session');go('/login')}
  document.getElementById('global-search').addEventListener('keydown',event=>{if(event.key==='Enter'&&event.currentTarget.value.trim())go('/visitor-requests?q='+encodeURIComponent(event.currentTarget.value.trim()))})
}

function renderLogin(){
  app.innerHTML='<div class="login-shell"><header class="login-brand-bar">'+brandMarkup(false)+'</header><div class="login-page"><section class="login-card"><div class="login-body"><h1 class="display" style="color:var(--title)">Select Your Role</h1><div class="user-list">'+USERS.map(item=>'<button class="user-choice" data-user="'+h(item.id)+'"><strong>'+h(item.name)+'</strong><span class="tiny link">Select →</span></button>').join('')+'</div></div></section></div>'+footerMarkup()+'</div>'
  app.querySelectorAll('[data-user]').forEach(button=>button.onclick=()=>{user=USERS.find(item=>item.id===button.dataset.user);localStorage.setItem('visitor.session',user.id);go(sessionStorage.getItem('visitor.destination')||'/dashboard');sessionStorage.removeItem('visitor.destination')})
}

function summaryCards(values){return'<section class="summary-grid">'+values.map(item=>{const body='<p>'+h(item[0])+'</p><strong>'+h(item[1])+'</strong>';return item[2]?'<a class="summary-card summary-link" href="'+h(item[2])+'" data-route aria-label="View '+h(item[0])+'">'+body+'</a>':'<article class="summary-card">'+body+'</article>'}).join('')+'</section>'}
function activity(items,title){
  return'<section class="panel"><div class="panel-heading"><h2>'+h(title)+'</h2><a href="/visitor-requests" data-route class="link tiny">View All →</a></div>'+requestTable(items)+'</section>'
}
function requestVisitorGroup(item){
  const names=Array.isArray(item.visitorNames)?item.visitorNames:[],companies=Array.isArray(item.companyNames)?item.companyNames:[]
  const count=Number(item.visitorCount)||names.length||1
  if(count===1){
    const name=names[0]||item.visitorName||'Visitor Details Pending'
    const company=companies[0]||item.companyName||'Company Details Pending'
    return'<div><strong>'+h(name)+'</strong><p class="tiny muted">'+h(company)+'</p></div>'
  }
  const completed=Math.max(0,Math.min(count,Number(item.completedVisitorForms)||0))
  const companyText=companies.length?companies.length+' '+(companies.length===1?'Company':'Companies'):'Company Details Pending'
  return'<div><strong>'+h(count)+'</strong><p class="tiny muted">'+h('Visitors · '+completed+' of '+count+' Forms Saved · '+companyText)+'</p></div>'
}
function requestTable(items){
  if(!items.length)return emptyState('No visitor requests','No records match this view. Create a request or adjust the active filters.')
  const index='<div class="request-list-index" aria-hidden="true"><span>Request</span><span>Visitors</span><span>Visit Range</span><span>Host</span><span>Status</span><span>Details</span></div>'
  return index+'<div class="request-list">'+items.map(item=>{const visitors=Array.isArray(item.visitors)?item.visitors:[];const visitorRows=visitors.map((visitor,index)=>'<tr><td class="strong">'+h(visitor.fullName||'Visitor '+(index+1))+'</td><td>'+h(visitor.companyName||'Pending')+'</td><td>'+h(visitor.citizenship||'Pending')+'</td><td>'+h(visitor.idType||'Pending')+'</td><td>'+chip(visitor.formStatus||'DRAFT')+'</td><td>'+chip(visitor.screeningDecision||'PENDING')+'</td><td>'+h(visitor.assetCount||0)+'</td><td><a href="/visitor-requests/'+h(item.id)+'" data-route class="link">Open</a></td></tr>').join('');return'<details class="request-row"><summary><div><span class="request-summary-label">Request</span><strong class="link">'+h(item.requestNumber)+'</strong></div><div><span class="request-summary-label">Visitors</span>'+requestVisitorGroup(item)+'</div><div><span class="request-summary-label">Visit Range</span><strong>'+h(fmtDateRange(item.visitDate,item.visitEndDate))+'</strong></div><div><span class="request-summary-label">Host</span><strong>'+h(item.hostName||'Unassigned')+'</strong>'+(item.hostDepartment?'<br><span class="tiny muted">'+h(item.hostDepartment)+'</span>':'')+'</div><div><span class="request-summary-label">Status</span>'+chip(item.currentStatus)+'</div><span class="request-expand">View Visitors</span></summary><div class="visitor-row-wrap"><div class="table-wrap"><table class="data-table compact-table visitor-row-table"><thead><tr><th>Visitor</th><th>Full Name of Visitor Company</th><th>Citizenship</th><th>ID Type</th><th>Form Status</th><th>Screening</th><th>Assets</th><th></th></tr></thead><tbody>'+visitorRows+'</tbody></table></div><div class="button-row" style="margin-top:10px"><a href="/visitor-requests/'+h(item.id)+'" data-route class="secondary-button compact-button">Open Request</a></div></div></details>'}).join('')+'</div>'
}

async function dashboardPage(main){
  if(user.role==='RECEPTION'){await receptionPage(main,true);return}
  if(user.role==='EXPORT_CONTROL'){
    const data=await api('/api/ec/dashboard')
    main.innerHTML=pageHeader('','Export Control','','<button class="secondary-button" data-render>Refresh</button>')+summaryCards([['Pending Export Control Review',data.pendingEcReviews,'/visitor-requests?view=pending-ec'],['Pending Documentation',data.pendingDocumentation,'/visitor-requests?view=pending-documentation'],['Approved',data.approved,'/visitor-requests?view=approved'],['Rejected',data.rejected,'/visitor-requests?view=rejected'],['Reception Rejections',data.receptionRejections,'/visitor-requests?view=reception-rejected'],['Visitor History',data.visitorHistory,'/visitor-history']])+activity(data.pendingEcReviewsItems,'Requests Awaiting Export Control Review')
    return
  }
  const data=await api('/api/dashboard')
  main.innerHTML=pageHeader('','Host/Requester','','<div class="button-row"><a href="/visitor-requests/new" data-route class="primary-button">Create Visitor Request</a><button class="secondary-button" data-render>Refresh</button></div>')+summaryCards([['Total Visitors',data.totalRequests,'/visitor-requests'],['Pending Export Control Review',data.pendingEcReviews,'/visitor-requests?view=pending-ec'],['Approved',data.approved,'/visitor-requests?view=approved'],["Today's Visitors",data.todaysVisits,'/visitor-requests?view=today'],['Checked In',data.currentlyInside,'/visitor-requests?view=checked-in'],['Checked Out',data.checkedOut,'/visitor-requests?view=checked-out'],['Pending Documentation',data.pendingDocumentation,'/visitor-requests?view=pending-documentation'],['No-Shows',data.noShows,'/visitor-requests?view=no-show']])+activity(data.recentRequests,'Visitor Activity')+'<div class="split"><a class="panel summary-link" href="/visitor-requests?view=upcoming" data-route><h2>Upcoming Visits</h2><p class="metric-number">'+data.upcomingVisits+'</p></a><a class="panel summary-link" href="/pending-actions" data-route><h2>Requests Awaiting Action</h2><p class="metric-number">'+data.pendingActions+'</p></a></div>'
}

async function requestsPage(main,mode='all'){
  const data=await api('/api/visitor-requests')
  let items=data.items
  let title='Visitor Requests',subtitle='',eyebrow=''
  const parameters=new URLSearchParams(location.search),query=parameters.get('q')?.toLowerCase(),view=parameters.get('view')||''
  if(mode==='history'){items=items.filter(item=>['APPROVED','PARTIALLY_APPROVED','REJECTED','CANCELLED','VISIT_PROCESS_COMPLETED'].includes(item.currentStatus));title='Visitor History'}
  const views={
    'pending-ec':['Pending Export Control Reviews',item=>['PENDING_EC_REVIEW','DOCUMENTATION_SUBMITTED','EC_RE_REVIEW_REQUIRED'].includes(item.currentStatus)],
    'pending-documentation':['Pending Documentation',item=>item.currentStatus==='PENDING_DOCUMENTATION'],
    approved:['Approved Requests',item=>['APPROVED','PARTIALLY_APPROVED'].includes(item.currentStatus)],
    rejected:['Rejected Requests',item=>item.currentStatus==='REJECTED'],
    today:["Today's Visits",item=>item.hasToday],
    'checked-in':['Checked-in Visitors',item=>item.hasCheckedIn],
    'checked-out':['Checked-out Visitors',item=>item.hasCheckedOut],
    'no-show':['No-Shows',item=>item.hasNoShow],
    'reception-rejected':['Reception Rejections',item=>item.hasReceptionRejection],
    upcoming:['Upcoming Visits',item=>item.visitEndDate>istNowParts().date&&!['CANCELLED','REJECTED','VISIT_PROCESS_COMPLETED'].includes(item.currentStatus)]
  }
  const activeView=views[view]?view:''
  if(activeView){title=views[activeView][0];items=items.filter(views[activeView][1])}
  if(query)items=items.filter(item=>[item.requestNumber,item.hostName,...(item.visitorNames||[]),...(item.companyNames||[])].some(value=>String(value||'').toLowerCase().includes(query)))
  const action=user.role==='HOST_REQUESTER'&&mode!=='history'?'<a href="/visitor-requests/new" data-route class="primary-button">Create Visitor Request</a>':''
  const clearPath=mode==='history'?'/visitor-history':'/visitor-requests'
  const filterSummary=query||activeView?'<div class="filter-summary"><span class="small"><strong>'+h(items.length)+'</strong> matching request'+(items.length===1?'':'s')+'</span><div class="filter-chipset">'+(activeView?filterChip(views[activeView][0]):'')+(query?filterChip('Search: '+parameters.get('q')):'')+'<a href="'+clearPath+'" data-route class="filter-clear">Clear All</a></div></div>':''
  main.innerHTML=pageHeader(eyebrow,title,subtitle,action)+filterSummary+'<section class="panel">'+requestTable(items)+'</section>'
}

function istNowParts(){const parts=Object.fromEntries(new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Kolkata',year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23'}).formatToParts(new Date()).filter(item=>item.type!=='literal').map(item=>[item.type,item.value]));return{date:parts.year+'-'+parts.month+'-'+parts.day,time:parts.hour+':'+parts.minute}}
async function createRequestPage(main){
  if(user.role!=='HOST_REQUESTER')throw new Error('Only a host or requester can create a visitor request.')
  const now=istNowParts()
  const blank={value:'',label:'Select'}
  main.innerHTML='<form id="create-form" class="form-page">'+pageHeader('','Create Visitor Request')+privacyNotice()+'<p class="required-note"><span class="required-mark">*</span> Required</p><section class="form-section">'+selectField('visitorType','Visitor Type','',[blank,'External','Internal'],true)+selectField('visitingSite','Site/Facility','',[blank,'Bengaluru','Delhi'],true)+field('numberOfVisitors','Number of Visitors','','number',true)+selectField('visitPurposeType','Purpose of Visit','',[blank,'Technical','Non-Technical','Others'],true)+textareaField('purpose','Brief Description on Visit','',true)+field('areasToVisit','Areas to Visit')+field('mainHostName','Main Host','','text',true)+field('hostDepartment','Host Department')+field('escortingHostName','Escorting Host')+'</section><section class="schedule-card"><div class="schedule-heading"><h2 class="display">Visit Schedule (IST)</h2></div><div class="schedule-grid">'+field('startDate','Start Date','','date',true)+field('startTime','Start Time','','time',true)+field('endDate','End Date','','date',true)+field('endTime','End Time','','time',true)+'</div></section><section id="contractor-options" class="hidden"><label class="check-row"><input type="checkbox" name="faculty"> Facilities Contractor (Only Check This Box if Visitor is a Facilities Contractor)</label><label class="check-row"><input type="checkbox" name="gtr"> Gas Turbine Research Establishment Contractor (Only Check This Box if Visitor is a Gas Turbine Research Establishment Contractor)</label></section><button class="primary-button" type="submit">Save Request</button></form>'
  const typeSelect=document.querySelector('[name="visitorType"]')
  const visitorCount=document.querySelector('[name="numberOfVisitors"]')
  visitorCount.min='1';visitorCount.max='20'
  const contractorOptions=document.getElementById('contractor-options')
  const toggleContractors=()=>{const external=typeSelect.value==='External';contractorOptions.classList.toggle('hidden',!external);if(!external)contractorOptions.querySelectorAll('input').forEach(input=>{input.checked=false})}
  contractorOptions.querySelectorAll('input').forEach(input=>input.addEventListener('change',event=>{if(event.currentTarget.checked)contractorOptions.querySelectorAll('input').forEach(other=>{if(other!==event.currentTarget)other.checked=false})}))
  const startDate=document.querySelector('[name="startDate"]'),startTime=document.querySelector('[name="startTime"]'),endDate=document.querySelector('[name="endDate"]'),endTime=document.querySelector('[name="endTime"]')
  const updateLimits=()=>{startDate.min=now.date;endDate.min=startDate.value||now.date;startTime.min=startDate.value===now.date?now.time:'';if(startDate.value&&endDate.value&&endDate.value<startDate.value)endDate.value=startDate.value;endTime.min=endDate.value&&endDate.value===startDate.value?startTime.value:''}
  typeSelect.onchange=toggleContractors
  ;[startDate,startTime,endDate,endTime].forEach(input=>input.addEventListener('change',updateLimits))
  toggleContractors();updateLimits()
  document.getElementById('create-form').onsubmit=async event=>{
    event.preventDefault();const button=event.submitter;button.disabled=true;const data=new FormData(event.currentTarget)
    const start=new Date(data.get('startDate')+'T'+data.get('startTime')+':00+05:30'),end=new Date(data.get('endDate')+'T'+data.get('endTime')+':00+05:30')
    if(start<=new Date()){button.disabled=false;toast('The visit start must be in the future.','error');return}
    if(end<=start){button.disabled=false;toast('The visit end must be after the visit start.','error');return}
    const payload={visitorType:data.get('visitorType'),visitingSite:data.get('visitingSite'),numberOfVisitors:Number(data.get('numberOfVisitors')),visitPurposeType:data.get('visitPurposeType'),purpose:data.get('purpose'),areasToVisit:data.get('areasToVisit'),mainHostName:data.get('mainHostName'),hostDepartment:data.get('hostDepartment'),escortingHostName:data.get('escortingHostName'),startDate:data.get('startDate'),startTime:data.get('startTime'),endDate:data.get('endDate'),endTime:data.get('endTime'),faculty:data.get('faculty')==='on',gtr:data.get('gtr')==='on'}
    try{const result=await api('/api/visitor-requests',{method:'POST',body:JSON.stringify(payload)});toast('Visitor request created.');go('/visitor-requests/'+result.id)}catch(reason){button.disabled=false;toast(reason.message,'error')}
  }
}

function countrySelect(name,title,value,required=false,extraClass='',withDial=false){const listId='countries-'+name,options=COUNTRIES.map(item=>'<option value="'+h(item.name)+'"'+(withDial?' label="+'+h(item.dial)+'"':'')+'></option>').join('');return'<label class="field '+h(extraClass)+'">'+h(title)+requiredMark(required)+'<input name="'+h(name)+'" list="'+h(listId)+'" value="'+h(value||'')+'" placeholder="Start typing a country" autocomplete="country-name" '+(required?'required aria-required="true"':'')+'><datalist id="'+h(listId)+'">'+options+'</datalist></label>'}
function phoneFields(form,requested){
  const phoneClass=requested.has('phoneCountry')?'requested-field':''
  const numberClass=requested.has('telephone')?'requested-field':''
  return '<div class="phone-fields">'+countrySelect('phoneCountry','Phone Country Code',form.phoneCountry||'',true,phoneClass,true)+'<label class="field phone-input '+numberClass+'">Phone Number'+requiredMark(true)+'<div class="phone-input"><span class="phone-prefix" id="phone-prefix">+'+h(form.phoneDialCode||'')+'</span><input name="telephone" type="tel" value="'+h(form.telephone||'')+'" inputmode="numeric" autocomplete="tel-national" pattern="[0-9]+" required aria-required="true" aria-describedby="phone-limit"></div><span class="field-note" id="phone-limit"></span></label></div>'
}
function assetRows(){return visitorDraft.assets.map((asset,index)=>'<div class="asset-row" data-asset-row="'+index+'">'+field('assetType','Asset Type',asset.assetType,'text',true)+field('description','Description',asset.description)+field('serialNumber','Serial Number',asset.serialNumber,'text',true)+'<button type="button" class="secondary-button" data-remove-asset="'+index+'">Remove</button></div>').join('')}
function validateScreeningFile(file){if(file.type&&file.type!=='application/pdf'&&!file.name.toLowerCase().endsWith('.pdf'))throw new Error('Denied Party Screening Results must be a PDF.');if(!file.name.toLowerCase().endsWith('.pdf'))throw new Error('Denied Party Screening Results must use the .pdf extension.');if(file.size<=0||file.size>10485760)throw new Error('The PDF must be between 1 byte and 10 MB.')}
async function uploadScreeningResult(formId,file){validateScreeningFile(file);return api('/api/visitor-forms/'+encodeURIComponent(formId)+'/screening-results',{method:'POST',headers:{'Content-Type':'application/pdf'},body:file})}
async function downloadScreeningResult(formId){try{const response=await fetch('/api/visitor-forms/'+encodeURIComponent(formId)+'/screening-results',{headers:{'X-Visitor-Role':user.id}});if(!response.ok){const data=await response.json();throw new Error(data.error||'The PDF could not be downloaded.')}const blob=await response.blob(),url=URL.createObjectURL(blob),anchor=document.createElement('a');anchor.href=url;anchor.download='Denied Party Screening Results.pdf';anchor.click();URL.revokeObjectURL(url)}catch(reason){toast(reason.message,'error')}}
async function visitorFormPage(main){
  const token=++viewToken
  main.innerHTML='<div class="form-page">'+loadingState('Loading visitor details')+'</div>'
  const formId=location.pathname.split('/').pop()
  try{
    const form=await api('/api/visitor-forms/'+encodeURIComponent(formId))
    if(token!==viewToken)return
    visitorDraft=JSON.parse(JSON.stringify(form))
    const requested=new Set(form.requestedFields||[])
    const requestedClass=name=>requested.has(name)?'requested-field':''
    const requestedBanner=requested.size?'<div class="status-banner"><span>Update: '+[...requested].map(name=>h(REQUEST_FIELD_LABELS[name])).join(', ')+'</span></div>':''
    const existingPdf=form.screeningResult?'<div class="inline-meta"><span>Attached: '+h(form.screeningResult.fileName)+'</span><span>'+h(fmtBytes(form.screeningResult.fileSize))+'</span><button type="button" class="text-button" data-download-pdf="'+h(form.id)+'">Download</button></div>':'<p class="muted small">No PDF attached.</p>'
    const screeningBox=form.visitorType==='External'?'<section class="file-box"><strong>Denied Party Screening Results</strong><p class="small muted">Optional PDF, maximum 10 MB. It is available only to the owning Host and Export Control.</p>'+existingPdf+'<label class="upload-dropzone" for="screening-pdf"><span class="upload-icon" aria-hidden="true">↑</span><span><strong>Attach or Replace PDF</strong><small>Choose a PDF up to 10 MB</small></span><input id="screening-pdf" type="file" accept="application/pdf,.pdf"></label><div id="screening-file-meta" class="inline-meta" aria-live="polite"></div><div id="screening-upload-progress" class="upload-progress hidden" role="progressbar" aria-label="Uploading PDF"><span></span></div><button type="button" class="secondary-button" id="upload-screening-pdf">Upload PDF</button></section>':''
    main.innerHTML='<form id="visitor-form" class="form-page">'+pageHeader('','Visitor Information',form.requestNumber+' · '+label(form.status))+privacyNotice()+requestedBanner+'<p class="required-note"><span class="required-mark">*</span> Required</p><section class="form-section"><div class="name-grid">'+field('firstName','First Name',form.firstName,'text',true,requestedClass('firstName'))+field('middleName','Middle Name',form.middleName,'text',false,requestedClass('middleName'))+field('lastName','Last Name',form.lastName,'text',true,requestedClass('lastName'))+'</div>'+field('designation','Designation/Position Held',form.designation,'text',true,requestedClass('designation'))+countrySelect('citizenship','Citizenship',form.citizenship,true,requestedClass('citizenship'))+field('email','Email Address',form.email,'email',false,requestedClass('email'))+phoneFields(form,requested)+'</section><section class="form-section"><div class="section-span"><h2 class="display">Company Address</h2></div>'+field('companyName','Full Name of Visitor Company',form.companyName,'text',true,requestedClass('companyName'))+field('companyAddress','Address',form.companyAddress,'text',true,requestedClass('companyAddress'))+field('officeCity','City',form.officeCity,'text',true,requestedClass('officeCity'))+countrySelect('officeCountry','Country',form.officeCountry,true,requestedClass('officeCountry'))+'</section><section class="form-section">'+selectField('idType','ID Type',form.idType,[{value:'',label:'Select'},"Driver\'s License",'Passport','Voter ID','Aadhaar Card','PAN Card','Other Government Issued ID'],true,requestedClass('idType'))+'<div id="other-id-field"><label class="field '+h(requestedClass('otherIdType'))+'">Government-Issued ID Type<span class="required-mark" aria-hidden="true"> *</span><input name="otherIdType" type="text" value="'+h(form.otherIdType||'')+'"></label></div></section><section class="form-section single '+requestedClass('assets')+'"><h2 class="display" style="color:var(--title);margin:0">Declared Assets</h2><p class="small muted">A serial number is required for every asset. Security will compare the displayed serial number with the physical asset before approval.</p><div id="asset-rows">'+assetRows()+'</div><button type="button" class="text-button" id="add-asset">Add Asset</button></section>'+screeningBox+'<div class="button-row"><button class="primary-button" type="submit">'+(form.versions.length?'Save Revised Details':'Save Visitor Details')+'</button><a href="/visitor-requests/'+h(form.visitorRequestId)+'" data-route class="secondary-button">Back to Request</a></div></form>'
    document.querySelector('[data-download-pdf]')?.addEventListener('click',event=>downloadScreeningResult(event.currentTarget.dataset.downloadPdf))
    const firstName=document.querySelector('[name="firstName"]'),lastName=document.querySelector('[name="lastName"]');firstName.minLength=2;lastName.minLength=2
    const idType=document.querySelector('[name="idType"]'),otherIdField=document.getElementById('other-id-field'),otherIdInput=document.querySelector('[name="otherIdType"]')
    const toggleOtherId=()=>{const visible=idType.value==='Other Government Issued ID';otherIdField.classList.toggle('hidden',!visible);otherIdInput.required=visible;otherIdInput.setAttribute('aria-required',String(visible));if(!visible)otherIdInput.value=''}
    idType.onchange=toggleOtherId;toggleOtherId()
    const phoneCountry=document.querySelector('[name="phoneCountry"]'),telephone=document.querySelector('[name="telephone"]'),phonePrefix=document.getElementById('phone-prefix'),phoneLimit=document.getElementById('phone-limit')
    const updatePhone=()=>{const selected=COUNTRIES.find(item=>item.name===phoneCountry.value);const dial=selected?.dial||'';const lengths=selected?.lengths||[];const maximum=lengths.length?Math.max(...lengths):15;const minimum=lengths.length?Math.min(...lengths):1;phonePrefix.textContent='+'+dial;telephone.minLength=minimum;telephone.maxLength=maximum;telephone.value=telephone.value.replace(/\D/g,'').slice(0,maximum);const valid=!telephone.value||lengths.includes(telephone.value.length);telephone.setCustomValidity(valid?'':'Enter a valid national phone number length.');phoneLimit.textContent=lengths.length===1?lengths[0]+' digits':lengths.length?minimum+'–'+maximum+' digits':''}
    phoneCountry.onchange=updatePhone;phoneCountry.oninput=updatePhone;telephone.oninput=updatePhone;updatePhone()
    const syncAssets=()=>{document.querySelectorAll('[data-asset-row]').forEach(row=>{const index=Number(row.dataset.assetRow);visitorDraft.assets[index]={id:visitorDraft.assets[index].id,assetType:row.querySelector('[name="assetType"]').value,description:row.querySelector('[name="description"]').value,serialNumber:row.querySelector('[name="serialNumber"]').value}})}
    const redraw=()=>{document.getElementById('asset-rows').innerHTML=assetRows();document.querySelectorAll('[data-remove-asset]').forEach(button=>button.onclick=()=>{syncAssets();visitorDraft.assets.splice(Number(button.dataset.removeAsset),1);redraw()})}
    document.getElementById('add-asset').onclick=()=>{syncAssets();visitorDraft.assets.push({assetType:'',description:'',serialNumber:''});redraw()}
    const screeningInput=document.getElementById('screening-pdf'),screeningMeta=document.getElementById('screening-file-meta'),uploadProgress=document.getElementById('screening-upload-progress')
    if(screeningInput)screeningInput.onchange=()=>{const file=screeningInput.files[0];screeningMeta.textContent=file?file.name+' · '+fmtBytes(file.size):''}
    const uploadButton=document.getElementById('upload-screening-pdf');if(uploadButton)uploadButton.onclick=async event=>{const button=event.currentTarget,file=screeningInput.files[0];if(!file){toast('Select a PDF first.','error');return}button.disabled=true;button.textContent='Uploading...';uploadProgress.classList.remove('hidden');try{await uploadScreeningResult(formId,file);toast('Denied Party Screening Results uploaded.');await render()}catch(reason){button.disabled=false;button.textContent='Upload PDF';uploadProgress.classList.add('hidden');toast(reason.message,'error')}}
    redraw()
    document.getElementById('visitor-form').onsubmit=async event=>{
      event.preventDefault();const button=event.submitter;button.disabled=true;syncAssets();const data=new FormData(event.currentTarget);const payload={assets:visitorDraft.assets},pdf=screeningInput?.files[0]
      for(const name of ['firstName','middleName','lastName','citizenship','designation','companyName','companyAddress','officeCity','officeCountry','phoneCountry','telephone','email','idType','otherIdType'])payload[name]=data.get(name)
      try{if(pdf)validateScreeningFile(pdf);const result=await api('/api/visitor-forms/'+encodeURIComponent(formId)+'/submit',{method:'POST',body:JSON.stringify(payload)});if(pdf)await uploadScreeningResult(formId,pdf);toast('Visitor details saved.');go('/visitor-requests/'+result.id)}catch(reason){button.disabled=false;toast(reason.message,'error')}
    }
  }catch(reason){main.innerHTML='<div class="form-page">'+errorBox(reason.message)+'</div>'}
}

const classificationLabel=value=>label(value)
const classificationTag=value=>value?'<span class="tag '+(value==='Vendor'?'tag-vendor':'tag-visitor')+'">'+h(value)+'</span>':'-'
const badgeTypeTag=value=>value?'<span class="badge-type '+(value.startsWith('Orange')?'badge-orange':'badge-red')+'">'+h(value)+'</span>':'-'
function detailInfo(title,body,open=true){return'<details class="info-card details-card" '+(open?'open':'')+'><summary><h2>'+h(title)+'</h2></summary><div style="margin-top:14px">'+body+'</div></details>'}
function auditCategory(action){
  if(action==='RESCHEDULE')return'Schedule'
  if(action==='EC_REQUEST_DOCUMENTS'||action==='ADDITIONAL_INFORMATION_SUBMITTED')return'Information Request'
  if(action.startsWith('EC_')||action.startsWith('SCREENING_'))return'Export Control'
  if(['VERIFY_ENTRY','CHECK_IN','CHECK_OUT','NO_SHOW'].includes(action))return'Security'
  if(action.startsWith('VISITOR_FORM'))return'Visitor Information'
  return'Request'
}
function auditHistoryWidget(request){
  const categories=[...new Set(request.auditHistory.map(item=>auditCategory(item.action)))].sort()
  const visitors=request.visitors.map((visitor,index)=>({value:visitor.id,label:visitor.fullName==='Visitor Details Pending'?'Visitor '+(index+1):visitor.fullName}))
  const filters='<div class="filter-grid">'+field('auditSearch','Search')+selectField('auditCategory','Event Category','',[{value:'',label:'All Categories'},...categories])+selectField('auditVisitor','Visitor','',[{value:'',label:'All Visitors'},...visitors])+field('auditStart','From Date','','date')+field('auditEnd','To Date','','date')+'</div><div class="button-row"><button type="button" class="secondary-button compact-button" id="clear-audit">Clear Filters</button></div><div id="audit-results" class="audit-list"></div>'
  return detailInfo('Audit History',filters,false)
}
function bindAuditHistory(request){
  const results=document.getElementById('audit-results')
  if(!results)return
  const renderAudit=()=>{
    const query=document.querySelector('[name="auditSearch"]').value.trim().toLowerCase(),category=document.querySelector('[name="auditCategory"]').value,visitorId=document.querySelector('[name="auditVisitor"]').value,start=document.querySelector('[name="auditStart"]').value,end=document.querySelector('[name="auditEnd"]').value,visitor=request.visitors.find(item=>item.id===visitorId)
    const rows=request.auditHistory.filter(item=>{const eventDate=item.createdAt.slice(0,10),visitorMatch=!visitor||item.details.toLowerCase().includes(visitor.fullName.toLowerCase())||item.details.toLowerCase().includes(('Visitor '+visitor.sequence).toLowerCase());return(!query||(item.action+' '+item.details).toLowerCase().includes(query))&&(!category||auditCategory(item.action)===category)&&visitorMatch&&(!start||eventDate>=start)&&(!end||eventDate<=end)})
    const visitRange=fmtDateRange(request.visitDays[0]?.visitDate,request.visitDays.at(-1)?.visitDate)
    results.innerHTML=rows.length?rows.map(item=>{const details=item.details.replace(/Visit date: \d{4}-\d{2}-\d{2}\./gi,'Visit range: '+visitRange+'.');return'<article class="audit-event"><span class="audit-category">'+h(auditCategory(item.action))+'</span><p><strong>'+h(label(item.action))+'</strong><br><span class="small muted">'+h(details)+'</span></p><time class="audit-time">'+h(fmtTime(item.createdAt))+'</time></article>'}).join(''):'<p class="empty">No audit events match these filters.</p>'
  }
  document.querySelectorAll('[name="auditSearch"],[name="auditCategory"],[name="auditVisitor"],[name="auditStart"],[name="auditEnd"]').forEach(input=>input.addEventListener(input.name==='auditSearch'?'input':'change',renderAudit))
  document.getElementById('clear-audit').onclick=()=>{document.querySelectorAll('[name="auditSearch"],[name="auditCategory"],[name="auditVisitor"],[name="auditStart"],[name="auditEnd"]').forEach(input=>{input.value=''});renderAudit()}
  renderAudit()
}
function visitRecordList(visitor,days){
  if(!days.length)return''
  const counts=new Map(),badges=new Set()
  visitor.receptionRecords.forEach(record=>{counts.set(record.status,(counts.get(record.status)||0)+1);if(record.badge)badges.add(record.badge)})
  const statuses=[...counts].map(([status,count])=>'<span>'+chip(status)+' <span class="tiny muted">'+h(count)+' day'+(count===1?'':'s')+'</span></span>').join(' ')
  return'<div class="record"><p class="small"><strong>Visit Range:</strong> '+h(fmtDateRange(days[0].visitDate,days.at(-1).visitDate))+'</p><div class="button-row">'+statuses+'</div>'+(badges.size?'<p class="tiny muted" style="margin-top:8px">Badge ID: '+h([...badges].join(', '))+'</p>':'')+'</div>'
}
function hostCanEditVisitor(request,visitor){
  if(user.role!=='HOST_REQUESTER')return false
  if(request.currentStatus==='PENDING_DOCUMENTATION'&&visitor.status==='REVISION_REQUIRED')return true
  return !request.submittedToExportControl&&['DRAFT','VISITOR_FORM_PENDING','VISITOR_FORM_SUBMITTED','HOST_REVIEW'].includes(request.currentStatus)
}
function visitorDetailRow(visitor,index,days,request){
  const shownId=visitor.idType==='Other Government Issued ID'?visitor.otherIdType:visitor.idType
  const screening=infoField('Screening Decision',visitor.ecDecision?label(visitor.ecDecision):'Pending')+infoField('Person Type',visitor.personType)+infoField('Tag',classificationLabel(visitor.idClassification))+(visitor.ecDecisionReason?infoField('Decision Reason',visitor.ecDecisionReason):'')
  const pdf=request.visitorType==='External'&&visitor.screeningResult&&user.role!=='RECEPTION'?'<p><strong>Denied Party Screening Results:</strong> <button type="button" class="text-button" data-download-pdf="'+h(visitor.id)+'">Download PDF</button></p>':''
  const assets=visitor.assets.length?'<div><strong>Declared Assets:</strong>'+visitor.assets.map(asset=>'<p class="tiny">'+h(asset.assetType)+' · '+h(asset.description||'No Description')+' · Serial Number '+h(asset.serialNumber)+'</p>').join('')+'</div>':infoField('Declared Assets','None')
  const fields=infoField('Full Legal Name',visitor.fullName)+infoField('Designation/Position Held',visitor.designation)+infoField('Citizenship',visitor.citizenship)+infoField('Full Name of Visitor Company',visitor.companyName)+infoField('Address',visitor.companyAddress)+infoField('City',visitor.officeCity)+infoField('Country',visitor.officeCountry)+infoField('ID Type',shownId)+infoField('Email Address',visitor.email)+infoField('Phone Number',visitor.phone)+screening+assets+pdf
  const edit=hostCanEditVisitor(request,visitor)?'<a class="secondary-button compact-button" href="/visitor-forms/'+h(visitor.id)+'" data-route>Edit Details</a>':''
  return'<details class="visitor-detail-row"><summary><div><span class="row-label">Visitor</span><strong>'+h(visitor.fullName||'Visitor '+(index+1))+'</strong><br><span class="tiny muted">'+h(label(visitor.status||'DRAFT'))+'</span></div><div><span class="row-label">Company</span>'+h(visitor.companyName||'Pending')+'</div><div><span class="row-label">Citizenship</span>'+h(visitor.citizenship||'Pending')+'</div><div><span class="row-label">Screening</span>'+chip(visitor.ecDecision||'PENDING')+'</div><div><span class="row-label">Assets</span>'+h(visitor.assets.length)+'</div><span class="visitor-detail-toggle" aria-hidden="true"></span></summary><div class="visitor-detail-body"><div class="visitor-information-grid">'+fields+'</div><div style="margin-top:12px">'+visitRecordList(visitor,days)+'</div>'+(edit?'<div class="button-row" style="margin-top:12px">'+edit+'</div>':'')+'</div></details>'
}
function workflowProgress(request){
  const steps=['Request','Visitor Details','Host Review','Export Control','Reception','Complete']
  const activeByStatus={DRAFT:1,VISITOR_FORM_PENDING:1,VISITOR_FORM_SUBMITTED:2,HOST_REVIEW:2,PENDING_EC_REVIEW:3,EC_REVIEW:3,PENDING_DOCUMENTATION:3,DOCUMENTATION_SUBMITTED:3,EC_RE_REVIEW_REQUIRED:3,APPROVED:4,PARTIALLY_APPROVED:4,VISIT_PROCESS_COMPLETED:5}
  const failed=request.currentStatus==='REJECTED'||request.currentStatus==='CANCELLED'
  const failureIndex=request.currentStatus==='REJECTED'?3:request.submittedToExportControl?4:2
  const active=failed?failureIndex:(activeByStatus[request.currentStatus]??1)
  const complete=request.currentStatus==='VISIT_PROCESS_COMPLETED'
  return'<section class="workflow-progress" aria-labelledby="workflow-progress-title"><div class="workflow-progress-heading"><h2 id="workflow-progress-title">Workflow Progress</h2>'+chip(request.currentStatus)+'</div><ol>'+steps.map((step,index)=>{const state=complete||index<active?'complete':failed&&index===active?'error':index===active?'active':'incomplete';return'<li class="progress-step progress-'+state+'" '+(state==='active'||state==='error'?'aria-current="step"':'')+'><span class="progress-marker" aria-hidden="true"></span><span>'+h(step)+'</span></li>'}).join('')+'</ol></section>'
}
function receptionWorkflow(request,activeVisitor,activeDay,approvedVisitorOptions){
  const queue=request.visitors.filter(visitor=>visitor.ecDecision==='APPROVED').map(visitor=>{const record=visitor.receptionRecords.find(item=>item.visitDayId===activeDay)||visitor.receptionRecords[0],selected=activeVisitor===visitor.id,href='/visitor-requests/'+h(request.id)+'?visitor='+encodeURIComponent(visitor.id)+'&day='+encodeURIComponent(record?.visitDayId||activeDay);return'<a href="'+href+'" data-route class="reception-person '+(selected?'active':'')+'" aria-current="'+(selected?'true':'false')+'"><span><strong>'+h(visitor.fullName)+'</strong><small>'+h(fmtDateRange(request.visitDays[0]?.visitDate,request.visitDays.at(-1)?.visitDate))+'</small></span>'+chip(record?.status||'UPCOMING')+'</a>'}).join('')
  const visitor=request.visitors.find(item=>item.id===activeVisitor)
  const record=visitor?.receptionRecords.find(item=>item.visitDayId===activeDay)
  const activeDate=request.visitDays.find(item=>item.id===activeDay)?.visitDate||''
  const controls='<div class="reception-controls">'+selectField('activeVisitor','Visitor',activeVisitor,approvedVisitorOptions,true)+'<label class="field">Operational Date'+requiredMark(true)+'<input name="activeDate" type="date" value="'+h(activeDate)+'" min="'+h(request.visitDays[0]?.visitDate||'')+'" max="'+h(request.visitDays.at(-1)?.visitDate||'')+'" required aria-required="true"></label><input type="hidden" name="activeDay" value="'+h(activeDay)+'"></div><p class="small"><strong>Visit Range:</strong> '+h(fmtDateRange(request.visitDays[0]?.visitDate,request.visitDays.at(-1)?.visitDate))+'</p>'
  if(!visitor||!record)return'<section class="reception-actions"><div class="reception-queue" aria-label="Approved visitors">'+queue+'</div>'+controls+'<p class="muted small">Select a visitor and visit date.</p></section>'
  const locked=['CHECKED_IN','COMPLETED','NO_SHOW','ENTRY_REJECTED','CANCELLED'].includes(record.status)
  const verificationComplete=locked||record.status==='RECEPTION_VERIFICATION'
  const identity='<label class="verification-check"><input type="checkbox" name="identityConfirmed" '+(record.identityStatus==='APPROVED'?'checked':'')+' '+(verificationComplete?'disabled':'')+'><span><strong>Identity Verified'+requiredMark(true)+'</strong><small class="muted">Confirm that the presented ID matches the approved visitor record.</small></span></label>'
  const assets=visitor.assets.length?(verificationComplete?'<p class="small"><strong>'+visitor.assets.length+' declared asset'+(visitor.assets.length===1?'':'s')+'</strong> · '+h(label(record.assetsStatus))+'</p>':'<label class="verification-check"><input type="checkbox" name="assetsConfirmed"><span><strong>Assets Verified'+requiredMark(true)+'</strong><small class="muted">Compare each displayed serial number with the physical asset, then confirm verification.</small></span></label>'+visitor.assets.map(asset=>'<div class="asset-serial-check"><span><strong>'+h(asset.assetType)+'</strong><small class="muted">'+h(asset.description||'Declared asset')+'</small></span><span><small class="muted">Declared Serial Number</small><strong class="serial-value">'+h(asset.serialNumber)+'</strong></span></div>').join('')):'<p class="small muted">No assets were declared. Asset verification is not required.</p>'
  const verification='<div class="verification-checklist">'+identity+assets+(verificationComplete?(record.verificationRemarks?'<p class="small"><strong>Remarks:</strong> '+h(record.verificationRemarks)+'</p>':''):textareaField('verificationRemarks','Verification Remarks'))+(!verificationComplete?'<div class="button-row"><button class="success-button" data-detail-action="verification-approve">Approve Verification</button><button class="danger-button" data-detail-action="verification-reject">Reject Entry</button></div>':'')+'</div>'
  const ready=record.identityStatus==='APPROVED'&&['APPROVED','NOT_APPLICABLE'].includes(record.assetsStatus)&&record.status==='RECEPTION_VERIFICATION'
  const checkedIn=['CHECKED_IN','COMPLETED'].includes(record.status),checkedOut=record.status==='COMPLETED'
  const badge='<div class="badge-panel"><div><p class="tiny muted" style="margin-bottom:7px">Badge Type</p>'+badgeTypeTag(record.badgeType||visitor.badgeType)+'</div><label class="field">Badge ID'+requiredMark(true)+'<input name="receptionBadge" value="'+h(record.badge||'')+'" maxlength="64" '+(checkedIn?'disabled':'')+'></label></div>'
  const completion='<div class="completion-checks"><label class="completion-check"><input type="checkbox" data-reception-toggle="check-in" '+(checkedIn?'checked disabled':ready?'':'disabled')+'> Checked In</label><label class="completion-check"><input type="checkbox" data-reception-toggle="check-out" '+(checkedOut?'checked disabled':record.status==='CHECKED_IN'?'':'disabled')+'> Checked Out</label></div>'
  const noShow=['UPCOMING','VERIFICATION_IN_PROGRESS'].includes(record.status)&&record.identityStatus!=='APPROVED'&&record.assetsStatus!=='APPROVED'?'<button class="secondary-button" data-detail-action="no-show">Mark as No-Show</button>':''
  return'<section class="reception-actions"><div class="reception-queue" aria-label="Approved visitors">'+queue+'</div>'+controls+verification+(record.status==='ENTRY_REJECTED'?'<div class="error-box"><span>'+h(record.verificationRemarks)+'</span></div>':'')+(record.status!=='ENTRY_REJECTED'?badge+completion:'')+(noShow?'<div class="button-row" style="margin-top:14px">'+noShow+'</div>':'')+'</section>'
}
async function detailPage(main,id){
  const request=await api('/api/visitor-requests/'+encodeURIComponent(id))
  const isHost=user.role==='HOST_REQUESTER',isCompliance=user.role==='EXPORT_CONTROL',isReception=user.role==='RECEPTION'
  const reviewStatuses=['PENDING_EC_REVIEW','DOCUMENTATION_SUBMITTED','EC_RE_REVIEW_REQUIRED']
  const blank={value:'',label:'Select'}
  const query=new URLSearchParams(location.search)
  const approvedVisitors=request.visitors.filter(visitor=>visitor.ecDecision==='APPROVED')
  const requestedVisitor=query.get('visitor'),requestedDay=query.get('day'),today=istNowParts().date
  const activeVisitor=request.visitors.some(item=>item.id===requestedVisitor&&(!isReception||item.ecDecision==='APPROVED'))?requestedVisitor:(isReception?approvedVisitors[0]?.id||'':'')
  const activeDay=request.visitDays.some(item=>item.id===requestedDay)?requestedDay:(isReception?(request.visitDays.find(item=>item.visitDate===today)||request.visitDays[0])?.id||'':'')
  const visitorOptions=[blank,...request.visitors.map((visitor,index)=>({value:visitor.id,label:visitor.fullName==='Visitor Details Pending'?'Visitor '+(visitor.sequence||index+1):visitor.fullName}))]
  const approvedVisitorOptions=[blank,...approvedVisitors.map((visitor,index)=>({value:visitor.id,label:visitor.fullName==='Visitor Details Pending'?'Visitor '+(visitor.sequence||index+1):visitor.fullName}))]
  let actions=''
  if(isCompliance&&reviewStatuses.includes(request.currentStatus)){const visitorChecks=request.visitors.map((visitor,index)=>'<label class="selection-card"><input type="checkbox" name="ecVisitor" value="'+h(visitor.id)+'"><span><strong>'+h(visitor.fullName==='Visitor Details Pending'?'Visitor '+(index+1):visitor.fullName)+'</strong><small>'+h(visitor.companyName||'Company Pending')+'</small></span>'+chip(visitor.ecDecision||'PENDING')+(visitor.idClassification?'<span class="tag '+(visitor.idClassification==='Vendor'?'tag-vendor':'tag-visitor')+'">'+h(visitor.idClassification)+'</span>':'')+'</label>').join('');actions='<section class="review-box"><div class="panel-heading"><h2>Export Control Review</h2><div class="button-row"><button class="text-button" type="button" data-select-all-ec>Select All</button><button class="text-button" type="button" data-clear-ec>Clear</button></div></div><p class="required-note"><span class="required-mark">*</span> Select at least one visitor and one visitor tag.</p><div class="selection-grid" role="group" aria-label="Visitors Selected for Screening">'+visitorChecks+'</div><p class="small"><strong>Visitor Tag<span class="required-mark"> *</span></strong></p><div class="classification-grid" role="radiogroup" aria-label="Visitor Tag">'+[['Vendor','#f28c28'],['Visitor','#9F0000']].map(item=>'<label class="classification"><input type="radio" name="classification" value="'+item[0]+'"><span class="swatch" style="background:'+item[1]+'"></span>'+h(item[0])+'</label>').join('')+'</div><div class="button-row"><button class="success-button" data-detail-action="approve">Approve Selected</button><button class="primary-button" data-detail-action="request-info">Request Information</button><button class="danger-button" data-detail-action="reject">Reject Selected</button></div></section>'}
  if(isReception&&['APPROVED','PARTIALLY_APPROVED'].includes(request.currentStatus))actions=receptionWorkflow(request,activeVisitor,activeDay,approvedVisitorOptions)
  if(isHost){
    const hostButtons=[]
    const receptionStatuses=request.visitors.flatMap(visitor=>visitor.receptionRecords.map(record=>record.status))
    const receptionStarted=receptionStatuses.some(status=>['VERIFICATION_IN_PROGRESS','RECEPTION_VERIFICATION','ENTRY_REJECTED','CHECKED_IN','COMPLETED','NO_SHOW'].includes(status))
    const visitStarted=receptionStatuses.some(status=>['CHECKED_IN','COMPLETED'].includes(status))
    if(request.currentStatus==='VISITOR_FORM_SUBMITTED')hostButtons.push('<button class="primary-button" data-detail-action="host-review">Save</button>')
    if(request.currentStatus==='HOST_REVIEW')hostButtons.push('<button class="primary-button" data-detail-action="send-to-ec">Send to Export Control</button>')
    const schedulingAllowed=['DRAFT','VISITOR_FORM_PENDING','VISITOR_FORM_SUBMITTED','HOST_REVIEW','APPROVED','PARTIALLY_APPROVED'].includes(request.currentStatus)
    if(schedulingAllowed){if(!receptionStarted)hostButtons.push('<button class="secondary-button" data-detail-action="reschedule">Reschedule</button>');if(!visitStarted)hostButtons.push('<button class="danger-button" data-detail-action="cancel">Cancel Request</button>')}
    actions=hostButtons.length?'<div class="button-row" style="margin-bottom:24px">'+hostButtons.join('')+'</div>':''
  }
  const contractorFields=request.visitorType==='External'?infoField('Facilities Contractor',request.faculty?'Yes':'No')+infoField('Gas Turbine Research Establishment Contractor',request.gtr?'Yes':'No'):''
  const requestFields=infoField('Request Number',request.requestNumber,true)+infoField('Number of Visitors',request.visitorCount)+infoField('Site/Facility',request.visitingSite)+infoField('Visit Range',fmtDateRange(request.visitDays[0]?.visitDate,request.visitDays.at(-1)?.visitDate))+infoField('Purpose of Visit',request.visitPurposeType)+infoField('Brief Description on Visit',request.purpose)+infoField('Areas to Visit',request.areasToVisit)+infoField('Main Host',request.mainHostName)+infoField('Host Department',request.hostDepartment)+infoField('Escorting Host',request.escortingHostName)+contractorFields+(request.cancellationReason?infoField('Cancellation Reason',request.cancellationReason):'')
  const visitorIndex='<div class="visitor-list-index" aria-hidden="true"><span>Visitor</span><span>Company</span><span>Citizenship</span><span>Screening</span><span>Assets</span><span>Details</span></div>'
  const visitorDetails='<section class="panel visitor-details-widget"><div class="panel-heading"><h2>Visitor Index</h2><span class="small muted">'+h(request.visitorCount)+' Total</span></div>'+visitorIndex+'<div class="visitor-detail-list">'+request.visitors.map((visitor,index)=>visitorDetailRow(visitor,index,request.visitDays,request)).join('')+'</div></section>'
  const screening=isCompliance?detailInfo('Screening Remarks','<div class="form-section single">'+selectField('screeningVisitor','Visitor','',visitorOptions,true)+textareaField('screeningRemark','Remark','',true)+'<button class="primary-button" data-detail-action="add-screening-remark">Add Remark</button></div>'+recordList(request.screeningRemarks,item=>'<div class="record"><p><strong>'+h(item.visitorName)+':</strong> '+h(item.remark)+'</p><p class="tiny muted">'+h(fmtTime(item.createdAt))+'</p></div>','No screening remarks.'),false):''
  const histories=!isReception?'<div class="detail-stack">'+auditHistoryWidget(request)+'</div>':''
  main.innerHTML='<a href="/visitor-requests" data-route class="back-link link">← Back to Requests</a>'+pageHeader('','Request Details',request.requestNumber,chip(request.currentStatus))+privacyNotice()+workflowProgress(request)+actions+'<section class="info-card request-overview"><h2>Request Information</h2><div class="request-field-grid">'+requestFields+'</div></section>'+visitorDetails+'<div class="detail-stack" style="margin-top:16px">'+screening+histories+'</div>'
  bindDetailActions(request)
  bindReceptionToggles(request)
  bindAuditHistory(request)
  document.querySelector('[data-select-all-ec]')?.addEventListener('click',()=>document.querySelectorAll('[name="ecVisitor"]').forEach(item=>{item.checked=true}))
  document.querySelector('[data-clear-ec]')?.addEventListener('click',()=>document.querySelectorAll('[name="ecVisitor"]').forEach(item=>{item.checked=false}))
  document.querySelectorAll('[data-download-pdf]').forEach(button=>button.addEventListener('click',()=>downloadScreeningResult(button.dataset.downloadPdf)))
  const receptionVisitor=document.querySelector('[name="activeVisitor"]'),receptionDay=document.querySelector('[name="activeDay"]'),receptionDate=document.querySelector('[name="activeDate"]')
  const updateReceptionVisitor=()=>{if(receptionVisitor?.value&&receptionDay?.value)go('/visitor-requests/'+request.id+'?visitor='+encodeURIComponent(receptionVisitor.value)+'&day='+encodeURIComponent(receptionDay.value))}
  const updateReceptionDate=()=>{const day=request.visitDays.find(item=>item.visitDate===receptionDate?.value);if(receptionVisitor?.value&&day)go('/visitor-requests/'+request.id+'?visitor='+encodeURIComponent(receptionVisitor.value)+'&day='+encodeURIComponent(day.id))}
  if(receptionVisitor)receptionVisitor.onchange=updateReceptionVisitor
  if(receptionDate)receptionDate.onchange=updateReceptionDate
}

function chosenClassification(){return document.querySelector('[name="classification"]:checked')?.value||''}
function chosenEcVisitors(){return[...document.querySelectorAll('[name="ecVisitor"]:checked')].map(item=>item.value)}
function chosenVisitor(){return document.querySelector('[name="activeVisitor"]')?.value||''}
function chosenDay(){return document.querySelector('[name="activeDay"]')?.value||''}
function istParts(value){const parts=Object.fromEntries(new Intl.DateTimeFormat('en-GB',{timeZone:'Asia/Kolkata',year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23'}).formatToParts(new Date(value)).filter(item=>item.type!=='literal').map(item=>[item.type,item.value]));return{date:parts.year+'-'+parts.month+'-'+parts.day,time:parts.hour+':'+parts.minute}}
async function actionRequest(request,action,extra={}){await api('/api/visitor-requests/'+encodeURIComponent(request.id)+'/actions',{method:'POST',body:JSON.stringify({action,visitorFormId:chosenVisitor(),visitDayId:chosenDay(),...extra})});toast('Request updated.');await render()}
function requireClassification(){const value=chosenClassification();if(!value)throw new Error('Select Vendor or Visitor.');return value}
function requireEcVisitors(){const values=chosenEcVisitors();if(!values.length)throw new Error('Select at least one visitor.');return values}
function requireReceptionSelection(){const visitorFormId=chosenVisitor(),visitDayId=chosenDay();if(!visitorFormId||!visitDayId)throw new Error('Select a visitor and visit date.');return{visitorFormId,visitDayId}}
function bindDetailActions(request){
  document.querySelectorAll('[data-detail-action]').forEach(button=>button.onclick=async()=>{
    const action=button.dataset.detailAction
    try{
      if(action==='host-review'){modal('Save Host Review','<p class="small">Confirm that every visitor form has been reviewed.</p>','Save',()=>actionRequest(request,'host-review'));return}
      if(action==='send-to-ec'){modal('Send to Export Control','<p class="small">The request will be locked for Export Control review.</p>','Send',()=>actionRequest(request,'send-to-ec'));return}
      if(action==='reschedule'){
        const start=istParts(request.visitStart),end=istParts(request.visitEnd),now=istNowParts()
        modal('Reschedule Visit','<div class="schedule-grid">'+field('newStartDate','Start Date',start.date,'date',true)+field('newStartTime','Start Time',start.time,'time',true)+field('newEndDate','End Date',end.date,'date',true)+field('newEndTime','End Time',end.time,'time',true)+'</div>'+textareaField('rescheduleReason','Reason') ,'Reschedule',()=>{const values={startDate:document.querySelector('[name="newStartDate"]').value,startTime:document.querySelector('[name="newStartTime"]').value,endDate:document.querySelector('[name="newEndDate"]').value,endTime:document.querySelector('[name="newEndTime"]').value,reason:document.querySelector('[name="rescheduleReason"]').value};return actionRequest(request,'reschedule',values)})
        const startDate=document.querySelector('[name="newStartDate"]'),startTime=document.querySelector('[name="newStartTime"]'),endDate=document.querySelector('[name="newEndDate"]'),endTime=document.querySelector('[name="newEndTime"]')
        const updateLimits=()=>{startDate.min=now.date;endDate.min=startDate.value||now.date;startTime.min=startDate.value===now.date?now.time:'';if(endDate.value<startDate.value)endDate.value=startDate.value;endTime.min=endDate.value===startDate.value?startTime.value:''}
        ;[startDate,startTime,endDate,endTime].forEach(input=>input.addEventListener('change',updateLimits));updateLimits()
        return
      }
      if(action==='cancel'){modal('Cancel Request',textareaField('cancelReason','Cancellation Reason','',true),'Cancel Request',()=>actionRequest(request,'cancel',{reason:document.querySelector('[name="cancelReason"]').value}),'danger');return}
      if(action==='approve'){const visitorFormIds=requireEcVisitors(),classification=requireClassification();modal('Approve Selected Visitors','<p class="small">Approve '+visitorFormIds.length+' selected visitor'+(visitorFormIds.length===1?'':'s')+'.</p>'+textareaField('approvalRemark','Approval Remarks'),'Approve Selected',()=>actionRequest(request,'ec-approve',{visitorFormIds,comment:document.querySelector('[name="approvalRemark"]').value,idClassification:classification}),'success');return}
      if(action==='request-info'){
        const fields=Object.entries(REQUEST_FIELD_LABELS).map(item=>'<label class="check-row"><input type="checkbox" name="informationField" value="'+h(item[0])+'"> '+h(item[1])+'</label>').join('')
        modal('Request Information',selectField('informationVisitor','Visitor','',[{value:'',label:'Select'},...request.visitors.map((visitor,index)=>({value:visitor.id,label:visitor.fullName==='Visitor Details Pending'?'Visitor '+(index+1):visitor.fullName}))],true)+'<div class="checkbox-grid">'+fields+'</div>'+textareaField('informationComment','Instructions','',true),'Send Request',()=>actionRequest(request,'ec-request-documents',{visitorFormId:document.querySelector('[name="informationVisitor"]').value,fields:[...document.querySelectorAll('[name="informationField"]:checked')].map(item=>item.value),comment:document.querySelector('[name="informationComment"]').value}));return
      }
      if(action==='reject'){const visitorFormIds=requireEcVisitors(),classification=requireClassification();modal('Reject Selected Visitors',textareaField('rejectionReason','Rejection Reason','',true),'Reject Selected',()=>actionRequest(request,'ec-reject',{visitorFormIds,reason:document.querySelector('[name="rejectionReason"]').value,idClassification:classification}),'danger');return}
      if(action==='add-screening-remark'){button.disabled=true;await actionRequest(request,'ec-add-remark',{visitorFormId:document.querySelector('[name="screeningVisitor"]').value,remark:document.querySelector('[name="screeningRemark"]').value});return}
      const selection=requireReceptionSelection()
      const visitor=request.visitors.find(item=>item.id===selection.visitorFormId)
      if(action==='verification-approve'){const identityConfirmed=document.querySelector('[name="identityConfirmed"]')?.checked===true,assetsConfirmed=visitor.assets.length?document.querySelector('[name="assetsConfirmed"]')?.checked===true:true,remarks=document.querySelector('[name="verificationRemarks"]')?.value||'';if(!identityConfirmed)throw new Error('Confirm identity verification.');if(visitor.assets.length&&!assetsConfirmed)throw new Error('Confirm asset verification.');modal('Approve Verification','<p class="small">Approve identity'+(visitor.assets.length?' and '+visitor.assets.length+' declared asset'+(visitor.assets.length===1?'':'s'):'')+' for <strong>'+h(visitor.fullName)+'</strong>.</p>','Approve',()=>actionRequest(request,'verify-entry',{...selection,decision:'APPROVE',identityConfirmed,assetsConfirmed,remarks}),'success');return}
      if(action==='verification-reject'){const remarks=document.querySelector('[name="verificationRemarks"]')?.value.trim()||'';if(!remarks)throw new Error('Enter remarks before rejecting entry.');modal('Reject Entry','<p class="small">This rejection is final for the selected visitor and visit date.</p><p class="small"><strong>Remarks:</strong> '+h(remarks)+'</p>','Reject Entry',()=>actionRequest(request,'verify-entry',{...selection,decision:'REJECT',remarks}),'danger');return}
      if(action==='no-show'){modal('Mark as No-Show','<p class="small">'+h(visitor.fullName)+' will be recorded as a no-show within '+h(fmtDateRange(request.visitDays[0]?.visitDate,request.visitDays.at(-1)?.visitDate))+'.</p>','Mark as No-Show',()=>actionRequest(request,'no-show',selection));return}
    }catch(reason){button.disabled=false;toast(reason.message,'error')}
  })
}

function bindReceptionToggles(request){
  document.querySelectorAll('[data-reception-toggle]').forEach(checkbox=>checkbox.onchange=()=>{
    if(!checkbox.checked)return
    checkbox.checked=false
    try{
      const selection=requireReceptionSelection(),visitor=request.visitors.find(item=>item.id===selection.visitorFormId)
      if(checkbox.dataset.receptionToggle==='check-in'){
        const badge=document.querySelector('[name="receptionBadge"]')?.value.trim()||''
        if(!badge)throw new Error('Enter the badge ID before check-in.')
        modal('Check in visitor','<p class="small">Assign <strong>'+h(visitor.badgeType)+'</strong> badge <strong>'+h(badge)+'</strong> to '+h(visitor.fullName)+'.</p>','Check in',()=>actionRequest(request,'check-in',{...selection,badgeNumber:badge}),'success')
      }else{
        modal('Check out visitor','<p class="small">Confirm badge return and departure for '+h(visitor.fullName)+'.</p>','Check out',()=>actionRequest(request,'check-out',selection))
      }
    }catch(reason){toast(reason.message,'error')}
  })
}

async function pendingPage(main){const data=await api('/api/visitor-requests');const pending=data.items.filter(item=>['VISITOR_FORM_PENDING','VISITOR_FORM_SUBMITTED','HOST_REVIEW','PENDING_EC_REVIEW','PENDING_DOCUMENTATION','DOCUMENTATION_SUBMITTED','EC_RE_REVIEW_REQUIRED'].includes(item.currentStatus));main.innerHTML=pageHeader('','Pending Actions')+'<section class="panel">'+requestTable(pending)+'</section>'}
async function receptionPage(main,asDashboard=false){
  const data=await api('/api/reception/dashboard')
  const view=new URLSearchParams(location.search).get('view')||''
  const filters={today:item=>item.scheduledToday,expected:item=>['UPCOMING','VERIFICATION_IN_PROGRESS','RECEPTION_VERIFICATION'].includes(item.status),inside:item=>item.status==='CHECKED_IN','checked-out':item=>item.status==='COMPLETED',rejected:item=>item.status==='ENTRY_REJECTED','no-show':item=>item.status==='NO_SHOW'}
  const filterNames={today:"Today's Visitors",expected:'Expected',inside:'Currently Inside','checked-out':"Today's Checked Out",rejected:"Today's Rejected",'no-show':"Today's No-Shows"}
  const activeView=filters[view]?view:''
  const items=activeView?data.items.filter(filters[activeView]):data.items
  const base=asDashboard?'/dashboard':'/reception'
  main.innerHTML=pageHeader('',asDashboard?'Security/Reception':'Reception Dashboard','','<div class="button-row"><a href="/reports" data-route class="primary-button">Open Reports</a><button class="secondary-button" data-render>Refresh</button></div>')+summaryCards([["Today's Visitors",data.todaysVisitors,base+'?view=today'],['Expected',data.expected,base+'?view=expected'],['Currently Inside',data.currentlyInside,base+'?view=inside'],["Today's Checked Out",data.checkedOut,base+'?view=checked-out'],["Today's Rejected",data.rejected,base+'?view=rejected'],["Today's No-Shows",data.noShow,base+'?view=no-show']])+(activeView?'<div class="status-banner"><span><strong>'+h(items.length)+'</strong> matching visitor'+(items.length===1?'':'s')+' · '+h(filterNames[activeView])+'</span><a href="'+base+'" data-route class="link">Clear Filters</a></div>':'')+'<section class="panel"><div class="panel-heading"><h2>Visitor Index</h2><span class="small muted" id="reception-count">'+h(items.length)+' Records</span></div><label class="field" style="display:block;margin:14px 0;width:min(100%,420px)">Search Visitors<input id="reception-search" type="search" placeholder="Name, company, request, asset, badge, or status"></label><div id="reception-results">'+receptionTable(items)+'</div></section>'
  document.getElementById('reception-search').oninput=event=>{const query=event.currentTarget.value.trim().toLowerCase();const matches=items.filter(item=>[item.visitorName,item.company,item.badge,item.requestNumber,item.mainHost,item.escort,item.personType,item.status,item.identityStatus,item.assetsStatus,...(item.assets||[]).flatMap(asset=>[asset.assetType,asset.description,asset.serialNumber])].some(value=>String(value||'').toLowerCase().includes(query)));document.getElementById('reception-count').textContent=matches.length+' Record'+(matches.length===1?'':'s');document.getElementById('reception-results').innerHTML=receptionTable(matches)}
}
function receptionTable(items){
  if(!items.length)return emptyState('No matching visitors','No approved visitor records match the selected view or search.')
  const link=item=>'/visitor-requests/'+h(item.requestId)+'?visitor='+encodeURIComponent(item.visitorFormId)+'&day='+encodeURIComponent(item.visitDayId)
  const assetText=item=>(item.assets||[]).length?item.assets.length+' · '+item.assets.map(asset=>asset.assetType).join(', '):'None'
  const desktop='<div class="table-wrap reception-desktop"><table class="data-table compact-table"><thead><tr><th scope="col">Request Number</th><th scope="col">Visitor</th><th scope="col">Company</th><th scope="col">Host and Escort</th><th scope="col">Person Type</th><th scope="col">Visit Range</th><th scope="col">ID Type</th><th scope="col">Assets</th><th scope="col">Badge Type</th><th scope="col">Badge ID</th><th scope="col">Status</th><th scope="col"></th></tr></thead><tbody>'+items.map(item=>'<tr><td class="strong link">'+h(item.requestNumber)+'</td><td class="strong">'+h(item.visitorName)+'</td><td>'+h(item.company)+'</td><td>'+h(item.mainHost+(item.escort?' / '+item.escort:''))+'</td><td>'+h(item.personType)+'</td><td><strong>'+h(fmtDateRange(item.visitStartDate,item.visitEndDate))+'</strong></td><td>'+h(item.idType||'-')+'</td><td>'+h(assetText(item))+'</td><td>'+badgeTypeTag(item.badgeType)+'</td><td class="nowrap"><strong>'+h(item.badge||'-')+'</strong></td><td>'+chip(item.status)+'</td><td><a href="'+link(item)+'" data-route class="primary-button compact-button">Manage</a></td></tr>').join('')+'</tbody></table></div>'
  const mobile='<div class="mobile-reception">'+items.map(item=>'<article class="mobile-reception-card"><div class="panel-heading"><div><strong>'+h(item.visitorName)+'</strong><p class="tiny muted">'+h(item.company)+'</p></div>'+chip(item.status)+'</div><dl><div><dt>Visit Range</dt><dd>'+h(fmtDateRange(item.visitStartDate,item.visitEndDate))+'</dd></div><div><dt>Person Type</dt><dd>'+h(item.personType)+'</dd></div><div><dt>ID Type</dt><dd>'+h(item.idType||'-')+'</dd></div><div><dt>Assets</dt><dd>'+h(assetText(item))+'</dd></div><div><dt>Badge Type</dt><dd>'+badgeTypeTag(item.badgeType)+'</dd></div><div><dt>Badge ID</dt><dd class="nowrap">'+h(item.badge||'-')+'</dd></div></dl><a href="'+link(item)+'" data-route class="primary-button wide" style="display:block;text-align:center;margin-top:14px">Manage Visitor</a></article>').join('')+'</div>'
  return desktop+mobile
}

function collapseAnalyticsRows(rows){
  const groups=new Map(),today=istNowParts().date,priority=row=>row.receptionStatus==='CHECKED_IN'?500:row.visitDate===today?400:['COMPLETED','NO_SHOW','ENTRY_REJECTED'].includes(row.receptionStatus)?300:row.receptionStatus==='RECEPTION_VERIFICATION'?200:100
  ;[...rows].sort((a,b)=>a.visitDate.localeCompare(b.visitDate)).forEach(row=>{const key=row.requestId+'|'+row.visitorFormId,current=groups.get(key),candidate={...row,visitStartDate:row.requestVisitStartDate,visitEndDate:row.requestVisitEndDate,openVisitDayId:row.visitDayId,_priority:priority(row)};if(!current||candidate._priority>current._priority||candidate._priority===current._priority&&candidate._priority>=300&&row.visitDate>current.visitDate)groups.set(key,candidate)})
  groups.forEach(item=>delete item._priority)
  return[...groups.values()]
}
function analyticsTable(rows){
  if(!rows.length)return emptyState('No matching records','Adjust or clear the filters to see additional visitor records.')
  const desktop='<div class="table-wrap desktop-only"><table class="data-table compact-table"><thead><tr><th scope="col">Visitor</th><th scope="col">Visit Range</th><th scope="col">Site</th><th scope="col">Request Number</th><th scope="col">Request Status</th><th scope="col">Screening</th><th scope="col">Reception</th><th scope="col">Identity</th><th scope="col">Assets</th><th scope="col">Badge ID</th><th scope="col"></th></tr></thead><tbody>'+rows.map(row=>'<tr><td class="strong">'+h(row.visitor||'Visitor Details Pending')+'<br><span class="tiny muted">'+h(row.company)+'</span></td><td>'+h(fmtDateRange(row.visitStartDate,row.visitEndDate))+'</td><td>'+h(row.site)+'</td><td class="strong nowrap">'+h(row.requestNumber)+'</td><td>'+chip(row.status)+'</td><td>'+chip(row.screeningDecision||'PENDING')+'</td><td>'+chip(row.receptionStatus)+'</td><td>'+chip(row.identityStatus)+'</td><td>'+chip(row.assetsStatus)+'<br><span class="tiny muted">'+h(row.assetCount?row.assetSummary:'None Declared')+'</span></td><td class="nowrap">'+h(row.badgeId||'-')+'</td><td><a href="/visitor-requests/'+h(row.requestId)+'?visitor='+encodeURIComponent(row.visitorFormId)+'&day='+encodeURIComponent(row.openVisitDayId)+'" data-route class="link">Open</a></td></tr>').join('')+'</tbody></table></div>'
  const mobile='<div class="mobile-records">'+rows.map(row=>'<article class="mobile-record"><div class="panel-heading"><strong>'+h(row.visitor||'Visitor Details Pending')+'</strong>'+chip(row.receptionStatus)+'</div><dl><div><dt>Request Number</dt><dd>'+h(row.requestNumber)+'</dd></div><div><dt>Request Status</dt><dd>'+h(label(row.status))+'</dd></div><div><dt>Visit Range</dt><dd>'+h(fmtDateRange(row.visitStartDate,row.visitEndDate))+'</dd></div><div><dt>Site</dt><dd>'+h(row.site)+'</dd></div><div><dt>Screening</dt><dd>'+h(label(row.screeningDecision||'PENDING'))+'</dd></div><div><dt>Identity</dt><dd>'+h(label(row.identityStatus))+'</dd></div><div><dt>Assets</dt><dd>'+h(row.assetCount?row.assetSummary:'None Declared')+'</dd></div><div><dt>Badge ID</dt><dd class="nowrap">'+h(row.badgeId||'-')+'</dd></div></dl><a href="/visitor-requests/'+h(row.requestId)+'?visitor='+encodeURIComponent(row.visitorFormId)+'&day='+encodeURIComponent(row.openVisitDayId)+'" data-route class="primary-button compact-button">Open Record</a></article>').join('')+'</div>'
  return desktop+mobile
}
async function analyticsPage(main,title='Visitor Analytics'){
  const hostHistory=user.role==='HOST_REQUESTER',data=await api('/api/analytics'),initialStatus=new URLSearchParams(location.search).get('status')||''
  const historyStatuses=new Set(['APPROVED','PARTIALLY_APPROVED','REJECTED','CANCELLED','VISIT_PROCESS_COMPLETED'])
  const sourceRows=hostHistory?data.rows.filter(row=>historyStatuses.has(row.status)):data.rows
  const unique=value=>[...new Set(sourceRows.map(row=>row[value]).filter(Boolean))].sort()
  const blank={value:'',label:'All'}
  const filters='<div class="filter-grid">'+field('analyticsSearch','Search')+field('analyticsDate','Visit Date','','date')+selectField('analyticsStatus','Request Status',initialStatus,[blank,...unique('status').map(value=>({value,label:value?label(value):'All'}))])+selectField('analyticsScreening','Screening Status','',[blank,...unique('screeningDecision').map(value=>({value,label:label(value)}))])+selectField('analyticsReception','Reception Status','',[blank,...unique('receptionStatus').map(value=>({value,label:label(value)}))])+selectField('analyticsIdentity','Identity Status','',[blank,...unique('identityStatus').map(value=>({value,label:label(value)}))])+selectField('analyticsAssets','Assets','',[blank,{value:'DECLARED',label:'Declared'},{value:'NONE',label:'None Declared'},{value:'VERIFIED',label:'Verified'},{value:'REJECTED',label:'Rejected'}])+selectField('analyticsSite','Site','',[blank,...unique('site')])+selectField('analyticsPersonType','Person Type','',[blank,...unique('personType')])+'</div><div class="button-row"><button type="button" class="secondary-button" id="clear-analytics">Clear Filters</button></div>'
  const roleTitle=hostHistory?'Visitor History':user.role==='RECEPTION'?'Security Reports':'Export Control Reports'
  main.innerHTML=pageHeader('',title||roleTitle,hostHistory?'Your completed, approved, rejected, and cancelled visitors with date-level filtering.':'Visitor-level counts with compact visit ranges and day-specific filtering.','<div class="button-row"><button class="secondary-button" data-export="csv">Export CSV</button><button class="primary-button" data-export="xlsx">Export XLSX</button></div>')+privacyNotice()+'<section class="panel">'+filters+'<div id="analytics-chips" class="filter-chipset analytics-filter-chips" aria-live="polite"></div><div id="analytics-summary"></div><h2 class="table-section-heading">Visitor Index</h2><div id="analytics-results" class="analytics-results"></div></section>'
  const values=()=>({q:document.querySelector('[name="analyticsSearch"]').value.trim(),date:document.querySelector('[name="analyticsDate"]').value,status:document.querySelector('[name="analyticsStatus"]').value,screening:document.querySelector('[name="analyticsScreening"]').value,reception:document.querySelector('[name="analyticsReception"]').value,identity:document.querySelector('[name="analyticsIdentity"]').value,assets:document.querySelector('[name="analyticsAssets"]').value,site:document.querySelector('[name="analyticsSite"]').value,personType:document.querySelector('[name="analyticsPersonType"]').value,history:hostHistory?'1':''})
  const filtered=()=>{const filter=values(),query=filter.q.toLowerCase();return sourceRows.filter(row=>(!query||[row.requestNumber,row.visitor,row.company,row.host,row.hostDepartment,row.badgeId,row.assetSummary].some(value=>String(value||'').toLowerCase().includes(query)))&&(!filter.date||row.visitDate===filter.date)&&(!filter.status||row.status===filter.status)&&(!filter.screening||row.screeningDecision===filter.screening)&&(!filter.reception||row.receptionStatus===filter.reception)&&(!filter.identity||row.identityStatus===filter.identity)&&(!filter.site||row.site===filter.site)&&(!filter.personType||row.personType===filter.personType)&&(!filter.assets||(filter.assets==='DECLARED'?row.assetCount>0:filter.assets==='NONE'?row.assetCount===0:filter.assets==='VERIFIED'?row.assetCount>0&&row.assetsStatus==='APPROVED':row.assetsStatus==='REJECTED')))}
  const filterMeta={q:['Search','analyticsSearch'],date:['Visit Date','analyticsDate'],status:['Request Status','analyticsStatus'],screening:['Screening','analyticsScreening'],reception:['Reception','analyticsReception'],identity:['Identity','analyticsIdentity'],assets:['Assets','analyticsAssets'],site:['Site','analyticsSite'],personType:['Person Type','analyticsPersonType']}
  const refresh=()=>{const filter=values(),dailyRows=filtered(),rows=collapseAnalyticsRows(dailyRows),visitors=new Set(dailyRows.map(row=>row.visitorFormId)),requests=new Set(dailyRows.map(row=>row.requestId)),assetVisitors=new Set(dailyRows.filter(row=>row.assetCount>0).map(row=>row.visitorFormId));document.getElementById('analytics-summary').innerHTML=summaryCards([['Matching Visitors',visitors.size],['Matching Requests',requests.size],['Checked In',new Set(dailyRows.filter(row=>row.receptionStatus==='CHECKED_IN').map(row=>row.visitorFormId)).size],['Assets Declared',assetVisitors.size]]);document.getElementById('analytics-results').innerHTML=analyticsTable(rows);document.getElementById('analytics-chips').innerHTML=Object.entries(filterMeta).filter(([key])=>filter[key]).map(([key,meta])=>filterChip(meta[0]+': '+(key==='status'||key==='screening'||key==='reception'||key==='identity'?label(filter[key]):filter[key]),meta[1])).join('');document.querySelectorAll('[data-clear-filter]').forEach(button=>button.onclick=()=>{document.querySelector('[name="'+button.dataset.clearFilter+'"]').value='';refresh()})}
  document.querySelectorAll('.filter-grid input,.filter-grid select').forEach(input=>input.addEventListener(input.tagName==='INPUT'&&input.type==='text'?'input':'change',refresh))
  document.getElementById('clear-analytics').onclick=()=>{document.querySelectorAll('.filter-grid input,.filter-grid select').forEach(input=>{input.value=''});refresh()}
  document.querySelectorAll('[data-export]').forEach(button=>button.onclick=()=>downloadExport(button.dataset.export,values()))
  refresh()
}
async function downloadExport(format,filters={}){try{const query=new URLSearchParams(Object.entries(filters).filter(item=>item[1])).toString(),response=await fetch('/api/analytics/export.'+format+(query?'?'+query:''),{headers:{'X-Visitor-Role':user.id}});if(!response.ok)throw new Error('Export could not be generated.');const blob=await response.blob(),url=URL.createObjectURL(blob),anchor=document.createElement('a');anchor.href=url;anchor.download='visitor-analytics.'+format;anchor.click();URL.revokeObjectURL(url)}catch(reason){toast(reason.message,'error')}}
async function render(){
  closeModal();const path=location.pathname
  const token=++viewToken
  if(!user){if(path!=='/login')sessionStorage.setItem('visitor.destination',path+location.search);renderLogin();return}
  if(path==='/login'){go('/dashboard');return}
  const redirects={'/my-visitors':'/visitor-requests','/export-control':'/reports','/reception':'/dashboard','/todays-visits':'/dashboard'}
  if(redirects[path]){history.replaceState({},'',redirects[path]+location.search);await render();return}
  shell();const main=document.getElementById('page')
  try{
    if(path.startsWith('/visitor-forms/')){if(user.role!=='HOST_REQUESTER')throw new Error('Only the requester can edit visitor information.');await visitorFormPage(main)}
    else if(path==='/'||path==='/dashboard')await dashboardPage(main)
    else if(path==='/visitor-requests')await requestsPage(main)
    else if(path==='/visitor-history'&&user.role==='HOST_REQUESTER')await analyticsPage(main,'Visitor History')
    else if(path==='/visitor-history')await requestsPage(main,'history')
    else if(path==='/visitor-requests/new')await createRequestPage(main)
    else if(/^\/visitor-requests\/[^/]+$/.test(path))await detailPage(main,path.split('/').pop())
    else if(path==='/pending-actions')await pendingPage(main)
    else if(path==='/reports'&&user.role==='HOST_REQUESTER'){history.replaceState({},'','/visitor-history');await analyticsPage(main,'Visitor History')}
    else if(path==='/reports')await analyticsPage(main,user.role==='RECEPTION'?'Security Reports':'Export Control Reports')
    else main.innerHTML=errorState('Page Not Found','The requested page does not exist or is not available for this role.')
  }catch(reason){if(token===viewToken)main.innerHTML=errorState('Unable to Load This Page',reason.message)}
}

document.addEventListener('click',event=>{const link=event.target.closest('a[data-route]');if(link){event.preventDefault();go(link.getAttribute('href'))}const refresh=event.target.closest('[data-render]');if(refresh)render();document.querySelectorAll('details.profile-menu[open],details.mobile-navigation[open]').forEach(item=>{if(!item.contains(event.target))item.removeAttribute('open')})})
document.addEventListener('keydown',event=>{if(event.key==='Escape'){closeModal();hideTooltip();document.querySelectorAll('details.profile-menu[open],details.mobile-navigation[open]').forEach(item=>item.removeAttribute('open'))}})
document.addEventListener('mouseover',event=>{const target=event.target.closest('[data-tooltip]');if(target)showTooltip(target)})
document.addEventListener('mouseout',event=>{if(event.target.closest('[data-tooltip]'))hideTooltip()})
document.addEventListener('focusin',event=>{const target=event.target.closest('[data-tooltip]');if(target)showTooltip(target)})
document.addEventListener('focusout',event=>{if(event.target.closest('[data-tooltip]'))hideTooltip()})
window.addEventListener('scroll',hideTooltip,true)
window.addEventListener('resize',hideTooltip)
window.addEventListener('popstate',render)
render()
</script>
</body>
</html>""".strip("\n")


def iso_now() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def new_id(prefix: str) -> str:
    return f"{prefix}-{uuid.uuid4().hex[:12]}"


def normalize_text(value: object) -> str:
    return "" if value is None else str(value)


def normalize_bytes(value: bytes | memoryview) -> bytes:
    return bytes(value)


def serialize_json(value: object) -> str:
    return json.dumps(value, ensure_ascii=False, separators=(",", ":"))


def deserialize_json(value: object) -> object:
    return json.loads(str(value))


USERS = [
    {"id": "role-host", "name": "Host/Requester", "role": "HOST_REQUESTER"},
    {"id": "role-export-control", "name": "Export Control", "role": "EXPORT_CONTROL"},
    {"id": "role-security-reception", "name": "Security/Reception", "role": "RECEPTION"},
]
ID_TYPES = {"Driver's License", "Passport", "Voter ID", "Aadhaar Card", "PAN Card", "Other Government Issued ID"}
PURPOSE_TYPES = {"Technical", "Non-Technical", "Others"}
COUNTRIES = [
    {"name": "Afghanistan", "dial": "93"},
    {"name": "Albania", "dial": "355"},
    {"name": "Algeria", "dial": "213"},
    {"name": "Andorra", "dial": "376"},
    {"name": "Angola", "dial": "244"},
    {"name": "Antigua and Barbuda", "dial": "1268"},
    {"name": "Argentina", "dial": "54"},
    {"name": "Armenia", "dial": "374"},
    {"name": "Australia", "dial": "61"},
    {"name": "Austria", "dial": "43"},
    {"name": "Azerbaijan", "dial": "994"},
    {"name": "Bahamas", "dial": "1242"},
    {"name": "Bahrain", "dial": "973"},
    {"name": "Bangladesh", "dial": "880"},
    {"name": "Barbados", "dial": "1246"},
    {"name": "Belarus", "dial": "375"},
    {"name": "Belgium", "dial": "32"},
    {"name": "Belize", "dial": "501"},
    {"name": "Benin", "dial": "229"},
    {"name": "Bhutan", "dial": "975"},
    {"name": "Bolivia", "dial": "591"},
    {"name": "Bosnia and Herzegovina", "dial": "387"},
    {"name": "Botswana", "dial": "267"},
    {"name": "Brazil", "dial": "55"},
    {"name": "Brunei", "dial": "673"},
    {"name": "Bulgaria", "dial": "359"},
    {"name": "Burkina Faso", "dial": "226"},
    {"name": "Burundi", "dial": "257"},
    {"name": "Cabo Verde", "dial": "238"},
    {"name": "Cambodia", "dial": "855"},
    {"name": "Cameroon", "dial": "237"},
    {"name": "Canada", "dial": "1"},
    {"name": "Central African Republic", "dial": "236"},
    {"name": "Chad", "dial": "235"},
    {"name": "Chile", "dial": "56"},
    {"name": "China", "dial": "86"},
    {"name": "Colombia", "dial": "57"},
    {"name": "Comoros", "dial": "269"},
    {"name": "Congo, Democratic Republic of the", "dial": "243"},
    {"name": "Congo, Republic of the", "dial": "242"},
    {"name": "Costa Rica", "dial": "506"},
    {"name": "Côte d'Ivoire", "dial": "225"},
    {"name": "Croatia", "dial": "385"},
    {"name": "Cuba", "dial": "53"},
    {"name": "Cyprus", "dial": "357"},
    {"name": "Czechia", "dial": "420"},
    {"name": "Denmark", "dial": "45"},
    {"name": "Djibouti", "dial": "253"},
    {"name": "Dominica", "dial": "1767"},
    {"name": "Dominican Republic", "dial": "1809"},
    {"name": "Ecuador", "dial": "593"},
    {"name": "Egypt", "dial": "20"},
    {"name": "El Salvador", "dial": "503"},
    {"name": "Equatorial Guinea", "dial": "240"},
    {"name": "Eritrea", "dial": "291"},
    {"name": "Estonia", "dial": "372"},
    {"name": "Eswatini", "dial": "268"},
    {"name": "Ethiopia", "dial": "251"},
    {"name": "Fiji", "dial": "679"},
    {"name": "Finland", "dial": "358"},
    {"name": "France", "dial": "33"},
    {"name": "Gabon", "dial": "241"},
    {"name": "Gambia", "dial": "220"},
    {"name": "Georgia", "dial": "995"},
    {"name": "Germany", "dial": "49"},
    {"name": "Ghana", "dial": "233"},
    {"name": "Greece", "dial": "30"},
    {"name": "Grenada", "dial": "1473"},
    {"name": "Guatemala", "dial": "502"},
    {"name": "Guinea", "dial": "224"},
    {"name": "Guinea-Bissau", "dial": "245"},
    {"name": "Guyana", "dial": "592"},
    {"name": "Haiti", "dial": "509"},
    {"name": "Honduras", "dial": "504"},
    {"name": "Hungary", "dial": "36"},
    {"name": "Iceland", "dial": "354"},
    {"name": "India", "dial": "91"},
    {"name": "Indonesia", "dial": "62"},
    {"name": "Iran", "dial": "98"},
    {"name": "Iraq", "dial": "964"},
    {"name": "Ireland", "dial": "353"},
    {"name": "Israel", "dial": "972"},
    {"name": "Italy", "dial": "39"},
    {"name": "Jamaica", "dial": "1876"},
    {"name": "Japan", "dial": "81"},
    {"name": "Jordan", "dial": "962"},
    {"name": "Kazakhstan", "dial": "7"},
    {"name": "Kenya", "dial": "254"},
    {"name": "Kiribati", "dial": "686"},
    {"name": "Korea, North", "dial": "850"},
    {"name": "Korea, South", "dial": "82"},
    {"name": "Kosovo", "dial": "383"},
    {"name": "Kuwait", "dial": "965"},
    {"name": "Kyrgyzstan", "dial": "996"},
    {"name": "Laos", "dial": "856"},
    {"name": "Latvia", "dial": "371"},
    {"name": "Lebanon", "dial": "961"},
    {"name": "Lesotho", "dial": "266"},
    {"name": "Liberia", "dial": "231"},
    {"name": "Libya", "dial": "218"},
    {"name": "Liechtenstein", "dial": "423"},
    {"name": "Lithuania", "dial": "370"},
    {"name": "Luxembourg", "dial": "352"},
    {"name": "Madagascar", "dial": "261"},
    {"name": "Malawi", "dial": "265"},
    {"name": "Malaysia", "dial": "60"},
    {"name": "Maldives", "dial": "960"},
    {"name": "Mali", "dial": "223"},
    {"name": "Malta", "dial": "356"},
    {"name": "Marshall Islands", "dial": "692"},
    {"name": "Mauritania", "dial": "222"},
    {"name": "Mauritius", "dial": "230"},
    {"name": "Mexico", "dial": "52"},
    {"name": "Micronesia", "dial": "691"},
    {"name": "Moldova", "dial": "373"},
    {"name": "Monaco", "dial": "377"},
    {"name": "Mongolia", "dial": "976"},
    {"name": "Montenegro", "dial": "382"},
    {"name": "Morocco", "dial": "212"},
    {"name": "Mozambique", "dial": "258"},
    {"name": "Myanmar", "dial": "95"},
    {"name": "Namibia", "dial": "264"},
    {"name": "Nauru", "dial": "674"},
    {"name": "Nepal", "dial": "977"},
    {"name": "Netherlands", "dial": "31"},
    {"name": "New Zealand", "dial": "64"},
    {"name": "Nicaragua", "dial": "505"},
    {"name": "Niger", "dial": "227"},
    {"name": "Nigeria", "dial": "234"},
    {"name": "North Macedonia", "dial": "389"},
    {"name": "Norway", "dial": "47"},
    {"name": "Oman", "dial": "968"},
    {"name": "Pakistan", "dial": "92"},
    {"name": "Palau", "dial": "680"},
    {"name": "Palestine", "dial": "970"},
    {"name": "Panama", "dial": "507"},
    {"name": "Papua New Guinea", "dial": "675"},
    {"name": "Paraguay", "dial": "595"},
    {"name": "Peru", "dial": "51"},
    {"name": "Philippines", "dial": "63"},
    {"name": "Poland", "dial": "48"},
    {"name": "Portugal", "dial": "351"},
    {"name": "Qatar", "dial": "974"},
    {"name": "Romania", "dial": "40"},
    {"name": "Russia", "dial": "7"},
    {"name": "Rwanda", "dial": "250"},
    {"name": "Saint Kitts and Nevis", "dial": "1869"},
    {"name": "Saint Lucia", "dial": "1758"},
    {"name": "Saint Vincent and the Grenadines", "dial": "1784"},
    {"name": "Samoa", "dial": "685"},
    {"name": "San Marino", "dial": "378"},
    {"name": "São Tomé and Príncipe", "dial": "239"},
    {"name": "Saudi Arabia", "dial": "966"},
    {"name": "Senegal", "dial": "221"},
    {"name": "Serbia", "dial": "381"},
    {"name": "Seychelles", "dial": "248"},
    {"name": "Sierra Leone", "dial": "232"},
    {"name": "Singapore", "dial": "65"},
    {"name": "Slovakia", "dial": "421"},
    {"name": "Slovenia", "dial": "386"},
    {"name": "Solomon Islands", "dial": "677"},
    {"name": "Somalia", "dial": "252"},
    {"name": "South Africa", "dial": "27"},
    {"name": "South Sudan", "dial": "211"},
    {"name": "Spain", "dial": "34"},
    {"name": "Sri Lanka", "dial": "94"},
    {"name": "Sudan", "dial": "249"},
    {"name": "Suriname", "dial": "597"},
    {"name": "Sweden", "dial": "46"},
    {"name": "Switzerland", "dial": "41"},
    {"name": "Syria", "dial": "963"},
    {"name": "Taiwan", "dial": "886"},
    {"name": "Tajikistan", "dial": "992"},
    {"name": "Tanzania", "dial": "255"},
    {"name": "Thailand", "dial": "66"},
    {"name": "Timor-Leste", "dial": "670"},
    {"name": "Togo", "dial": "228"},
    {"name": "Tonga", "dial": "676"},
    {"name": "Trinidad and Tobago", "dial": "1868"},
    {"name": "Tunisia", "dial": "216"},
    {"name": "Türkiye", "dial": "90"},
    {"name": "Turkmenistan", "dial": "993"},
    {"name": "Tuvalu", "dial": "688"},
    {"name": "Uganda", "dial": "256"},
    {"name": "Ukraine", "dial": "380"},
    {"name": "United Arab Emirates", "dial": "971"},
    {"name": "United Kingdom", "dial": "44"},
    {"name": "United States", "dial": "1"},
    {"name": "Uruguay", "dial": "598"},
    {"name": "Uzbekistan", "dial": "998"},
    {"name": "Vanuatu", "dial": "678"},
    {"name": "Vatican City", "dial": "39"},
    {"name": "Venezuela", "dial": "58"},
    {"name": "Vietnam", "dial": "84"},
    {"name": "Yemen", "dial": "967"},
    {"name": "Zambia", "dial": "260"},
    {"name": "Zimbabwe", "dial": "263"},
]
COUNTRY_NAMES = {item["name"] for item in COUNTRIES}
COUNTRY_DIAL_CODES = {item["name"]: item["dial"] for item in COUNTRIES}
PHONE_LENGTHS = {
    "Australia": (9,),
    "Austria": tuple(range(4, 14)),
    "Bangladesh": (10,),
    "Belgium": (8, 9),
    "Brazil": (10, 11),
    "Canada": (10,),
    "China": tuple(range(7, 13)),
    "Denmark": (8,),
    "Finland": tuple(range(5, 13)),
    "France": (9,),
    "Germany": tuple(range(4, 14)),
    "Hong Kong": (8,),
    "India": (10,),
    "Indonesia": tuple(range(7, 13)),
    "Ireland": tuple(range(7, 10)),
    "Israel": (8, 9),
    "Italy": tuple(range(6, 12)),
    "Japan": (9, 10),
    "Malaysia": tuple(range(7, 11)),
    "Mexico": (10,),
    "Netherlands": (9,),
    "New Zealand": tuple(range(8, 11)),
    "Norway": (8,),
    "Pakistan": (10,),
    "Philippines": (10,),
    "Poland": (9,),
    "Portugal": (9,),
    "Qatar": (8,),
    "Saudi Arabia": (9,),
    "Singapore": (8,),
    "South Africa": (9,),
    "Korea, South": tuple(range(8, 11)),
    "Spain": (9,),
    "Sri Lanka": (9,),
    "Sweden": tuple(range(7, 11)),
    "Switzerland": (9,),
    "Taiwan": (9,),
    "Thailand": (8, 9),
    "Türkiye": (10,),
    "United Arab Emirates": (8, 9),
    "United Kingdom": tuple(range(7, 11)),
    "United States": (10,),
}


def phone_lengths(country: str) -> tuple[int, ...]:
    if country in PHONE_LENGTHS:
        return PHONE_LENGTHS[country]
    maximum = 15 - len(COUNTRY_DIAL_CODES[country])
    return tuple(range(6, maximum + 1))


REQUESTABLE_FIELDS = {
    "firstName": "First Name",
    "middleName": "Middle Name",
    "lastName": "Last Name",
    "designation": "Designation/Position Held",
    "citizenship": "Citizenship",
    "companyName": "Full Name of Visitor Company",
    "companyAddress": "Company Address",
    "officeCity": "Company City",
    "officeCountry": "Company Country",
    "phoneCountry": "Phone Country Code",
    "telephone": "Phone Number",
    "email": "Email Address",
    "idType": "ID Type",
    "otherIdType": "Government-Issued ID Type",
    "assets": "Declared Assets",
}


def split_full_name(value: str) -> tuple[str, str, str]:
    parts = str(value or "").split()
    if len(parts) < 2:
        return (parts[0] if parts else "", "", "")
    if len(parts) == 2:
        return parts[0], "", parts[1]
    return parts[0], " ".join(parts[1:-1]), parts[-1]


def joined_full_name(first_name: str, middle_name: str, last_name: str) -> str:
    return " ".join(part for part in (first_name.strip(), middle_name.strip(), last_name.strip()) if part)


def make_form(request_id: str, index: int, visitor: dict | None = None, status: str = "DRAFT") -> dict:
    source = visitor or {}
    legacy_first, legacy_middle, legacy_last = split_full_name(source.get("fullName", ""))
    first_name = source.get("firstName", legacy_first)
    middle_name = source.get("middleName", legacy_middle)
    last_name = source.get("lastName", legacy_last)
    return {
        "id": new_id("form"),
        "visitorRequestId": request_id,
        "status": status,
        "firstName": first_name,
        "middleName": middle_name,
        "lastName": last_name,
        "fullName": joined_full_name(first_name, middle_name, last_name),
        "citizenship": source.get("citizenship", source.get("nationality", "")),
        "designation": source.get("designation", ""),
        "companyName": source.get("companyName", ""),
        "companyAddress": source.get("companyAddress", ""),
        "officeCity": source.get("officeCity", ""),
        "officeCountry": source.get("officeCountry", ""),
        "phoneCountry": source.get("phoneCountry", ""),
        "phoneDialCode": source.get("phoneDialCode", ""),
        "telephone": source.get("telephone", ""),
        "email": source.get("email", ""),
        "idType": source.get("idType", ""),
        "otherIdType": source.get("otherIdType", ""),
        "assets": deepcopy(source.get("assets", [])),
        "screeningResult": deepcopy(source.get("screeningResult")),
        "receptionRecords": [],
        "versions": [],
        "ecDecision": source.get("ecDecision", ""),
        "idClassification": source.get("idClassification", ""),
        "ecDecisionReason": source.get("ecDecisionReason", ""),
        "ecDecisionAt": source.get("ecDecisionAt"),
        "sequence": index + 1,
    }


def make_reception_record(form_id: str, day: dict, status: str = "UPCOMING") -> dict:
    return {
        "id": new_id("visit"),
        "visitorFormId": form_id,
        "visitDayId": day["id"],
        "status": status,
        "actualArrivalTime": None,
        "actualDepartureTime": None,
        "badge": "",
        "badgeType": "",
        "badgeReturnedAt": None,
        "holdReason": "",
        "identityStatus": "PENDING",
        "identityHoldReason": "",
        "assetsStatus": "PENDING",
        "assetsHoldReason": "",
        "verificationRemarks": "",
    }


def reset_reception_records(request: dict) -> None:
    for form in request["visitorForms"]:
        form["receptionRecords"] = [make_reception_record(form["id"], day) for day in request["visitDays"]]


def ensure_request_shape(request: dict) -> dict:
    request.setdefault("screeningRemarks", [])
    request.setdefault("cancellationReason", "")
    request.setdefault("scheduleChanges", [])
    request.setdefault("hostDepartment", "")
    if request.get("visitPurposeType") == "Other":
        request["visitPurposeType"] = "Others"
    if request.get("idClassification") not in {"", "Vendor", "Visitor"}:
        request["idClassification"] = ""
    for form_index, form in enumerate(request["visitorForms"]):
        legacy_first, legacy_middle, legacy_last = split_full_name(form.get("fullName", ""))
        form.setdefault("firstName", legacy_first)
        form.setdefault("middleName", legacy_middle)
        form.setdefault("lastName", legacy_last)
        form["fullName"] = joined_full_name(form["firstName"], form["middleName"], form["lastName"])
        form.setdefault("screeningResult", None)
        if "citizenship" not in form:
            form["citizenship"] = form.pop("nationality", "")
        form.setdefault("phoneCountry", "")
        form.setdefault("phoneDialCode", "")
        if "ecDecision" not in form:
            form["ecDecision"] = "APPROVED" if request.get("currentStatus") in {"APPROVED", "VISIT_PROCESS_COMPLETED"} else "REJECTED" if request.get("currentStatus") == "REJECTED" else ""
        form.setdefault("idClassification", request.get("idClassification", "") if form["ecDecision"] in {"APPROVED", "REJECTED"} else "")
        form.setdefault("ecDecisionReason", request.get("rejectionReason", "") if form["ecDecision"] == "REJECTED" else "")
        form.setdefault("ecDecisionAt", request.get("approvedAt") if form["ecDecision"] == "APPROVED" else request.get("rejectedAt") if form["ecDecision"] == "REJECTED" else None)
        if "receptionRecords" not in form:
            form["receptionRecords"] = []
            for day in request["visitDays"]:
                inherited_status = day.get("status", "UPCOMING") if form_index == 0 else "UPCOMING"
                record = make_reception_record(form["id"], day, inherited_status)
                if form_index == 0:
                    record["actualArrivalTime"] = day.get("actualArrivalTime")
                    record["actualDepartureTime"] = day.get("actualDepartureTime")
                    record["badge"] = day.get("badge", "")
                form["receptionRecords"].append(record)
        for record in form["receptionRecords"]:
            record.setdefault("holdReason", "")
            record.setdefault("badgeReturnedAt", None)
            record.setdefault("badgeType", "")
            processed = record.get("status") in {"RECEPTION_VERIFICATION", "CHECKED_IN", "COMPLETED"}
            rejected = record.get("status") in {"RECEPTION_HOLD", "ENTRY_REJECTED"}
            if record.get("status") == "RECEPTION_HOLD":
                record["status"] = "ENTRY_REJECTED"
            record.setdefault("identityStatus", "REJECTED" if rejected else "APPROVED" if processed else "PENDING")
            record.setdefault("identityHoldReason", record.get("holdReason", "") if rejected else "")
            record.setdefault("assetsStatus", "APPROVED" if processed else "PENDING")
            record.setdefault("assetsHoldReason", "")
            record.setdefault("verificationRemarks", record.get("holdReason", "") if rejected else "")
    default_form = request["visitorForms"][0]
    for item in request.get("informationRequests", []):
        item.setdefault("visitorFormId", default_form["id"])
        item.setdefault("visitorName", default_form["fullName"] or "Visitor 1")
        if not isinstance(item.get("fields"), list):
            item["fields"] = ["firstName", "lastName"]
        item["fields"] = ["citizenship" if field_name == "nationality" else field_name for field_name in item["fields"]]
        if "fullName" in item["fields"]:
            item["fields"] = [field_name for field_name in item["fields"] if field_name != "fullName"] + ["firstName", "middleName", "lastName"]
            original_name = item.get("originalValues", {}).pop("fullName", default_form["fullName"])
            first_name, middle_name, last_name = split_full_name(original_name)
            item.setdefault("originalValues", {}).update({"firstName": first_name, "middleName": middle_name, "lastName": last_name})
        item["fields"] = list(dict.fromkeys(item["fields"]))
        if "nationality" in item.get("originalValues", {}) and "citizenship" not in item["originalValues"]:
            item["originalValues"]["citizenship"] = item["originalValues"].pop("nationality")
        item["fieldLabels"] = [REQUESTABLE_FIELDS.get(field_name, field_name) for field_name in item["fields"]]
        item.setdefault("originalValues", {field_name: deepcopy(default_form.get(field_name, "")) for field_name in item["fields"]})
        item.setdefault("changes", [])
        for change in item["changes"]:
            if change.get("field") in {"nationality", "Nationality"}:
                change["field"] = "Citizenship"
        item.setdefault("respondedAt", None)
    return request


def database_connection() -> sqlite3.Connection:
    connection = sqlite3.connect(DATABASE_PATH, timeout=10)
    connection.row_factory = sqlite3.Row
    connection.execute("PRAGMA foreign_keys = ON")
    return connection


def ensure_column(connection: sqlite3.Connection, table: str, column: str, declaration: str) -> None:
    columns = {row["name"] for row in connection.execute(f"PRAGMA table_info({table})")}
    if column not in columns:
        connection.execute(f"ALTER TABLE {table} ADD COLUMN {column} {declaration}")


def migrate_request_numbers(connection: sqlite3.Connection) -> None:
    rows = list(connection.execute("SELECT id, request_number, created_at FROM requests ORDER BY created_at, id"))
    pattern = re.compile(r"^V-(\d{4})-([1-9][0-9]*)$")
    counters: dict[int, int] = {}
    if any(pattern.fullmatch(row["request_number"]) is None for row in rows):
        for row in rows:
            connection.execute("UPDATE requests SET request_number = ? WHERE id = ?", (f"__migrating__{row['id']}", row["id"]))
        for row in rows:
            match = re.match(r"^(\d{4})", row["created_at"] or "")
            year = int(match.group(1)) if match else datetime.now(IST).year
            counters[year] = counters.get(year, 0) + 1
            connection.execute("UPDATE requests SET request_number = ? WHERE id = ?", (f"V-{year}-{counters[year]}", row["id"]))
    else:
        for row in rows:
            match = pattern.fullmatch(row["request_number"])
            if match:
                year, number = int(match.group(1)), int(match.group(2))
                counters[year] = max(counters.get(year, 0), number)
    for year, number in counters.items():
        key = f"request_sequence_{year}"
        connection.execute("INSERT INTO app_meta (key, value) VALUES (?, ?) ON CONFLICT(key) DO UPDATE SET value = MAX(value, excluded.value)", (key, number))


def initialize_database() -> None:
    DATABASE_PATH.parent.mkdir(parents=True, exist_ok=True)
    with database_connection() as connection:
        connection.execute("PRAGMA journal_mode = WAL")
        connection.executescript("""
            CREATE TABLE IF NOT EXISTS app_meta (key TEXT PRIMARY KEY, value INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS requests (
                id TEXT PRIMARY KEY,
                request_number TEXT NOT NULL UNIQUE,
                requester_id TEXT NOT NULL,
                main_host_id TEXT NOT NULL,
                main_host_name TEXT NOT NULL,
                host_department TEXT NOT NULL DEFAULT '',
                escorting_host_id TEXT NOT NULL,
                escorting_host_name TEXT NOT NULL,
                visitor_type TEXT NOT NULL CHECK (visitor_type IN ('Internal','External')),
                facilities_contractor INTEGER NOT NULL CHECK (facilities_contractor IN (0,1)),
                research_contractor INTEGER NOT NULL CHECK (research_contractor IN (0,1)),
                visiting_site TEXT NOT NULL CHECK (visiting_site IN ('Bengaluru','Delhi')),
                visitor_count INTEGER NOT NULL CHECK (visitor_count BETWEEN 1 AND 20),
                purpose TEXT NOT NULL,
                areas_to_visit TEXT NOT NULL,
                purpose_type TEXT NOT NULL CHECK (purpose_type IN ('Technical','Non-Technical','Others')),
                legacy_classification TEXT NOT NULL,
                status TEXT NOT NULL,
                visit_start TEXT NOT NULL,
                visit_end TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                submitted_at TEXT,
                approved_at TEXT,
                rejected_at TEXT,
                rejection_reason TEXT NOT NULL,
                cancellation_reason TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_requests_requester ON requests (requester_id, created_at DESC);
            CREATE INDEX IF NOT EXISTS ix_requests_status ON requests (status, created_at DESC);
            CREATE TABLE IF NOT EXISTS visitor_forms (
                id TEXT PRIMARY KEY,
                request_id TEXT NOT NULL REFERENCES requests(id) ON DELETE CASCADE,
                sequence INTEGER NOT NULL,
                status TEXT NOT NULL,
                full_name TEXT NOT NULL,
                first_name TEXT NOT NULL DEFAULT '',
                middle_name TEXT NOT NULL DEFAULT '',
                last_name TEXT NOT NULL DEFAULT '',
                citizenship TEXT NOT NULL,
                designation TEXT NOT NULL,
                company_name TEXT NOT NULL,
                company_address TEXT NOT NULL,
                office_city TEXT NOT NULL,
                office_country TEXT NOT NULL,
                phone_country TEXT NOT NULL,
                phone_dial_code TEXT NOT NULL,
                telephone TEXT NOT NULL,
                email TEXT NOT NULL,
                id_type TEXT NOT NULL,
                other_id_type TEXT NOT NULL,
                ec_decision TEXT NOT NULL CHECK (ec_decision IN ('','APPROVED','REJECTED')),
                classification TEXT NOT NULL CHECK (classification IN ('','Vendor','Visitor')),
                ec_decision_reason TEXT NOT NULL,
                ec_decision_at TEXT,
                UNIQUE (request_id, sequence)
            );
            CREATE INDEX IF NOT EXISTS ix_visitor_forms_request ON visitor_forms (request_id, sequence);
            CREATE TABLE IF NOT EXISTS visit_days (
                id TEXT PRIMARY KEY,
                request_id TEXT NOT NULL REFERENCES requests(id) ON DELETE CASCADE,
                visit_date TEXT NOT NULL,
                arrival_time TEXT NOT NULL,
                departure_time TEXT NOT NULL,
                UNIQUE (request_id, visit_date)
            );
            CREATE TABLE IF NOT EXISTS assets (
                id TEXT PRIMARY KEY,
                visitor_form_id TEXT NOT NULL REFERENCES visitor_forms(id) ON DELETE CASCADE,
                position INTEGER NOT NULL,
                asset_type TEXT NOT NULL,
                description TEXT NOT NULL,
                serial_number TEXT NOT NULL,
                verification_status TEXT NOT NULL,
                UNIQUE (visitor_form_id, position)
            );
            CREATE TABLE IF NOT EXISTS reception_records (
                id TEXT PRIMARY KEY,
                visitor_form_id TEXT NOT NULL REFERENCES visitor_forms(id) ON DELETE CASCADE,
                visit_day_id TEXT NOT NULL REFERENCES visit_days(id) ON DELETE CASCADE,
                status TEXT NOT NULL,
                actual_arrival_time TEXT,
                actual_departure_time TEXT,
                badge TEXT NOT NULL,
                badge_type TEXT NOT NULL DEFAULT '',
                badge_returned_at TEXT,
                hold_reason TEXT NOT NULL,
                identity_status TEXT NOT NULL DEFAULT 'PENDING',
                identity_hold_reason TEXT NOT NULL DEFAULT '',
                assets_status TEXT NOT NULL DEFAULT 'PENDING',
                assets_hold_reason TEXT NOT NULL DEFAULT '',
                verification_remarks TEXT NOT NULL DEFAULT '',
                UNIQUE (visitor_form_id, visit_day_id)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ux_active_badges ON reception_records (badge COLLATE NOCASE) WHERE badge <> '' AND status = 'CHECKED_IN';
            CREATE TABLE IF NOT EXISTS form_versions (
                id TEXT PRIMARY KEY,
                visitor_form_id TEXT NOT NULL REFERENCES visitor_forms(id) ON DELETE CASCADE,
                version INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                snapshot TEXT NOT NULL,
                UNIQUE (visitor_form_id, version)
            );
            CREATE TABLE IF NOT EXISTS denied_party_screening_results (
                visitor_form_id TEXT PRIMARY KEY REFERENCES visitor_forms(id) ON DELETE CASCADE,
                file_name TEXT NOT NULL,
                content_type TEXT NOT NULL CHECK (content_type = 'application/pdf'),
                file_size INTEGER NOT NULL CHECK (file_size BETWEEN 1 AND 10485760),
                content BLOB NOT NULL,
                uploaded_by TEXT NOT NULL,
                uploaded_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS screening_remarks (
                id TEXT PRIMARY KEY,
                request_id TEXT NOT NULL REFERENCES requests(id) ON DELETE CASCADE,
                visitor_form_id TEXT NOT NULL REFERENCES visitor_forms(id) ON DELETE CASCADE,
                remark TEXT NOT NULL,
                created_by TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ec_reviews (
                id TEXT PRIMARY KEY,
                request_id TEXT NOT NULL REFERENCES requests(id) ON DELETE CASCADE,
                reviewer_id TEXT NOT NULL,
                status TEXT NOT NULL,
                decision TEXT NOT NULL,
                comments TEXT NOT NULL,
                reviewed_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ec_review_targets (
                review_id TEXT NOT NULL REFERENCES ec_reviews(id) ON DELETE CASCADE,
                visitor_form_id TEXT NOT NULL REFERENCES visitor_forms(id) ON DELETE CASCADE,
                position INTEGER NOT NULL,
                PRIMARY KEY (review_id, visitor_form_id)
            );
            CREATE TABLE IF NOT EXISTS information_requests (
                id TEXT PRIMARY KEY,
                request_id TEXT NOT NULL REFERENCES requests(id) ON DELETE CASCADE,
                visitor_form_id TEXT NOT NULL REFERENCES visitor_forms(id) ON DELETE CASCADE,
                comment TEXT NOT NULL,
                status TEXT NOT NULL CHECK (status IN ('PENDING','RESOLVED')),
                created_at TEXT NOT NULL,
                responded_at TEXT
            );
            CREATE TABLE IF NOT EXISTS information_request_fields (
                information_request_id TEXT NOT NULL REFERENCES information_requests(id) ON DELETE CASCADE,
                position INTEGER NOT NULL,
                field_name TEXT NOT NULL,
                original_value TEXT NOT NULL,
                PRIMARY KEY (information_request_id, field_name)
            );
            CREATE TABLE IF NOT EXISTS information_changes (
                information_request_id TEXT NOT NULL REFERENCES information_requests(id) ON DELETE CASCADE,
                position INTEGER NOT NULL,
                field_name TEXT NOT NULL,
                before_value TEXT NOT NULL,
                after_value TEXT NOT NULL,
                PRIMARY KEY (information_request_id, position)
            );
            CREATE TABLE IF NOT EXISTS schedule_changes (
                id TEXT PRIMARY KEY,
                request_id TEXT NOT NULL REFERENCES requests(id) ON DELETE CASCADE,
                old_start TEXT NOT NULL,
                old_end TEXT NOT NULL,
                new_start TEXT NOT NULL,
                new_end TEXT NOT NULL,
                reason TEXT NOT NULL,
                changed_by TEXT NOT NULL,
                changed_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS audit_events (
                id TEXT PRIMARY KEY,
                request_id TEXT NOT NULL REFERENCES requests(id) ON DELETE RESTRICT,
                action TEXT NOT NULL,
                details TEXT NOT NULL,
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_audit_events_request ON audit_events (request_id, created_at DESC);
            CREATE TRIGGER IF NOT EXISTS audit_events_no_update BEFORE UPDATE ON audit_events BEGIN SELECT RAISE(ABORT, 'Audit events are immutable'); END;
            CREATE TRIGGER IF NOT EXISTS audit_events_no_delete BEFORE DELETE ON audit_events BEGIN SELECT RAISE(ABORT, 'Audit events are immutable'); END;
            CREATE TRIGGER IF NOT EXISTS schedule_changes_no_update BEFORE UPDATE ON schedule_changes BEGIN SELECT RAISE(ABORT, 'Schedule history is immutable'); END;
            CREATE TRIGGER IF NOT EXISTS schedule_changes_no_delete BEFORE DELETE ON schedule_changes BEGIN SELECT RAISE(ABORT, 'Schedule history is immutable'); END;
        """)
        form_columns = {row["name"] for row in connection.execute("PRAGMA table_info(visitor_forms)")}
        if "nationality" in form_columns and "citizenship" not in form_columns:
            connection.execute("ALTER TABLE visitor_forms RENAME COLUMN nationality TO citizenship")
        ensure_column(connection, "requests", "host_department", "TEXT NOT NULL DEFAULT ''")
        ensure_column(connection, "visitor_forms", "first_name", "TEXT NOT NULL DEFAULT ''")
        ensure_column(connection, "visitor_forms", "middle_name", "TEXT NOT NULL DEFAULT ''")
        ensure_column(connection, "visitor_forms", "last_name", "TEXT NOT NULL DEFAULT ''")
        ensure_column(connection, "reception_records", "badge_type", "TEXT NOT NULL DEFAULT ''")
        ensure_column(connection, "reception_records", "identity_status", "TEXT NOT NULL DEFAULT 'PENDING'")
        ensure_column(connection, "reception_records", "identity_hold_reason", "TEXT NOT NULL DEFAULT ''")
        ensure_column(connection, "reception_records", "assets_status", "TEXT NOT NULL DEFAULT 'PENDING'")
        ensure_column(connection, "reception_records", "assets_hold_reason", "TEXT NOT NULL DEFAULT ''")
        ensure_column(connection, "reception_records", "verification_remarks", "TEXT NOT NULL DEFAULT ''")
        for row in connection.execute("SELECT id, full_name, first_name, middle_name, last_name FROM visitor_forms"):
            if not row["first_name"] and not row["last_name"]:
                first_name, middle_name, last_name = split_full_name(row["full_name"])
                connection.execute("UPDATE visitor_forms SET first_name = ?, middle_name = ?, last_name = ? WHERE id = ?", (first_name, middle_name, last_name, row["id"]))
        connection.execute("UPDATE reception_records SET identity_status = 'APPROVED', assets_status = 'APPROVED' WHERE status IN ('RECEPTION_VERIFICATION','CHECKED_IN','COMPLETED') AND (identity_status = 'PENDING' OR assets_status = 'PENDING')")
        connection.execute("UPDATE reception_records SET status = 'ENTRY_REJECTED', identity_status = 'REJECTED', assets_status = 'REJECTED', identity_hold_reason = hold_reason, assets_hold_reason = hold_reason, verification_remarks = hold_reason WHERE status = 'RECEPTION_HOLD'")
        connection.execute("UPDATE information_request_fields SET field_name = 'citizenship' WHERE field_name = 'nationality'")
        connection.execute("UPDATE information_changes SET field_name = 'citizenship' WHERE field_name = 'nationality'")
        connection.execute("UPDATE information_changes SET field_name = 'Citizenship' WHERE field_name = 'Nationality'")
        connection.execute("INSERT OR IGNORE INTO app_meta (key, value) VALUES ('request_sequence', 0)")
        migrated = connection.execute("SELECT value FROM app_meta WHERE key = 'legacy_payload_migrated'").fetchone()
        legacy_table = connection.execute("SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'visitor_requests'").fetchone()
        if migrated is None and legacy_table:
            columns = {row["name"] for row in connection.execute("PRAGMA table_info(visitor_requests)")}
            if "payload" in columns:
                for row in connection.execute("SELECT payload FROM visitor_requests"):
                    _persist_request(connection, ensure_request_shape(json.loads(row["payload"])))
        connection.execute("INSERT OR REPLACE INTO app_meta (key, value) VALUES ('legacy_payload_migrated', 1)")
        migrate_request_numbers(connection)
    DATABASE_PATH.chmod(0o600)


def _persist_request(connection: sqlite3.Connection, request: dict) -> None:
    request = ensure_request_shape(request)
    columns = ["id", "request_number", "requester_id", "main_host_id", "main_host_name", "host_department", "escorting_host_id", "escorting_host_name", "visitor_type", "facilities_contractor", "research_contractor", "visiting_site", "visitor_count", "purpose", "areas_to_visit", "purpose_type", "legacy_classification", "status", "visit_start", "visit_end", "created_at", "updated_at", "submitted_at", "approved_at", "rejected_at", "rejection_reason", "cancellation_reason"]
    values = [request["id"], request["requestNumber"], request["requesterId"], request.get("mainHostId", request["requesterId"]), normalize_text(request["mainHostName"]), normalize_text(request.get("hostDepartment", "")), request.get("escortingHostId", ""), normalize_text(request["escortingHostName"]), request["visitorType"], int(request["faculty"]), int(request["gtr"]), request["visitingSite"], request["numberOfVisitors"], normalize_text(request["purpose"]), normalize_text(request["areasToVisit"]), request["visitPurposeType"], request.get("idClassification", ""), request["currentStatus"], request["visitStart"], request["visitEnd"], request["createdAt"], request["updatedAt"], request.get("submittedAt"), request.get("approvedAt"), request.get("rejectedAt"), normalize_text(request.get("rejectionReason", "")), normalize_text(request.get("cancellationReason", ""))]
    if "batch_id" in {row["name"] for row in connection.execute("PRAGMA table_info(requests)")}:
        columns.insert(2, "batch_id")
        values.insert(2, request["id"])
    assignments = ",".join(f"{column}=excluded.{column}" for column in columns[1:])
    connection.execute(f"INSERT INTO requests ({','.join(columns)}) VALUES ({','.join('?' for _ in columns)}) ON CONFLICT(id) DO UPDATE SET {assignments}", values)
    connection.execute("DELETE FROM visitor_forms WHERE request_id = ?", (request["id"],))
    connection.execute("DELETE FROM visit_days WHERE request_id = ?", (request["id"],))
    connection.execute("DELETE FROM ec_reviews WHERE request_id = ?", (request["id"],))
    for day in request["visitDays"]:
        connection.execute("INSERT INTO visit_days (id,request_id,visit_date,arrival_time,departure_time) VALUES (?,?,?,?,?)", (day["id"], request["id"], day["visitDate"], day["expectedArrivalTime"], day["expectedDepartureTime"]))
    for form in request["visitorForms"]:
        connection.execute("INSERT INTO visitor_forms (id,request_id,sequence,status,full_name,first_name,middle_name,last_name,citizenship,designation,company_name,company_address,office_city,office_country,phone_country,phone_dial_code,telephone,email,id_type,other_id_type,ec_decision,classification,ec_decision_reason,ec_decision_at) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", (form["id"], request["id"], form["sequence"], form["status"], normalize_text(form["fullName"]), normalize_text(form["firstName"]), normalize_text(form["middleName"]), normalize_text(form["lastName"]), normalize_text(form["citizenship"]), normalize_text(form["designation"]), normalize_text(form["companyName"]), normalize_text(form["companyAddress"]), normalize_text(form["officeCity"]), normalize_text(form["officeCountry"]), normalize_text(form["phoneCountry"]), normalize_text(form["phoneDialCode"]), normalize_text(form["telephone"]), normalize_text(form["email"]), normalize_text(form["idType"]), normalize_text(form["otherIdType"]), form["ecDecision"], form["idClassification"], normalize_text(form["ecDecisionReason"]), form["ecDecisionAt"]))
        for position, asset in enumerate(form["assets"]):
            connection.execute("INSERT INTO assets (id,visitor_form_id,position,asset_type,description,serial_number,verification_status) VALUES (?,?,?,?,?,?,?)", (asset["id"], form["id"], position, normalize_text(asset["assetType"]), normalize_text(asset["description"]), normalize_text(asset["serialNumber"]), asset["verificationStatus"]))
        for record in form["receptionRecords"]:
            connection.execute("INSERT INTO reception_records (id,visitor_form_id,visit_day_id,status,actual_arrival_time,actual_departure_time,badge,badge_type,badge_returned_at,hold_reason,identity_status,identity_hold_reason,assets_status,assets_hold_reason,verification_remarks) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", (record["id"], form["id"], record["visitDayId"], record["status"], normalize_text(record["actualArrivalTime"]), normalize_text(record["actualDepartureTime"]), normalize_text(record["badge"]), record.get("badgeType", ""), normalize_text(record.get("badgeReturnedAt")), normalize_text(record["holdReason"]), record["identityStatus"], normalize_text(record["identityHoldReason"]), record["assetsStatus"], normalize_text(record["assetsHoldReason"]), normalize_text(record.get("verificationRemarks", ""))))
        for version in form["versions"]:
            connection.execute("INSERT INTO form_versions (id,visitor_form_id,version,created_at,snapshot) VALUES (?,?,?,?,?)", (version.get("id", new_id("version")), form["id"], version["version"], version["createdAt"], serialize_json(version["snapshot"])))
        screening_result = form.get("screeningResult")
        if screening_result:
            connection.execute("INSERT INTO denied_party_screening_results (visitor_form_id,file_name,content_type,file_size,content,uploaded_by,uploaded_at) VALUES (?,?,?,?,?,?,?)", (form["id"], screening_result["fileName"], screening_result["contentType"], screening_result["fileSize"], normalize_bytes(screening_result["content"]), normalize_text(screening_result["uploadedBy"]), screening_result["uploadedAt"]))
    form_ids = {form["id"] for form in request["visitorForms"]}
    for item in request["screeningRemarks"]:
        if item["visitorFormId"] in form_ids:
            connection.execute("INSERT INTO screening_remarks (id,request_id,visitor_form_id,remark,created_by,created_at) VALUES (?,?,?,?,?,?)", (item["id"], request["id"], item["visitorFormId"], normalize_text(item["remark"]), normalize_text(item["createdBy"]), item["createdAt"]))
    for review in request["ecReviews"]:
        connection.execute("INSERT INTO ec_reviews (id,request_id,reviewer_id,status,decision,comments,reviewed_at) VALUES (?,?,?,?,?,?,?)", (review["id"], request["id"], normalize_text(review["reviewerId"]), review["status"], review["decision"], normalize_text(review["comments"]), review["reviewedAt"]))
        for position, form_id in enumerate(review.get("visitorFormIds", [])):
            if form_id in form_ids:
                connection.execute("INSERT INTO ec_review_targets (review_id,visitor_form_id,position) VALUES (?,?,?)", (review["id"], form_id, position))
    for item in request["informationRequests"]:
        if item["visitorFormId"] not in form_ids:
            continue
        connection.execute("INSERT INTO information_requests (id,request_id,visitor_form_id,comment,status,created_at,responded_at) VALUES (?,?,?,?,?,?,?)", (item["id"], request["id"], item["visitorFormId"], normalize_text(item["comment"]), item["status"], item["createdAt"], item.get("respondedAt")))
        for position, field_name in enumerate(item["fields"]):
            connection.execute("INSERT INTO information_request_fields (information_request_id,position,field_name,original_value) VALUES (?,?,?,?)", (item["id"], position, field_name, serialize_json(item["originalValues"].get(field_name, ""))))
        for position, change in enumerate(item["changes"]):
            connection.execute("INSERT INTO information_changes (information_request_id,position,field_name,before_value,after_value) VALUES (?,?,?,?,?)", (item["id"], position, change["field"], serialize_json(change["before"]), serialize_json(change["after"])))
    for item in request["scheduleChanges"]:
        connection.execute("INSERT OR IGNORE INTO schedule_changes (id,request_id,old_start,old_end,new_start,new_end,reason,changed_by,changed_at) VALUES (?,?,?,?,?,?,?,?,?)", (item["id"], request["id"], item["oldStart"], item["oldEnd"], item["newStart"], item["newEnd"], normalize_text(item["reason"]), normalize_text(item["changedBy"]), item["changedAt"]))
    for item in request["auditHistory"]:
        connection.execute("INSERT OR IGNORE INTO audit_events (id,request_id,action,details,created_at) VALUES (?,?,?,?,?)", (item["id"], request["id"], item["action"], normalize_text(item["details"]), item["createdAt"]))


def _load_requests(connection: sqlite3.Connection, request_id: str | None = None) -> list[dict]:
    query = "SELECT * FROM requests"
    parameters: tuple = ()
    if request_id is not None:
        query += " WHERE id = ?"
        parameters = (request_id,)
    query += " ORDER BY created_at DESC"
    results = []
    for row in connection.execute(query, parameters):
        request = {"id":row["id"],"requestNumber":row["request_number"],"requesterId":row["requester_id"],"mainHostId":row["main_host_id"],"mainHostName":normalize_text(row["main_host_name"]),"hostDepartment":normalize_text(row["host_department"]),"escortingHostId":row["escorting_host_id"],"escortingHostName":normalize_text(row["escorting_host_name"]),"visitorType":row["visitor_type"],"faculty":bool(row["facilities_contractor"]),"gtr":bool(row["research_contractor"]),"visitingSite":row["visiting_site"],"numberOfVisitors":row["visitor_count"],"purpose":normalize_text(row["purpose"]),"areasToVisit":normalize_text(row["areas_to_visit"]),"visitPurposeType":row["purpose_type"],"idClassification":row["legacy_classification"],"currentStatus":row["status"],"visitStart":row["visit_start"],"visitEnd":row["visit_end"],"createdAt":row["created_at"],"updatedAt":row["updated_at"],"submittedAt":row["submitted_at"],"approvedAt":row["approved_at"],"rejectedAt":row["rejected_at"],"rejectionReason":normalize_text(row["rejection_reason"]),"cancellationReason":normalize_text(row["cancellation_reason"]),"visitorForms":[],"visitDays":[],"screeningRemarks":[],"ecReviews":[],"comments":[],"informationRequests":[],"scheduleChanges":[],"auditHistory":[]}
        for day in connection.execute("SELECT * FROM visit_days WHERE request_id = ? ORDER BY visit_date", (request["id"],)):
            request["visitDays"].append({"id":day["id"],"visitDate":day["visit_date"],"expectedArrivalTime":day["arrival_time"],"expectedDepartureTime":day["departure_time"]})
        for form_row in connection.execute("SELECT * FROM visitor_forms WHERE request_id = ? ORDER BY sequence", (request["id"],)):
            form = {"id":form_row["id"],"visitorRequestId":request["id"],"sequence":form_row["sequence"],"status":form_row["status"],"fullName":normalize_text(form_row["full_name"]),"firstName":normalize_text(form_row["first_name"]),"middleName":normalize_text(form_row["middle_name"]),"lastName":normalize_text(form_row["last_name"]),"citizenship":normalize_text(form_row["citizenship"]),"designation":normalize_text(form_row["designation"]),"companyName":normalize_text(form_row["company_name"]),"companyAddress":normalize_text(form_row["company_address"]),"officeCity":normalize_text(form_row["office_city"]),"officeCountry":normalize_text(form_row["office_country"]),"phoneCountry":normalize_text(form_row["phone_country"]),"phoneDialCode":normalize_text(form_row["phone_dial_code"]),"telephone":normalize_text(form_row["telephone"]),"email":normalize_text(form_row["email"]),"idType":normalize_text(form_row["id_type"]),"otherIdType":normalize_text(form_row["other_id_type"]),"ecDecision":form_row["ec_decision"],"idClassification":form_row["classification"],"ecDecisionReason":normalize_text(form_row["ec_decision_reason"]),"ecDecisionAt":form_row["ec_decision_at"],"assets":[],"receptionRecords":[],"versions":[],"screeningResult":None}
            for asset in connection.execute("SELECT * FROM assets WHERE visitor_form_id = ? ORDER BY position", (form["id"],)):
                form["assets"].append({"id":asset["id"],"assetType":normalize_text(asset["asset_type"]),"description":normalize_text(asset["description"]),"serialNumber":normalize_text(asset["serial_number"]),"verificationStatus":asset["verification_status"]})
            for record in connection.execute("SELECT * FROM reception_records WHERE visitor_form_id = ?", (form["id"],)):
                form["receptionRecords"].append({"id":record["id"],"visitorFormId":form["id"],"visitDayId":record["visit_day_id"],"status":record["status"],"actualArrivalTime":normalize_text(record["actual_arrival_time"]),"actualDepartureTime":normalize_text(record["actual_departure_time"]),"badge":normalize_text(record["badge"]),"badgeType":record["badge_type"],"badgeReturnedAt":normalize_text(record["badge_returned_at"]),"holdReason":normalize_text(record["hold_reason"]),"identityStatus":record["identity_status"],"identityHoldReason":normalize_text(record["identity_hold_reason"]),"assetsStatus":record["assets_status"],"assetsHoldReason":normalize_text(record["assets_hold_reason"]),"verificationRemarks":normalize_text(record["verification_remarks"])})
            for version in connection.execute("SELECT * FROM form_versions WHERE visitor_form_id = ? ORDER BY version", (form["id"],)):
                snapshot = deserialize_json(version["snapshot"])
                if "citizenship" not in snapshot:
                    snapshot["citizenship"] = snapshot.pop("nationality", "")
                form["versions"].append({"id":version["id"],"version":version["version"],"createdAt":version["created_at"],"snapshot":snapshot})
            screening_result = connection.execute("SELECT * FROM denied_party_screening_results WHERE visitor_form_id = ?", (form["id"],)).fetchone()
            if screening_result:
                form["screeningResult"] = {"fileName":screening_result["file_name"],"contentType":screening_result["content_type"],"fileSize":screening_result["file_size"],"content":normalize_bytes(screening_result["content"]),"uploadedBy":normalize_text(screening_result["uploaded_by"]),"uploadedAt":screening_result["uploaded_at"]}
            request["visitorForms"].append(form)
        names = {form["id"]: form["fullName"] or f"Visitor {form['sequence']}" for form in request["visitorForms"]}
        for item in connection.execute("SELECT * FROM screening_remarks WHERE request_id = ? ORDER BY created_at DESC", (request["id"],)):
            request["screeningRemarks"].append({"id":item["id"],"visitorFormId":item["visitor_form_id"],"visitorName":names.get(item["visitor_form_id"], "Visitor"),"remark":normalize_text(item["remark"]),"createdBy":normalize_text(item["created_by"]),"createdAt":item["created_at"]})
        for review in connection.execute("SELECT * FROM ec_reviews WHERE request_id = ? ORDER BY reviewed_at DESC", (request["id"],)):
            targets = [target["visitor_form_id"] for target in connection.execute("SELECT visitor_form_id FROM ec_review_targets WHERE review_id = ? ORDER BY position", (review["id"],))]
            request["ecReviews"].append({"id":review["id"],"reviewerId":normalize_text(review["reviewer_id"]),"status":review["status"],"decision":review["decision"],"comments":normalize_text(review["comments"]),"reviewedAt":review["reviewed_at"],"visitorFormIds":targets,"visitorNames":[names.get(form_id, "Visitor") for form_id in targets]})
        for item in connection.execute("SELECT * FROM information_requests WHERE request_id = ? ORDER BY created_at DESC", (request["id"],)):
            fields = list(connection.execute("SELECT * FROM information_request_fields WHERE information_request_id = ? ORDER BY position", (item["id"],)))
            changes = list(connection.execute("SELECT * FROM information_changes WHERE information_request_id = ? ORDER BY position", (item["id"],)))
            field_names = [field["field_name"] for field in fields]
            request["informationRequests"].append({"id":item["id"],"visitorFormId":item["visitor_form_id"],"visitorName":names.get(item["visitor_form_id"], "Visitor"),"fields":field_names,"fieldLabels":[REQUESTABLE_FIELDS.get(name, name) for name in field_names],"comment":normalize_text(item["comment"]),"originalValues":{field["field_name"]:deserialize_json(field["original_value"]) for field in fields},"changes":[{"field":change["field_name"],"before":deserialize_json(change["before_value"]),"after":deserialize_json(change["after_value"])} for change in changes],"status":item["status"],"createdAt":item["created_at"],"respondedAt":item["responded_at"]})
        for item in connection.execute("SELECT * FROM schedule_changes WHERE request_id = ? ORDER BY changed_at DESC", (request["id"],)):
            request["scheduleChanges"].append({"id":item["id"],"oldStart":item["old_start"],"oldEnd":item["old_end"],"newStart":item["new_start"],"newEnd":item["new_end"],"reason":normalize_text(item["reason"]),"changedBy":normalize_text(item["changed_by"]),"changedAt":item["changed_at"]})
        for item in connection.execute("SELECT * FROM audit_events WHERE request_id = ? ORDER BY created_at DESC", (request["id"],)):
            request["auditHistory"].append({"id":item["id"],"action":item["action"],"details":normalize_text(item["details"]),"createdAt":item["created_at"]})
        results.append(ensure_request_shape(request))
    return results


def load_requests() -> list[dict]:
    with database_connection() as connection:
        return _load_requests(connection)


def save_request(request: dict) -> None:
    with database_connection() as connection:
        _persist_request(connection, request)


def next_sequence(year: int) -> int:
    key = f"request_sequence_{year}"
    with database_connection() as connection:
        connection.execute("BEGIN IMMEDIATE")
        connection.execute("INSERT OR IGNORE INTO app_meta (key, value) VALUES (?, 0)", (key,))
        value = connection.execute("SELECT value FROM app_meta WHERE key = ?", (key,)).fetchone()[0] + 1
        connection.execute("UPDATE app_meta SET value = ? WHERE key = ?", (value, key))
        connection.commit()
    return value


def find_user(user_id: str | None) -> dict | None:
    return next((item for item in USERS if item["id"] == user_id), None)


def find_request(request_id: str) -> dict | None:
    with database_connection() as connection:
        requests = _load_requests(connection, request_id)
    return requests[0] if requests else None


def find_form(form_id: str) -> tuple[dict, dict] | None:
    for request in load_requests():
        for form in request["visitorForms"]:
            if form["id"] == form_id:
                return request, form
    return None


def person_type(request: dict, form: dict) -> str:
    if form["idClassification"] == "Vendor":
        return "Vendor"
    if request["gtr"]:
        return "GTRE"
    return "Internal Visitor" if request["visitorType"] == "Internal" else "External Visitor"


def badge_type(request: dict, form: dict) -> str:
    category = person_type(request, form)
    if category == "Vendor":
        return "Orange-Vendors"
    if category == "GTRE":
        return "Red-GTRE"
    return "Red-Visitor"


def submitted_to_export_control(request: dict) -> bool:
    post_submission_statuses = {
        "PENDING_EC_REVIEW", "PENDING_DOCUMENTATION", "DOCUMENTATION_SUBMITTED",
        "EC_RE_REVIEW_REQUIRED", "APPROVED", "PARTIALLY_APPROVED", "REJECTED",
        "CANCELLED", "VISIT_PROCESS_COMPLETED",
    }
    return request["currentStatus"] in post_submission_statuses or any(item["action"] == "SEND_TO_EC" for item in request["auditHistory"])


def list_item(request: dict) -> dict:
    forms = request["visitorForms"]
    visitor_names = [form["fullName"] or "Visitor Details Pending" for form in forms]
    company_names = list(dict.fromkeys(form["companyName"] for form in forms if form["companyName"]))
    visitor_count = len(forms)
    today = datetime.now(IST).date().isoformat()
    entries = reception_entries([request])
    return {
        "id": request["id"],
        "requestNumber": request["requestNumber"],
        "visitorName": visitor_names[0] if visitor_count == 1 else "",
        "companyName": company_names[0] if visitor_count == 1 and company_names else "",
        "visitorCount": visitor_count,
        "visitorNames": visitor_names,
        "companyNames": company_names,
        "visitors": [
            {
                "id": form["id"],
                "fullName": form["fullName"] or f"Visitor {form['sequence']}",
                "companyName": form["companyName"],
                "citizenship": form["citizenship"],
                "idType": form["otherIdType"] if form["idType"] == "Other Government Issued ID" else form["idType"],
                "formStatus": form["status"],
                "screeningDecision": form["ecDecision"],
                "assetCount": len(form["assets"]),
            }
            for form in forms
        ],
        "completedVisitorForms": sum(form["status"] == "SUBMITTED" for form in forms),
        "currentStatus": request["currentStatus"],
        "submittedToExportControl": submitted_to_export_control(request),
        "createdAt": request["createdAt"],
        "visitDate": request["visitDays"][0]["visitDate"] if request["visitDays"] else "",
        "visitEndDate": request["visitDays"][-1]["visitDate"] if request["visitDays"] else "",
        "hostName": request["mainHostName"],
        "hostDepartment": request.get("hostDepartment", ""),
        "currentStage": request["currentStatus"],
        "lastUpdated": request["updatedAt"],
        "requesterId": request["requesterId"],
        "hasToday": any(day["visitDate"] == today for _, _, day, _ in entries),
        "hasCheckedIn": any(record["status"] == "CHECKED_IN" for _, _, _, record in entries),
        "hasCheckedOut": any(record["status"] == "COMPLETED" for _, _, _, record in entries),
        "hasNoShow": any(record["status"] == "NO_SHOW" for _, _, _, record in entries),
        "hasReceptionRejection": any(record["status"] == "ENTRY_REJECTED" for _, _, _, record in entries),
    }


def request_detail(request: dict, include_screening: bool = False, reception_view: bool = False) -> dict:
    assets = []
    versions = []
    visitors = []
    for visitor_form in request["visitorForms"]:
        visible_assets = deepcopy(visitor_form["assets"])
        assets.extend(visible_assets)
        phone = ""
        if visitor_form["telephone"]:
            prefix = f"+{visitor_form['phoneDialCode']} " if visitor_form["phoneDialCode"] else ""
            phone = f"{prefix}{visitor_form['telephone']}"
        visitors.append({
            "id": visitor_form["id"],
            "sequence": visitor_form["sequence"],
            "status": visitor_form["status"],
            "fullName": visitor_form["fullName"] or "Visitor Details Pending",
            "firstName": visitor_form["firstName"],
            "middleName": visitor_form["middleName"],
            "lastName": visitor_form["lastName"],
            "companyName": visitor_form["companyName"],
            "companyAddress": visitor_form["companyAddress"],
            "officeCity": visitor_form["officeCity"],
            "officeCountry": visitor_form["officeCountry"],
            "citizenship": visitor_form["citizenship"],
            "designation": visitor_form["designation"],
            "email": visitor_form["email"],
            "phone": phone,
            "phoneCountry": visitor_form["phoneCountry"],
            "phoneDialCode": visitor_form["phoneDialCode"],
            "telephone": visitor_form["telephone"],
            "idType": visitor_form["idType"],
            "otherIdType": visitor_form["otherIdType"],
            "ecDecision": visitor_form["ecDecision"],
            "idClassification": visitor_form["idClassification"],
            "ecDecisionReason": visitor_form["ecDecisionReason"],
            "ecDecisionAt": visitor_form["ecDecisionAt"],
            "visitorType": request["visitorType"],
            "personType": person_type(request, visitor_form),
            "badgeType": badge_type(request, visitor_form),
            "assets": visible_assets,
            "receptionRecords": deepcopy(visitor_form["receptionRecords"]),
            "screeningResult": {key: value for key, value in visitor_form["screeningResult"].items() if key != "content"} if visitor_form.get("screeningResult") and not reception_view else None,
        })
        for version in visitor_form["versions"] if not reception_view else []:
            snapshot = version["snapshot"]
            versions.append({"id": version.get("id", ""), "visitorFormId": visitor_form["id"], "version": version["version"], "fullName": snapshot.get("fullName", ""), "citizenship": snapshot.get("citizenship", snapshot.get("nationality", "")), "company": snapshot.get("companyName", ""), "designation": snapshot.get("designation", ""), "idType": snapshot.get("idType", ""), "assets": json.dumps(snapshot.get("assets", [])), "createdAt": version["createdAt"]})
    return {
        "id": request["id"],
        "requestNumber": request["requestNumber"],
        "requesterId": request["requesterId"],
        "visitorType": request["visitorType"],
        "visitorCount": len(visitors),
        "visitors": visitors,
        "purpose": request["purpose"],
        "areasToVisit": request["areasToVisit"],
        "visitingSite": request["visitingSite"],
        "visitStart": request["visitStart"],
        "visitEnd": request["visitEnd"],
        "visitPurposeType": request["visitPurposeType"],
        "mainHostName": request["mainHostName"],
        "hostDepartment": request.get("hostDepartment", ""),
        "escortingHostName": request["escortingHostName"],
        "faculty": request["faculty"],
        "gtr": request["gtr"],
        "idClassification": request["idClassification"],
        "currentStatus": request["currentStatus"],
        "submittedToExportControl": submitted_to_export_control(request),
        "cancellationReason": request.get("cancellationReason", ""),
        "visitorFormIds": [item["id"] for item in request["visitorForms"]],
        "visitorForms": [{"id": item["id"], "status": item["status"], "fullName": item["fullName"], "ecDecision": item["ecDecision"], "idClassification": item["idClassification"], "personType": person_type(request, item), "badgeType": badge_type(request, item), "ecDecisionReason": item["ecDecisionReason"], "receptionRecords": deepcopy(item["receptionRecords"]), "screeningResult": {key: value for key, value in item["screeningResult"].items() if key != "content"} if item.get("screeningResult") and not reception_view else None} for item in request["visitorForms"]],
        "formVersions": versions,
        "visitDays": deepcopy(request["visitDays"]),
        "assets": assets,
        "auditHistory": deepcopy(request["auditHistory"]),
        "screeningRemarks": deepcopy(request["screeningRemarks"]) if include_screening else [],
        "ecReviews": deepcopy(request["ecReviews"]),
        "comments": deepcopy(request["comments"]),
        "informationRequests": deepcopy(request["informationRequests"]),
        "scheduleChanges": deepcopy(request["scheduleChanges"]),
        "previousRequests": [],
        "previousVisitDays": [],
    }


def reception_entries(requests: list[dict]) -> list[tuple[dict, dict, dict, dict]]:
    entries = []
    for request in requests:
        days = {day["id"]: day for day in request["visitDays"]}
        for form in request["visitorForms"]:
            for record in form["receptionRecords"]:
                day = days.get(record["visitDayId"])
                if day is not None:
                    entries.append((request, form, day, record))
    return entries


def dashboard(user: dict) -> dict:
    requests = load_requests()
    if user["role"] == "HOST_REQUESTER":
        requests = [item for item in requests if item["requesterId"] == user["id"]]
    today = datetime.now(IST).date().isoformat()
    entries = reception_entries(requests)
    pending = {"VISITOR_FORM_PENDING", "VISITOR_FORM_SUBMITTED", "HOST_REVIEW", "PENDING_EC_REVIEW", "PENDING_DOCUMENTATION", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"}
    today_visitors = {(request["id"], form["id"]) for request, form, day, _ in entries if day["visitDate"] == today}
    checked_in_visitors = {(request["id"], form["id"]) for request, form, _, record in entries if record["status"] == "CHECKED_IN"}
    upcoming_visitors = {(request["id"], form["id"]) for request, form, day, record in entries if day["visitDate"] > today and record["status"] == "UPCOMING"}
    no_show_visitors = {(request["id"], form["id"]) for request, form, _, record in entries if record["status"] == "NO_SHOW"}
    checked_out_visitors = {(request["id"], form["id"]) for request, form, _, record in entries if record["status"] == "COMPLETED"}
    return {
        "totalRequests": sum(len(item["visitorForms"]) for item in requests),
        "pendingActions": sum(item["currentStatus"] in pending for item in requests),
        "todaysVisits": len(today_visitors),
        "currentlyInside": len(checked_in_visitors),
        "upcomingVisits": len(upcoming_visitors),
        "noShows": len(no_show_visitors),
        "pendingEcReviews": sum(item["currentStatus"] in {"PENDING_EC_REVIEW", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"} for item in requests),
        "pendingDocumentation": sum(item["currentStatus"] == "PENDING_DOCUMENTATION" for item in requests),
        "approved": sum(item["currentStatus"] in {"APPROVED", "PARTIALLY_APPROVED"} for item in requests),
        "checkedOut": len(checked_out_visitors),
        "recentRequests": [list_item(item) for item in sorted(requests, key=lambda value: value["createdAt"], reverse=True)[:10]],
    }


def compliance_dashboard() -> dict:
    requests = load_requests()
    pending = [item for item in requests if item["currentStatus"] in {"PENDING_EC_REVIEW", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"}]
    docs = [item for item in requests if item["currentStatus"] == "PENDING_DOCUMENTATION"]
    rejected_requests = {request["id"] for request, _, _, record in reception_entries(requests) if record["status"] == "ENTRY_REJECTED"}
    return {
        "pendingEcReviews": len(pending),
        "pendingDocumentation": len(docs),
        "receptionRejections": len(rejected_requests),
        "approved": sum(item["currentStatus"] in {"APPROVED", "PARTIALLY_APPROVED"} for item in requests),
        "rejected": sum(item["currentStatus"] == "REJECTED" for item in requests),
        "visitorHistory": sum(item["currentStatus"] in {"APPROVED", "PARTIALLY_APPROVED", "REJECTED", "CANCELLED", "VISIT_PROCESS_COMPLETED"} for item in requests),
        "pendingEcReviewsItems": [list_item(item) for item in pending],
        "pendingDocumentationItems": [list_item(item) for item in docs],
    }


def reception_dashboard() -> dict:
    today = datetime.now(IST).date().isoformat()
    items = []
    requests = [request for request in load_requests() if request["currentStatus"] in {"APPROVED", "PARTIALLY_APPROVED", "VISIT_PROCESS_COMPLETED"}]
    for request in requests:
        days = {day["id"]: day for day in request["visitDays"]}
        for form in request["visitorForms"]:
            if form["ecDecision"] != "APPROVED":
                continue
            dated_records = [(days[record["visitDayId"]], record) for record in form["receptionRecords"] if record["visitDayId"] in days]
            today_record = next(((day, record) for day, record in dated_records if day["visitDate"] == today), None)
            active_record = next(((day, record) for day, record in dated_records if record["status"] == "CHECKED_IN"), None)
            selected = active_record or today_record
            if selected is None:
                continue
            day, record = selected
            items.append({
            "id": record["id"],
            "visitDayId": day["id"],
            "visitorFormId": form["id"],
            "visitDate": day["visitDate"],
            "scheduledToday": today_record is not None,
            "visitStartDate": request["visitDays"][0]["visitDate"] if request["visitDays"] else "",
            "visitEndDate": request["visitDays"][-1]["visitDate"] if request["visitDays"] else "",
            "status": record["status"],
            "requestId": request["id"],
            "requestNumber": request["requestNumber"],
            "visitorName": form["fullName"] or "Visitor Details Pending",
            "company": form["companyName"],
            "mainHost": request["mainHostName"],
            "escort": request["escortingHostName"] or None,
            "faculty": request["faculty"],
            "gtr": request["gtr"],
            "idClassification": form["idClassification"],
            "personType": person_type(request, form),
            "badgeType": record["badgeType"] or badge_type(request, form),
            "approvalStatus": request["currentStatus"],
            "screeningStatus": form["ecDecision"],
            "idType": form["otherIdType"] if form["idType"] == "Other Government Issued ID" else form["idType"],
            "badge": record["badge"] or None,
            "identityStatus": record["identityStatus"],
            "identityHoldReason": record["identityHoldReason"],
            "assetsStatus": record["assetsStatus"],
            "assetsHoldReason": record["assetsHoldReason"],
            "verificationRemarks": record["verificationRemarks"],
            "assets": deepcopy(form["assets"]),
            })
    return {
        "todaysVisitors": sum(item["scheduledToday"] for item in items),
        "expected": sum(item["status"] in {"UPCOMING", "VERIFICATION_IN_PROGRESS", "RECEPTION_VERIFICATION"} for item in items),
        "rejected": sum(item["status"] == "ENTRY_REJECTED" for item in items),
        "currentlyInside": sum(item["status"] == "CHECKED_IN" for item in items),
        "checkedOut": sum(item["status"] == "COMPLETED" for item in items),
        "noShow": sum(item["status"] == "NO_SHOW" for item in items),
        "items": items,
    }


def analytics_data(include_asset_serials: bool = True, requester_id: str | None = None) -> dict:
    statuses: dict[str, int] = {}
    rows = []
    requests = load_requests()
    if requester_id:
        requests = [request for request in requests if request["requesterId"] == requester_id]
    for request in requests:
        for form in request["visitorForms"]:
            statuses[request["currentStatus"]] = statuses.get(request["currentStatus"], 0) + 1
            records = {record["visitDayId"]: record for record in form["receptionRecords"]}
            asset_summary = "; ".join(f"{asset['assetType']}: {asset['serialNumber']}" if include_asset_serials else asset["assetType"] for asset in form["assets"])
            for day in request["visitDays"] or [{}]:
                record = records.get(day.get("id"), {})
                rows.append({"requestId":request["id"],"visitorFormId":form["id"],"visitDayId":day.get("id", ""),"requestNumber":request["requestNumber"],"visitor":form["fullName"],"company":form["companyName"],"visitDate":day.get("visitDate", ""),"requestVisitStartDate":request["visitDays"][0]["visitDate"] if request["visitDays"] else "","requestVisitEndDate":request["visitDays"][-1]["visitDate"] if request["visitDays"] else "","site":request["visitingSite"],"host":request["mainHostName"],"hostDepartment":request.get("hostDepartment", ""),"status":request["currentStatus"],"screeningDecision":form["ecDecision"],"tag":form["idClassification"],"personType":person_type(request, form),"receptionStatus":record.get("status", "UPCOMING"),"identityStatus":record.get("identityStatus", "PENDING"),"assetsStatus":record.get("assetsStatus", "PENDING"),"assetCount":len(form["assets"]),"assetSummary":asset_summary,"badgeType":record.get("badgeType") or badge_type(request, form),"badgeId":record.get("badge", ""),"verificationRemarks":record.get("verificationRemarks", ""),"createdAt":request["createdAt"]})
    return {"totalRequests": sum(len(request["visitorForms"]) for request in requests), "byStatus": statuses, "rows": rows}


def add_audit(request: dict, action: str, details: str) -> None:
    request["auditHistory"].insert(0, {"id": new_id("audit"), "action": action, "details": details, "createdAt": iso_now()})
    request["updatedAt"] = iso_now()


class ApiError(Exception):
    def __init__(self, status: int, message: str):
        super().__init__(message)
        self.status = status
        self.message = message


def require_role(user: dict | None, *roles: str) -> dict:
    if user is None:
        raise ApiError(401, "Sign in is required.")
    if roles and user["role"] not in roles:
        raise ApiError(403, "Your role cannot perform this action.")
    return user


WORKFLOW_ACTION_ROLES = {
    "host-review": "HOST_REQUESTER",
    "send-to-ec": "HOST_REQUESTER",
    "reschedule": "HOST_REQUESTER",
    "cancel": "HOST_REQUESTER",
    "ec-add-remark": "EXPORT_CONTROL",
    "ec-approve": "EXPORT_CONTROL",
    "ec-reject": "EXPORT_CONTROL",
    "ec-request-documents": "EXPORT_CONTROL",
    "verify-entry": "RECEPTION",
    "check-in": "RECEPTION",
    "check-out": "RECEPTION",
    "no-show": "RECEPTION",
}
WORKFLOW_ACTION_STATES = {
    "host-review": {"VISITOR_FORM_SUBMITTED"},
    "send-to-ec": {"HOST_REVIEW"},
    "reschedule": {"DRAFT", "VISITOR_FORM_PENDING", "VISITOR_FORM_SUBMITTED", "HOST_REVIEW", "APPROVED", "PARTIALLY_APPROVED"},
    "cancel": {"DRAFT", "VISITOR_FORM_PENDING", "VISITOR_FORM_SUBMITTED", "HOST_REVIEW", "APPROVED", "PARTIALLY_APPROVED"},
    "ec-approve": {"PENDING_EC_REVIEW", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"},
    "ec-reject": {"PENDING_EC_REVIEW", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"},
    "ec-request-documents": {"PENDING_EC_REVIEW", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"},
    "verify-entry": {"APPROVED", "PARTIALLY_APPROVED"},
    "check-in": {"APPROVED", "PARTIALLY_APPROVED"},
    "check-out": {"APPROVED", "PARTIALLY_APPROVED"},
    "no-show": {"APPROVED", "PARTIALLY_APPROVED"},
}


def authorize_workflow_action(request: dict, action: str, user: dict) -> None:
    role = WORKFLOW_ACTION_ROLES.get(action)
    if role is None:
        raise ApiError(400, "Unknown workflow action.")
    require_role(user, role)
    if role == "HOST_REQUESTER" and request["requesterId"] != user["id"]:
        raise ApiError(403, "Only the requester can change this request.")
    allowed_states = WORKFLOW_ACTION_STATES.get(action)
    if allowed_states is not None and request["currentStatus"] not in allowed_states:
        raise ApiError(409, "This action is not available at the request's current stage.")


def validated_visit_window(payload: dict) -> tuple[list, str, str]:
    start_date = str(payload.get("startDate", "")).strip()
    start_time = str(payload.get("startTime", "")).strip()
    end_date = str(payload.get("endDate", "")).strip()
    end_time = str(payload.get("endTime", "")).strip()
    try:
        start = datetime.strptime(f"{start_date} {start_time}", "%Y-%m-%d %H:%M").replace(tzinfo=IST)
        end = datetime.strptime(f"{end_date} {end_time}", "%Y-%m-%d %H:%M").replace(tzinfo=IST)
    except ValueError as exc:
        raise ApiError(400, "Select valid start and end dates and times.") from exc
    if start <= datetime.now(IST):
        raise ApiError(400, "The visit start must be in the future.")
    if end <= start:
        raise ApiError(400, "The visit end must be after the visit start.")
    if end - start > timedelta(days=365):
        raise ApiError(400, "A visit cannot span more than 365 days.")
    result = []
    current = start.date()
    while current <= end.date():
        arrival = start.strftime("%H:%M") if current == start.date() else "00:00"
        departure = end.strftime("%H:%M") if current == end.date() else "23:59"
        result.append({"id": new_id("day"), "visitDate": current.isoformat(), "expectedArrivalTime": arrival, "expectedDepartureTime": departure})
        current += timedelta(days=1)
    return result, start.isoformat(), end.isoformat()


def create_request(payload: dict, user: dict) -> dict:
    require_role(user, "HOST_REQUESTER")
    visitor_type = str(payload.get("visitorType", ""))
    if visitor_type not in {"Internal", "External"}:
        raise ApiError(400, "Visitor type must be Internal or External.")
    count = payload.get("numberOfVisitors")
    if isinstance(count, bool) or not isinstance(count, int) or not 1 <= count <= 20:
        raise ApiError(400, "Number of visitors must be between 1 and 20.")
    required = ["visitingSite", "visitPurposeType", "purpose", "mainHostName"]
    if any(not str(payload.get(key, "")).strip() for key in required):
        raise ApiError(400, "Complete all required request fields.")
    visiting_site = str(payload["visitingSite"]).strip()
    if visiting_site not in {"Bengaluru", "Delhi"}:
        raise ApiError(400, "Site/facility must be Bengaluru or Delhi.")
    purpose_type = str(payload["visitPurposeType"]).strip()
    if purpose_type not in PURPOSE_TYPES:
        raise ApiError(400, "Select a valid purpose of visit.")
    if bool(payload.get("faculty")) and bool(payload.get("gtr")):
        raise ApiError(400, "Select no more than one contractor classification.")
    visit_days, visit_start, visit_end = validated_visit_window(payload)
    fingerprint = (
        user["id"],
        visitor_type,
        visiting_site,
        count,
        purpose_type,
        str(payload["purpose"]).strip().casefold(),
        str(payload["mainHostName"]).strip().casefold(),
        visit_start,
        visit_end,
    )
    for existing in load_requests():
        existing_fingerprint = (
            existing["requesterId"],
            existing["visitorType"],
            existing["visitingSite"],
            existing["numberOfVisitors"],
            existing["visitPurposeType"],
            existing["purpose"].casefold(),
            existing["mainHostName"].casefold(),
            existing["visitStart"],
            existing["visitEnd"],
        )
        if existing["currentStatus"] not in {"CANCELLED", "REJECTED", "VISIT_PROCESS_COMPLETED"} and existing_fingerprint == fingerprint:
            raise ApiError(409, f"An identical active request already exists as {existing['requestNumber']}.")
    request_year = datetime.now(IST).year
    sequence = next_sequence(request_year)
    request_id = new_id("request")
    now = iso_now()
    request = {
        "id": request_id,
        "requestNumber": f"V-{request_year}-{sequence}",
        "requesterId": user["id"],
        "mainHostId": user["id"],
        "mainHostName": str(payload["mainHostName"]).strip(),
        "hostDepartment": str(payload.get("hostDepartment", "")).strip(),
        "escortingHostId": "",
        "escortingHostName": str(payload.get("escortingHostName", "")).strip(),
        "visitorType": visitor_type,
        "faculty": visitor_type == "External" and bool(payload.get("faculty")),
        "gtr": visitor_type == "External" and bool(payload.get("gtr")),
        "visitingSite": visiting_site,
        "numberOfVisitors": count,
        "purpose": str(payload["purpose"]).strip(),
        "areasToVisit": str(payload.get("areasToVisit", "")).strip(),
        "visitPurposeType": purpose_type,
        "idClassification": "",
        "currentStatus": "DRAFT",
        "createdAt": now,
        "updatedAt": now,
        "submittedAt": None,
        "approvedAt": None,
        "rejectedAt": None,
        "rejectionReason": "",
        "cancellationReason": "",
        "visitorForms": [],
        "visitStart": visit_start,
        "visitEnd": visit_end,
        "visitDays": visit_days,
        "screeningRemarks": [],
        "ecReviews": [],
        "comments": [],
        "informationRequests": [],
        "scheduleChanges": [],
        "auditHistory": [],
    }
    request["visitorForms"] = [make_form(request_id, index) for index in range(count)]
    reset_reception_records(request)
    add_audit(request, "REQUEST_CREATED", f"Visitor request created with {count} visitor form{'s' if count != 1 else ''}.")
    save_request(request)
    return request_detail(request, user["role"] == "EXPORT_CONTROL", user["role"] == "RECEPTION")


def validate_visitor_form(payload: dict) -> None:
    required = ["firstName", "lastName", "designation", "companyName", "companyAddress", "officeCity", "officeCountry", "phoneCountry", "telephone", "idType"]
    if any(not str(payload.get(key, "")).strip() for key in required):
        raise ApiError(400, "Complete all required visitor fields.")
    if len(str(payload.get("firstName", "")).strip()) < 2:
        raise ApiError(400, "First Name must contain at least two characters.")
    if len(str(payload.get("lastName", "")).strip()) < 2:
        raise ApiError(400, "Last Name must contain at least two characters.")
    citizenship = str(payload.get("citizenship", "")).strip()
    if citizenship not in COUNTRY_NAMES:
        raise ApiError(400, "Select a valid citizenship.")
    office_country = str(payload.get("officeCountry", "")).strip()
    if office_country not in COUNTRY_NAMES:
        raise ApiError(400, "Select a valid company country.")
    phone_country = str(payload.get("phoneCountry", "")).strip()
    if phone_country not in COUNTRY_DIAL_CODES:
        raise ApiError(400, "Select a valid phone country code.")
    telephone = str(payload.get("telephone", "")).strip()
    if re.fullmatch(r"[0-9]+", telephone) is None:
        raise ApiError(400, "Phone number must contain digits only.")
    allowed_lengths = phone_lengths(phone_country)
    if len(telephone) not in allowed_lengths:
        if len(allowed_lengths) == 1:
            requirement = f"exactly {allowed_lengths[0]} digits"
        elif allowed_lengths == tuple(range(allowed_lengths[0], allowed_lengths[-1] + 1)):
            requirement = f"between {allowed_lengths[0]} and {allowed_lengths[-1]} digits"
        else:
            requirement = ", ".join(str(length) for length in allowed_lengths[:-1]) + f", or {allowed_lengths[-1]} digits"
        raise ApiError(400, f"Phone number must contain {requirement} for {phone_country}.")
    if payload.get("idType") not in ID_TYPES:
        raise ApiError(400, "Select a valid identity document type.")
    if payload.get("idType") == "Other Government Issued ID" and not str(payload.get("otherIdType", "")).strip():
        raise ApiError(400, "Enter the government-issued identity document type.")
    assets = payload.get("assets", [])
    if not isinstance(assets, list):
        raise ApiError(400, "Assets must be supplied as a list.")
    for asset in assets:
        if not str(asset.get("assetType", "")).strip():
            raise ApiError(400, "Every declared asset requires an asset type.")
        if not str(asset.get("serialNumber", "")).strip():
            raise ApiError(400, "Every declared asset requires a serial number for Security verification.")
    serial_numbers = [str(asset.get("serialNumber", "")).strip().casefold() for asset in assets]
    if len(serial_numbers) != len(set(serial_numbers)):
        raise ApiError(400, "Each declared asset must have a unique serial number.")


def submit_form(form_id: str, payload: dict, user: dict) -> dict:
    located = find_form(form_id)
    if located is None:
        raise ApiError(404, "Visitor form was not found.")
    request, form = located
    require_host_owner(request, user)
    initial_edit = not submitted_to_export_control(request) and request["currentStatus"] in {"DRAFT", "VISITOR_FORM_PENDING", "VISITOR_FORM_SUBMITTED", "HOST_REVIEW"}
    requested_revision = request["currentStatus"] == "PENDING_DOCUMENTATION" and form["status"] == "REVISION_REQUIRED"
    if not initial_edit and not requested_revision:
        raise ApiError(409, "Visitor details are locked unless Export Control requests more information.")
    validate_visitor_form(payload)
    fields = ["firstName", "middleName", "lastName", "citizenship", "designation", "companyName", "companyAddress", "officeCity", "officeCountry", "phoneCountry", "telephone", "email", "idType", "otherIdType"]
    candidate = {key: str(payload.get(key, "")).strip() for key in fields}
    candidate["fullName"] = joined_full_name(candidate["firstName"], candidate["middleName"], candidate["lastName"])
    candidate["phoneDialCode"] = COUNTRY_DIAL_CODES[candidate["phoneCountry"]]
    candidate_assets = []
    for item in payload.get("assets", []):
        candidate_assets.append({"id": item.get("id") or new_id("asset"), "assetType": str(item.get("assetType", "")).strip(), "description": str(item.get("description", "")).strip(), "serialNumber": str(item.get("serialNumber", "")).strip(), "verificationStatus": "NotVerified"})
    snapshot = deepcopy(candidate)
    snapshot["assets"] = deepcopy(candidate_assets)
    pending_information = [item for item in request["informationRequests"] if item["status"] == "PENDING" and item["visitorFormId"] == form["id"]]
    unchanged_requested_fields = []
    for information_request in pending_information:
        for field_name in information_request["fields"]:
            before = information_request["originalValues"].get(field_name, "")
            after = snapshot.get(field_name, "")
            if field_name == "assets":
                before = [{key: item.get(key, "") for key in ("assetType", "description", "serialNumber")} for item in before]
                after = [{key: item.get(key, "") for key in ("assetType", "description", "serialNumber")} for item in after]
            if before == after:
                unchanged_requested_fields.append(REQUESTABLE_FIELDS[field_name])
    if unchanged_requested_fields:
        labels = ", ".join(dict.fromkeys(unchanged_requested_fields))
        raise ApiError(400, f"Update the requested fields before submitting: {labels}.")
    if form["versions"]:
        previous = form["versions"][-1]["snapshot"]
        same_fields = all(previous.get(key, "") == snapshot.get(key, "") for key in candidate)
        previous_assets = [{key: item.get(key, "") for key in ("assetType", "description", "serialNumber")} for item in previous.get("assets", [])]
        submitted_assets = [{key: item.get(key, "") for key in ("assetType", "description", "serialNumber")} for item in snapshot["assets"]]
        if same_fields and previous_assets == submitted_assets:
            raise ApiError(409, "This version has already been submitted.")
    for other in request["visitorForms"]:
        if other["id"] == form["id"] or not other["fullName"]:
            continue
        same_phone = other["phoneCountry"] == candidate["phoneCountry"] and other["telephone"] == candidate["telephone"]
        same_identity = other["fullName"].casefold() == candidate["fullName"].casefold() and other["companyName"].casefold() == candidate["companyName"].casefold()
        if same_phone or same_identity:
            raise ApiError(409, "This visitor is already included in the request.")
    for key, value in candidate.items():
        form[key] = value
    form["assets"] = candidate_assets
    form["status"] = "SUBMITTED"
    form["versions"].append({"id": new_id("version"), "version": len(form["versions"]) + 1, "createdAt": iso_now(), "snapshot": snapshot})
    if request["currentStatus"] in {"PENDING_DOCUMENTATION", "DOCUMENTATION_SUBMITTED"}:
        for info in request["informationRequests"]:
            if info["status"] == "PENDING" and info["visitorFormId"] == form["id"]:
                info["status"] = "RESOLVED"
                info["respondedAt"] = iso_now()
                info["changes"] = []
                for field_name in info["fields"]:
                    before = info["originalValues"].get(field_name, "")
                    after = snapshot.get(field_name, "")
                    if before != after:
                        info["changes"].append({"field": REQUESTABLE_FIELDS[field_name], "before": before, "after": after})
        request["currentStatus"] = "PENDING_DOCUMENTATION" if any(info["status"] == "PENDING" for info in request["informationRequests"]) else "DOCUMENTATION_SUBMITTED"
        add_audit(request, "ADDITIONAL_INFORMATION_SUBMITTED", f"Visitor: {form['fullName']}. Form version {len(form['versions'])} submitted.")
    else:
        request["currentStatus"] = "VISITOR_FORM_SUBMITTED" if all(item["status"] == "SUBMITTED" for item in request["visitorForms"]) else "VISITOR_FORM_PENDING"
        add_audit(request, "VISITOR_FORM_SUBMITTED", f"Visitor: {form['fullName']}. Visitor {form['sequence']} submitted their form.")
    save_request(request)
    return request_detail(request)


def save_screening_result(form_id: str, content: bytes, user: dict) -> dict:
    located = find_form(form_id)
    if located is None:
        raise ApiError(404, "Visitor form was not found.")
    request, form = located
    require_host_owner(request, user)
    if request["visitorType"] != "External":
        raise ApiError(409, "Denied Party Screening is skipped for internal visitors.")
    initial_edit = not submitted_to_export_control(request) and request["currentStatus"] in {"DRAFT", "VISITOR_FORM_PENDING", "VISITOR_FORM_SUBMITTED", "HOST_REVIEW"}
    requested_revision = request["currentStatus"] == "PENDING_DOCUMENTATION" and form["status"] == "REVISION_REQUIRED"
    if not initial_edit and not requested_revision:
        raise ApiError(409, "The screening attachment is locked unless Export Control requests more information.")
    if len(content) == 0 or len(content) > 10_485_760:
        raise ApiError(400, "The PDF must be between 1 byte and 10 MB.")
    if not content.startswith(b"%PDF-"):
        raise ApiError(400, "Denied Party Screening Results must be a valid PDF.")
    now = iso_now()
    form["screeningResult"] = {"fileName":"Denied Party Screening Results.pdf","contentType":"application/pdf","fileSize":len(content),"content":content,"uploadedBy":user["name"],"uploadedAt":now}
    add_audit(request, "SCREENING_RESULT_ATTACHED", f"{user['name']}: Denied Party Screening Results PDF attached for Visitor {form['sequence']}.")
    save_request(request)
    return {key: value for key, value in form["screeningResult"].items() if key != "content"}


def screening_result(form_id: str, user: dict) -> dict:
    located = find_form(form_id)
    if located is None:
        raise ApiError(404, "Visitor form was not found.")
    request, form = located
    if request["visitorType"] != "External":
        raise ApiError(404, "Denied Party Screening is not required for internal visitors.")
    if user["role"] == "HOST_REQUESTER" and request["requesterId"] != user["id"]:
        raise ApiError(403, "Only the requester can access this PDF.")
    require_role(user, "HOST_REQUESTER", "EXPORT_CONTROL")
    if not form.get("screeningResult"):
        raise ApiError(404, "Denied Party Screening Results were not attached.")
    return form["screeningResult"]


def require_host_owner(request: dict, user: dict) -> None:
    require_role(user, "HOST_REQUESTER")
    if request["requesterId"] != user["id"]:
        raise ApiError(403, "Only the requester can change this request.")


def selected_visit(request: dict, form_id: str, day_id: str) -> tuple[dict, dict, dict]:
    form = next((item for item in request["visitorForms"] if item["id"] == form_id), None)
    if form is None:
        raise ApiError(400, "Select a valid visitor.")
    day = next((item for item in request["visitDays"] if item["id"] == day_id), None)
    if day is None:
        raise ApiError(400, "Select a valid visit day.")
    record = next((item for item in form["receptionRecords"] if item["visitDayId"] == day_id), None)
    if record is None:
        raise ApiError(400, "The selected visitor is not scheduled for that day.")
    return form, day, record


def all_reception_records(request: dict) -> list[dict]:
    return [record for form in request["visitorForms"] for record in form["receptionRecords"]]


def selected_ec_forms(request: dict, payload: dict) -> list[dict]:
    form_ids = payload.get("visitorFormIds")
    if not isinstance(form_ids, list):
        single_id = str(payload.get("visitorFormId", ""))
        form_ids = [single_id] if single_id else []
    form_ids = list(dict.fromkeys(str(item) for item in form_ids if str(item)))
    if not form_ids:
        raise ApiError(400, "Select at least one visitor.")
    forms = [form for form in request["visitorForms"] if form["id"] in form_ids]
    if len(forms) != len(form_ids):
        raise ApiError(400, "Select valid visitors from this request.")
    return forms


def update_screening_status(request: dict, now: str) -> None:
    decisions = [form["ecDecision"] for form in request["visitorForms"]]
    approved = [form for form in request["visitorForms"] if form["ecDecision"] == "APPROVED"]
    if any(not decision for decision in decisions):
        request["currentStatus"] = "PENDING_EC_REVIEW"
    elif all(decision == "REJECTED" for decision in decisions):
        request["currentStatus"] = "REJECTED"
        request["rejectedAt"] = now
        request["approvedAt"] = None
    elif all(decision == "APPROVED" for decision in decisions):
        request["currentStatus"] = "APPROVED"
        request["approvedAt"] = now
        request["rejectedAt"] = None
    else:
        request["currentStatus"] = "PARTIALLY_APPROVED"
        request["approvedAt"] = now
        request["rejectedAt"] = None
    classifications = {form["idClassification"] for form in approved if form["idClassification"]}
    request["idClassification"] = next(iter(classifications)) if len(classifications) == 1 and len(approved) == len(request["visitorForms"]) else ""


def execute_action(request: dict, payload: dict, user: dict) -> dict:
    action = str(payload.get("action", "")).strip().lower()
    authorize_workflow_action(request, action, user)
    status = request["currentStatus"]
    now = iso_now()
    audit_context = ""
    if action == "host-review":
        request["currentStatus"] = "HOST_REVIEW"
    elif action == "send-to-ec":
        request["currentStatus"] = "PENDING_EC_REVIEW"
        request["submittedAt"] = now
    elif action == "reschedule":
        if any(record["status"] in {"VERIFICATION_IN_PROGRESS", "RECEPTION_VERIFICATION", "ENTRY_REJECTED", "CHECKED_IN", "COMPLETED", "NO_SHOW"} for record in all_reception_records(request)):
            raise ApiError(409, "A request cannot be rescheduled after reception processing begins.")
        visit_days, visit_start, visit_end = validated_visit_window(payload)
        if visit_start == request["visitStart"] and visit_end == request["visitEnd"]:
            raise ApiError(409, "Select a different visit window.")
        reason = str(payload.get("reason", "")).strip()
        audit_context = f"Previous window: {request['visitStart']} to {request['visitEnd']}. New window: {visit_start} to {visit_end}." + (f" Reason: {reason}." if reason else "")
        request["scheduleChanges"].insert(0, {"id": new_id("schedule"), "oldStart": request["visitStart"], "oldEnd": request["visitEnd"], "newStart": visit_start, "newEnd": visit_end, "reason": reason, "changedBy": user["name"], "changedAt": now})
        request["visitDays"] = visit_days
        request["visitStart"] = visit_start
        request["visitEnd"] = visit_end
        reset_reception_records(request)
        if status in {"PENDING_EC_REVIEW", "PENDING_DOCUMENTATION", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED", "APPROVED", "PARTIALLY_APPROVED"}:
            request["currentStatus"] = "PENDING_DOCUMENTATION" if status == "PENDING_DOCUMENTATION" else "HOST_REVIEW"
            request["submittedAt"] = None
            request["approvedAt"] = None
            request["idClassification"] = ""
            for form in request["visitorForms"]:
                form["ecDecision"] = ""
                form["idClassification"] = ""
                form["ecDecisionReason"] = ""
                form["ecDecisionAt"] = None
    elif action == "cancel":
        if any(record["status"] in {"CHECKED_IN", "COMPLETED"} for record in all_reception_records(request)):
            raise ApiError(409, "A visit that has started cannot be cancelled.")
        reason = str(payload.get("reason", "")).strip()
        if not reason:
            raise ApiError(400, "A cancellation reason is required.")
        audit_context = f"Reason: {reason}."
        request["currentStatus"] = "CANCELLED"
        request["cancellationReason"] = reason
        for record in all_reception_records(request):
            record["status"] = "CANCELLED"
            record["badge"] = ""
            record["badgeType"] = ""
    elif action == "ec-add-remark":
        form_id = str(payload.get("visitorFormId", ""))
        form = next((item for item in request["visitorForms"] if item["id"] == form_id), None)
        if form is None:
            raise ApiError(400, "Select a valid visitor.")
        remark = str(payload.get("remark", "")).strip()
        if not remark:
            raise ApiError(400, "Enter a screening remark.")
        if any(item["visitorFormId"] == form_id and item["remark"].casefold() == remark.casefold() for item in request["screeningRemarks"]):
            raise ApiError(409, "This screening remark has already been added.")
        request["screeningRemarks"].insert(0, {"id": new_id("remark"), "visitorFormId": form_id, "visitorName": form["fullName"] or f"Visitor {form['sequence']}", "remark": remark, "createdBy": user["name"], "createdAt": now})
    elif action in {"ec-approve", "ec-reject", "ec-request-documents"}:
        if action == "ec-request-documents":
            form_id = str(payload.get("visitorFormId", ""))
            form = next((item for item in request["visitorForms"] if item["id"] == form_id), None)
            if form is None:
                raise ApiError(400, "Select a valid visitor.")
            fields = payload.get("fields", [])
            if not isinstance(fields, list):
                raise ApiError(400, "Select the information that must be updated.")
            fields = list(dict.fromkeys(str(item) for item in fields))
            if not fields or any(item not in REQUESTABLE_FIELDS for item in fields):
                raise ApiError(400, "Select valid visitor fields.")
            comment = str(payload.get("comment", "")).strip()
            if not comment:
                raise ApiError(400, "Enter instructions for the requester.")
            if any(item["status"] == "PENDING" and item["visitorFormId"] == form_id and item["fields"] == fields for item in request["informationRequests"]):
                raise ApiError(409, "An identical information request is already pending.")
            original_values = {field_name: deepcopy(form["assets"] if field_name == "assets" else form.get(field_name, "")) for field_name in fields}
            request["currentStatus"] = "PENDING_DOCUMENTATION"
            form["status"] = "REVISION_REQUIRED"
            form["ecDecision"] = ""
            form["idClassification"] = ""
            form["ecDecisionReason"] = ""
            form["ecDecisionAt"] = None
            visitor_name = form["fullName"] or f"Visitor {form['sequence']}"
            request["informationRequests"].insert(0, {"id": new_id("info"), "visitorFormId": form_id, "visitorName": visitor_name, "fields": fields, "fieldLabels": [REQUESTABLE_FIELDS[item] for item in fields], "comment": comment, "originalValues": original_values, "changes": [], "status": "PENDING", "createdAt": now, "respondedAt": None})
            request["ecReviews"].insert(0, {"id": new_id("review"), "reviewerId": user["name"], "status": "PendingDocumentation", "decision": "RequestDocumentation", "comments": comment, "reviewedAt": now, "visitorFormIds": [form["id"]], "visitorNames": [visitor_name]})
            audit_context = f"Visitor: {visitor_name}. Fields: {', '.join(REQUESTABLE_FIELDS[item] for item in fields)}. Instructions: {comment}."
        else:
            forms = selected_ec_forms(request, payload)
            classification = str(payload.get("idClassification", "")).strip()
            if classification not in {"Vendor", "Visitor"}:
                raise ApiError(400, "Select Vendor or Visitor.")
            visitor_names = [form["fullName"] or f"Visitor {form['sequence']}" for form in forms]
            audit_context = f"Visitors: {', '.join(visitor_names)}. Tag: {classification}."
            if action == "ec-approve":
                comment = str(payload.get("comment", "")).strip()
                if comment:
                    audit_context += f" Remark: {comment}."
                for form in forms:
                    form["ecDecision"] = "APPROVED"
                    form["idClassification"] = classification
                    form["ecDecisionReason"] = ""
                    form["ecDecisionAt"] = now
                    for record in form["receptionRecords"]:
                        if record["status"] == "ENTRY_REJECTED" and record["identityStatus"] != "REJECTED":
                            record["status"] = "UPCOMING"
                request["ecReviews"].insert(0, {"id": new_id("review"), "reviewerId": user["name"], "status": "Approved", "decision": "Approve", "comments": comment, "reviewedAt": now, "visitorFormIds": [form["id"] for form in forms], "visitorNames": visitor_names})
            else:
                reason = str(payload.get("reason", "")).strip()
                if not reason:
                    raise ApiError(400, "A rejection reason is required.")
                audit_context += f" Reason: {reason}."
                for form in forms:
                    form["ecDecision"] = "REJECTED"
                    form["idClassification"] = classification
                    form["ecDecisionReason"] = reason
                    form["ecDecisionAt"] = now
                    for record in form["receptionRecords"]:
                        record["status"] = "ENTRY_REJECTED"
                request["ecReviews"].insert(0, {"id": new_id("review"), "reviewerId": user["name"], "status": "Rejected", "decision": "Reject", "comments": reason, "reviewedAt": now, "visitorFormIds": [form["id"] for form in forms], "visitorNames": visitor_names})
            update_screening_status(request, now)
            request["rejectionReason"] = reason if action == "ec-reject" and request["currentStatus"] == "REJECTED" else ""
    elif action in {"verify-entry", "check-in", "check-out", "no-show"}:
        form, day, record = selected_visit(request, str(payload.get("visitorFormId", "")), str(payload.get("visitDayId", "")))
        visitor_name = form["fullName"] or f"Visitor {form['sequence']}"
        visit_range = f"{request['visitDays'][0]['visitDate']} to {request['visitDays'][-1]['visitDate']}" if request["visitDays"] else "Not scheduled"
        audit_context = f"Visitor: {visitor_name}. Visit range: {visit_range}."
        if form["ecDecision"] != "APPROVED":
            raise ApiError(409, "Only an approved visitor can be processed at reception.")
        today = datetime.now(IST).date().isoformat()
        if action in {"verify-entry", "check-in"} and day["visitDate"] != today:
            raise ApiError(409, "Reception processing is available only on the scheduled visit date.")
        if action == "no-show" and day["visitDate"] > today:
            raise ApiError(409, "A future visit cannot be marked as a no-show.")
        if action == "verify-entry":
            if record["status"] not in {"UPCOMING", "VERIFICATION_IN_PROGRESS", "RECEPTION_VERIFICATION"}:
                raise ApiError(409, "Verification is not available for this visit.")
            decision = str(payload.get("decision", "")).strip().upper()
            if decision not in {"APPROVE", "REJECT"}:
                raise ApiError(400, "Select Approve or Reject.")
            remarks = str(payload.get("remarks", "")).strip()
            if decision == "REJECT":
                if not remarks:
                    raise ApiError(400, "Remarks are required when entry is rejected.")
                record["identityStatus"] = "REJECTED"
                record["assetsStatus"] = "REJECTED"
                record["identityHoldReason"] = remarks
                record["assetsHoldReason"] = remarks
                record["verificationRemarks"] = remarks
                record["holdReason"] = remarks
                record["status"] = "ENTRY_REJECTED"
                for asset in form["assets"]:
                    asset["verificationStatus"] = "Rejected"
                audit_context += f" Entry rejected. Remarks: {remarks}."
            else:
                if payload.get("identityConfirmed") is not True:
                    raise ApiError(400, "Confirm identity verification before approval.")
                if form["assets"]:
                    if payload.get("assetsConfirmed") is not True:
                        raise ApiError(400, "Confirm asset verification before approval.")
                    record["assetsStatus"] = "APPROVED"
                else:
                    record["assetsStatus"] = "NOT_APPLICABLE"
                record["identityStatus"] = "APPROVED"
                record["identityHoldReason"] = ""
                record["assetsHoldReason"] = ""
                record["verificationRemarks"] = remarks
                record["holdReason"] = ""
                record["status"] = "RECEPTION_VERIFICATION"
                for asset in form["assets"]:
                    asset["verificationStatus"] = "Verified"
                audit_context += " Identity and assets approved." + (f" Remarks: {remarks}." if remarks else "")
        elif action == "check-in":
            if record["identityStatus"] != "APPROVED" or record["assetsStatus"] not in {"APPROVED", "NOT_APPLICABLE"} or record["status"] != "RECEPTION_VERIFICATION":
                raise ApiError(409, "Identity and assets must be verified before check-in.")
            if any(item["id"] != record["id"] and item["status"] == "CHECKED_IN" for item in form["receptionRecords"]):
                raise ApiError(409, "This visitor is already checked in for another visit date.")
            badge = str(payload.get("badgeNumber", "")).strip()
            if not badge:
                raise ApiError(400, "Badge ID is required.")
            if len(badge) > 64:
                raise ApiError(400, "Badge ID cannot exceed 64 characters.")
            for other in load_requests():
                for other_record in all_reception_records(other):
                    if other_record["badge"].casefold() == badge.casefold() and other_record["status"] == "CHECKED_IN":
                        raise ApiError(409, "That badge is already assigned to an active visitor.")
            record["badge"] = badge
            record["badgeType"] = badge_type(request, form)
            record["status"] = "CHECKED_IN"
            record["actualArrivalTime"] = now
        elif action == "check-out":
            if record["status"] != "CHECKED_IN":
                raise ApiError(409, "The visitor must be checked in before check-out.")
            record["status"] = "COMPLETED"
            record["actualDepartureTime"] = now
            record["badgeReturnedAt"] = now
        else:
            if record["status"] not in {"UPCOMING", "VERIFICATION_IN_PROGRESS"}:
                raise ApiError(409, "This visit cannot be marked as a no-show.")
            if record["identityStatus"] == "APPROVED" or record["assetsStatus"] == "APPROVED":
                raise ApiError(409, "A visitor cannot be marked as a no-show after identity or asset verification.")
            record["status"] = "NO_SHOW"
        if all(item["status"] in {"COMPLETED", "NO_SHOW", "ENTRY_REJECTED", "CANCELLED"} for item in all_reception_records(request)):
            request["currentStatus"] = "VISIT_PROCESS_COMPLETED"
    action_labels = {
        "host-review": "Host review saved",
        "send-to-ec": "Sent to Export Control",
        "reschedule": "Visit rescheduled",
        "cancel": "Request cancelled",
        "ec-add-remark": "Screening remark added",
        "ec-approve": "Selected visitors approved",
        "ec-reject": "Selected visitors rejected",
        "ec-request-documents": "Additional information requested",
        "verify-entry": "Entry verification completed",
        "check-in": "Visitor checked in",
        "check-out": "Visitor checked out",
        "no-show": "Visitor marked as a no-show",
    }
    add_audit(request, action.upper().replace("-", "_"), f"{user['name']}: {action_labels[action]}." + (f" {audit_context}" if audit_context else ""))
    save_request(request)
    return request_detail(request, user["role"] == "EXPORT_CONTROL", user["role"] == "RECEPTION")


def spreadsheet_text(value: object) -> str:
    text = str(value)
    return f"'{text}" if text.startswith(("=", "+", "-", "@", "\t", "\r")) else text


ANALYTICS_COLUMNS = [
    ("Request", "requestNumber"),
    ("Visitor", "visitor"),
    ("Company", "company"),
    ("Site", "site"),
    ("Host", "host"),
    ("Host Department", "hostDepartment"),
    ("Person Type", "personType"),
    ("Visit Range", "visitRange"),
    ("Request Status", "status"),
    ("Screening Decision", "screeningDecision"),
    ("Reception Status", "receptionStatus"),
    ("Identity Status", "identityStatus"),
    ("Asset Status", "assetsStatus"),
    ("Asset Details", "assetSummary"),
    ("Badge Type", "badgeType"),
    ("Badge ID", "badgeId"),
    ("Verification Remarks", "verificationRemarks"),
    ("Created", "createdAt"),
]


def collapse_analytics_rows(rows: list[dict]) -> list[dict]:
    today = datetime.now(IST).date().isoformat()
    groups: dict[tuple[str, str], dict] = {}

    def priority(row: dict) -> int:
        if row["receptionStatus"] == "CHECKED_IN":
            return 500
        if row["visitDate"] == today:
            return 400
        if row["receptionStatus"] in {"COMPLETED", "NO_SHOW", "ENTRY_REJECTED"}:
            return 300
        if row["receptionStatus"] == "RECEPTION_VERIFICATION":
            return 200
        return 100

    for row in sorted(rows, key=lambda item: item["visitDate"]):
        key = (row["requestId"], row["visitorFormId"])
        current = groups.get(key)
        row_priority = priority(row)
        current_priority = priority(current) if current is not None else -1
        if current is None or row_priority > current_priority or (row_priority == current_priority and row_priority >= 300 and row["visitDate"] > current["visitDate"]):
            group = deepcopy(row)
            group["visitStartDate"] = row["requestVisitStartDate"]
            group["visitEndDate"] = row["requestVisitEndDate"]
            group["visitRange"] = group["visitStartDate"] if group["visitStartDate"] == group["visitEndDate"] else f"{group['visitStartDate']} – {group['visitEndDate']}"
            groups[key] = group
    return list(groups.values())


def filtered_analytics_rows(query: str = "", include_asset_serials: bool = True, requester_id: str | None = None) -> list[dict]:
    parameters = {key: values[-1] for key, values in parse_qs(query).items() if values}
    rows = analytics_data(include_asset_serials, requester_id)["rows"]
    if parameters.get("history") == "1":
        history_statuses = {"APPROVED", "PARTIALLY_APPROVED", "REJECTED", "CANCELLED", "VISIT_PROCESS_COMPLETED"}
        rows = [row for row in rows if row["status"] in history_statuses]
    exact_filters = {"date":"visitDate","status":"status","screening":"screeningDecision","reception":"receptionStatus","identity":"identityStatus","site":"site","personType":"personType"}
    for parameter, field_name in exact_filters.items():
        value = parameters.get(parameter, "").strip()
        if value:
            rows = [row for row in rows if row[field_name] == value]
    asset_filter = parameters.get("assets", "").strip()
    if asset_filter == "DECLARED":
        rows = [row for row in rows if row["assetCount"] > 0]
    elif asset_filter == "NONE":
        rows = [row for row in rows if row["assetCount"] == 0]
    elif asset_filter == "VERIFIED":
        rows = [row for row in rows if row["assetCount"] > 0 and row["assetsStatus"] == "APPROVED"]
    elif asset_filter == "REJECTED":
        rows = [row for row in rows if row["assetsStatus"] == "REJECTED"]
    search = parameters.get("q", "").strip().casefold()
    if search:
        searchable = ("requestNumber", "visitor", "company", "host", "hostDepartment", "badgeId", "assetSummary")
        rows = [row for row in rows if any(search in str(row[field_name] or "").casefold() for field_name in searchable)]
    return collapse_analytics_rows(rows)


def csv_bytes(query: str = "", include_asset_serials: bool = True, requester_id: str | None = None) -> bytes:
    output = io.StringIO()
    writer = csv.writer(output)
    writer.writerow([label for label, _ in ANALYTICS_COLUMNS])
    for row in filtered_analytics_rows(query, include_asset_serials, requester_id):
        writer.writerow([spreadsheet_text(row[field_name]) for _, field_name in ANALYTICS_COLUMNS])
    return output.getvalue().encode("utf-8-sig")


def spreadsheet_column_name(number: int) -> str:
    letters = ""
    while number:
        number, remainder = divmod(number - 1, 26)
        letters = chr(65 + remainder) + letters
    return letters


def xlsx_bytes(query: str = "", include_asset_serials: bool = True, requester_id: str | None = None) -> bytes:
    rows = [[label for label, _ in ANALYTICS_COLUMNS]]
    rows.extend([[row[field_name] for _, field_name in ANALYTICS_COLUMNS] for row in filtered_analytics_rows(query, include_asset_serials, requester_id)])
    sheet_rows = []
    for row_number, row in enumerate(rows, 1):
        cells = []
        for column_number, value in enumerate(row, 1):
            letters = spreadsheet_column_name(column_number)
            style = ' s="1"' if row_number == 1 else ""
            cells.append(f'<c r="{letters}{row_number}" t="inlineStr"{style}><is><t>{xml_escape(str(value))}</t></is></c>')
        sheet_rows.append(f'<row r="{row_number}">{"".join(cells)}</row>')
    last_column = spreadsheet_column_name(len(ANALYTICS_COLUMNS))
    files = {
        "[Content_Types].xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>',
        "_rels/.rels": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>',
        "xl/workbook.xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Visitor Analytics" sheetId="1" r:id="rId1"/></sheets></workbook>',
        "xl/_rels/workbook.xml.rels": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>',
        "xl/styles.xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Arial"/></font><font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Arial"/></font></fonts><fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF10069F"/><bgColor indexed="64"/></patternFill></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/></cellXfs></styleSheet>',
        "xl/worksheets/sheet1.xml": f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><cols><col min="1" max="{len(ANALYTICS_COLUMNS)}" width="24" customWidth="1"/></cols><sheetData>{"".join(sheet_rows)}</sheetData><autoFilter ref="A1:{last_column}{len(rows)}"/></worksheet>',
    }
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, content in files.items():
            archive.writestr(name, content)
    return buffer.getvalue()




def rendered_html() -> bytes:
    countries = [{**item, "lengths": list(phone_lengths(item["name"]))} for item in COUNTRIES]
    return INDEX_TEMPLATE.replace("__USERS__", json.dumps(USERS, separators=(",", ":"))).replace("__COUNTRIES__", json.dumps(countries, ensure_ascii=False, separators=(",", ":"))).replace("__BRAND_LOGO__", BRAND_LOGO_DATA_URI).encode("utf-8")


class Handler(BaseHTTPRequestHandler):
    server_version = "VisitorManagement/1.0"

    def log_message(self, format_string: str, *args: object) -> None:
        print(f"{self.address_string()} [{self.log_date_time_string()}] {format_string % args}")

    def current_user(self) -> dict | None:
        return find_user(self.headers.get("X-Visitor-Role"))

    def send_bytes(self, status: int, payload: bytes, content_type: str, filename: str | None = None) -> None:
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(payload)))
        self.send_header("Cache-Control", "no-store")
        self.send_header("X-Content-Type-Options", "nosniff")
        self.send_header("X-Frame-Options", "DENY")
        self.send_header("Referrer-Policy", "no-referrer")
        self.send_header("Permissions-Policy", "camera=(), microphone=(), geolocation=()")
        self.send_header("Content-Security-Policy", "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'")
        if filename:
            self.send_header("Content-Disposition", f'attachment; filename="{filename}"')
        self.end_headers()
        self.wfile.write(payload)

    def send_json(self, status: int, data: object) -> None:
        self.send_bytes(status, json.dumps(data, ensure_ascii=False, separators=(",", ":")).encode("utf-8"), "application/json; charset=utf-8")

    def read_json(self) -> dict:
        try:
            length = int(self.headers.get("Content-Length", "0"))
        except ValueError as exc:
            raise ApiError(400, "Invalid request length.") from exc
        if length <= 0 or length > 2_000_000:
            raise ApiError(400, "A valid JSON request body is required.")
        try:
            data = json.loads(self.rfile.read(length))
        except (UnicodeDecodeError, json.JSONDecodeError) as exc:
            raise ApiError(400, "A valid JSON request body is required.") from exc
        if not isinstance(data, dict):
            raise ApiError(400, "The JSON request body must be an object.")
        return data

    def read_bytes(self, maximum: int) -> bytes:
        try:
            length = int(self.headers.get("Content-Length", "0"))
        except ValueError as exc:
            raise ApiError(400, "Invalid request length.") from exc
        if length <= 0 or length > maximum:
            raise ApiError(400, "The PDF must be between 1 byte and 10 MB.")
        return self.rfile.read(length)

    def handle_api_error(self, action) -> None:
        try:
            with LOCK:
                action()
        except ApiError as exc:
            self.send_json(exc.status, {"error": exc.message})
        except BrokenPipeError:
            return
        except Exception as exc:
            print(f"Unhandled request error: {exc}")
            self.send_json(500, {"error": "The application encountered an unexpected server error."})

    def do_GET(self) -> None:
        self.handle_api_error(self.get_route)

    def get_route(self) -> None:
        split = urlsplit(self.path)
        path = split.path.rstrip("/") or "/"
        user = self.current_user()
        if path == "/api/health":
            self.send_json(200, {"status": "Healthy", "service": APP_TITLE, "database": "SQLite"})
            return
        if path == "/api/users":
            self.send_json(200, USERS)
            return
        if path == "/api/visitor-requests":
            actor = require_role(user)
            requests = load_requests()
            if actor["role"] == "HOST_REQUESTER":
                requests = [item for item in requests if item["requesterId"] == actor["id"]]
            items = [list_item(item) for item in requests]
            self.send_json(200, {"items": items, "total": len(items)})
            return
        if path.startswith("/api/visitor-requests/"):
            require_role(user)
            request_id = path.removeprefix("/api/visitor-requests/")
            request = find_request(request_id)
            if request is None:
                raise ApiError(404, "Visitor request was not found.")
            if user["role"] == "HOST_REQUESTER" and request["requesterId"] != user["id"]:
                raise ApiError(403, "Only the requester can view this request.")
            self.send_json(200, request_detail(request, user["role"] == "EXPORT_CONTROL", user["role"] == "RECEPTION"))
            return
        if path.startswith("/api/visitor-forms/") and path.endswith("/screening-results"):
            actor = require_role(user, "HOST_REQUESTER", "EXPORT_CONTROL")
            form_id = path.removeprefix("/api/visitor-forms/").removesuffix("/screening-results").rstrip("/")
            result = screening_result(form_id, actor)
            self.send_bytes(200, result["content"], result["contentType"], result["fileName"])
            return
        if path.startswith("/api/visitor-forms/"):
            actor = require_role(user, "HOST_REQUESTER")
            form_id = path.removeprefix("/api/visitor-forms/")
            located = find_form(form_id)
            if located is None:
                raise ApiError(404, "Visitor form was not found.")
            request, form = located
            require_host_owner(request, actor)
            initial_edit = not submitted_to_export_control(request) and request["currentStatus"] in {"DRAFT", "VISITOR_FORM_PENDING", "VISITOR_FORM_SUBMITTED", "HOST_REVIEW"}
            requested_revision = request["currentStatus"] == "PENDING_DOCUMENTATION" and form["status"] == "REVISION_REQUIRED"
            if not initial_edit and not requested_revision:
                raise ApiError(409, "Visitor details are locked unless Export Control requests more information.")
            result = deepcopy(form)
            if result.get("screeningResult"):
                result["screeningResult"].pop("content", None)
            result["requestNumber"] = request["requestNumber"]
            result["visitorType"] = request["visitorType"]
            result["requestedFields"] = list(dict.fromkeys(field_name for item in request["informationRequests"] if item["status"] == "PENDING" and item["visitorFormId"] == form["id"] for field_name in item["fields"]))
            self.send_json(200, result)
            return
        if path == "/api/dashboard":
            self.send_json(200, dashboard(require_role(user)))
            return
        if path == "/api/ec/dashboard":
            require_role(user, "EXPORT_CONTROL")
            self.send_json(200, compliance_dashboard())
            return
        if path == "/api/reception/dashboard":
            require_role(user, "RECEPTION")
            self.send_json(200, reception_dashboard())
            return
        if path == "/api/analytics":
            actor = require_role(user, "HOST_REQUESTER", "EXPORT_CONTROL", "RECEPTION")
            self.send_json(200, analytics_data(True, actor["id"] if actor["role"] == "HOST_REQUESTER" else None))
            return
        if path == "/api/analytics/export.csv":
            actor = require_role(user, "HOST_REQUESTER", "EXPORT_CONTROL", "RECEPTION")
            self.send_bytes(200, csv_bytes(split.query, True, actor["id"] if actor["role"] == "HOST_REQUESTER" else None), "text/csv; charset=utf-8", "visitor-analytics.csv")
            return
        if path == "/api/analytics/export.xlsx":
            actor = require_role(user, "HOST_REQUESTER", "EXPORT_CONTROL", "RECEPTION")
            self.send_bytes(200, xlsx_bytes(split.query, True, actor["id"] if actor["role"] == "HOST_REQUESTER" else None), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "visitor-analytics.xlsx")
            return
        if path.startswith("/api/"):
            raise ApiError(404, "API route was not found.")
        self.send_bytes(200, rendered_html(), "text/html; charset=utf-8")

    def do_POST(self) -> None:
        self.handle_api_error(self.post_route)

    def post_route(self) -> None:
        path = urlsplit(self.path).path.rstrip("/")
        user = self.current_user()
        if path == "/api/visitor-requests":
            actor = require_role(user, "HOST_REQUESTER")
            self.send_json(201, create_request(self.read_json(), actor))
            return
        if path.startswith("/api/visitor-forms/") and path.endswith("/submit"):
            actor = require_role(user, "HOST_REQUESTER")
            form_id = path.removeprefix("/api/visitor-forms/").removesuffix("/submit").rstrip("/")
            self.send_json(200, submit_form(form_id, self.read_json(), actor))
            return
        if path.startswith("/api/visitor-forms/") and path.endswith("/screening-results"):
            actor = require_role(user, "HOST_REQUESTER")
            if self.headers.get("Content-Type", "").split(";", 1)[0].strip().lower() != "application/pdf":
                raise ApiError(400, "Denied Party Screening Results must be uploaded as a PDF.")
            form_id = path.removeprefix("/api/visitor-forms/").removesuffix("/screening-results").rstrip("/")
            self.send_json(201, save_screening_result(form_id, self.read_bytes(10_485_760), actor))
            return
        if path.startswith("/api/visitor-requests/") and path.endswith("/actions"):
            actor = require_role(user)
            request_id = path.removeprefix("/api/visitor-requests/").removesuffix("/actions").rstrip("/")
            request = find_request(request_id)
            if request is None:
                raise ApiError(404, "Visitor request was not found.")
            self.send_json(200, execute_action(request, self.read_json(), actor))
            return
        raise ApiError(404, "API route was not found.")

    def do_PUT(self) -> None:
        self.handle_api_error(self.put_route)

    def put_route(self) -> None:
        raise ApiError(404, "API route was not found.")


def main() -> None:
    global DATABASE_PATH
    os.umask(0o077)
    parser = argparse.ArgumentParser(description="Run the self-contained visitor management application.")
    parser.add_argument("--host", default=HOST)
    parser.add_argument("--port", type=int, default=PORT)
    parser.add_argument("--database", type=Path, default=DATABASE_PATH)
    parser.add_argument("--no-browser", action="store_true")
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("port must be between 1 and 65535")
    DATABASE_PATH = args.database.expanduser().resolve()
    try:
        initialize_database()
    except RuntimeError as exc:
        parser.error(str(exc))  
    server = ThreadingHTTPServer((args.host, args.port), Handler)
    address = f"http://{args.host}:{args.port}"
    print(f"{APP_TITLE} is running at {address}")
    print("Press Ctrl+C to stop.")
    if not args.no_browser:
        threading.Timer(0.4, lambda: webbrowser.open(address)).start()
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\nStopping server.")
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
