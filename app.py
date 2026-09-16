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
from urllib.parse import urlsplit
from xml.sax.saxutils import escape as xml_escape


APP_TITLE = "Visitor Management"
HOST = "127.0.0.1"
PORT = 8000
LOCK = threading.RLock()
IST = timezone(timedelta(hours=5, minutes=30), name="IST")
APP_DIRECTORY = Path(__file__).resolve().parent
INDEX_PATH = APP_DIRECTORY / "index.html"
DATABASE_PATH = Path(os.environ.get("VISITOR_DATABASE_PATH", str(APP_DIRECTORY / "visitor_management.sqlite3")))
INDEX_TEMPLATE = INDEX_PATH.read_text(encoding="utf-8").rstrip("\n")


def iso_now() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def new_id(prefix: str) -> str:
    return f"{prefix}-{uuid.uuid4().hex[:12]}"


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
    "fullName": "Full legal name",
    "designation": "Designation/position held",
    "citizenship": "Citizenship",
    "companyName": "Visitor company",
    "companyAddress": "Company address",
    "officeCity": "Company city",
    "officeCountry": "Company country",
    "phoneCountry": "Phone country code",
    "telephone": "Phone number",
    "email": "Email address",
    "idType": "ID type",
    "otherIdType": "Government-issued ID type",
    "assets": "Declared assets",
}


def make_form(request_id: str, index: int, visitor: dict | None = None, status: str = "DRAFT") -> dict:
    source = visitor or {}
    return {
        "id": new_id("form"),
        "visitorRequestId": request_id,
        "status": status,
        "fullName": source.get("fullName", ""),
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
    }


def reset_reception_records(request: dict) -> None:
    for form in request["visitorForms"]:
        form["receptionRecords"] = [make_reception_record(form["id"], day) for day in request["visitDays"]]


def ensure_request_shape(request: dict) -> dict:
    request.setdefault("screeningRemarks", [])
    request.setdefault("cancellationReason", "")
    request.setdefault("scheduleChanges", [])
    if request.get("visitPurposeType") == "Other":
        request["visitPurposeType"] = "Others"
    if request.get("idClassification") not in {"", "Vendor", "Visitor"}:
        request["idClassification"] = ""
    for form_index, form in enumerate(request["visitorForms"]):
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
            held = record.get("status") == "RECEPTION_HOLD"
            record.setdefault("identityStatus", "HOLD" if held else "APPROVED" if processed else "PENDING")
            record.setdefault("identityHoldReason", record.get("holdReason", "") if held else "")
            record.setdefault("assetsStatus", "APPROVED" if processed else "PENDING")
            record.setdefault("assetsHoldReason", "")
    default_form = request["visitorForms"][0]
    for item in request.get("informationRequests", []):
        item.setdefault("visitorFormId", default_form["id"])
        item.setdefault("visitorName", default_form["fullName"] or "Visitor 1")
        if not isinstance(item.get("fields"), list):
            item["fields"] = ["fullName"]
        item["fields"] = ["citizenship" if field_name == "nationality" else field_name for field_name in item["fields"]]
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


def initialize_database() -> None:
    DATABASE_PATH.parent.mkdir(parents=True, exist_ok=True)
    with database_connection() as connection:
        connection.execute("PRAGMA journal_mode = WAL")
        connection.executescript("""
            CREATE TABLE IF NOT EXISTS app_meta (key TEXT PRIMARY KEY, value INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS requests (
                id TEXT PRIMARY KEY,
                request_number TEXT NOT NULL UNIQUE,
                batch_id TEXT NOT NULL UNIQUE,
                requester_id TEXT NOT NULL,
                main_host_id TEXT NOT NULL,
                main_host_name TEXT NOT NULL,
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
        ensure_column(connection, "reception_records", "badge_type", "TEXT NOT NULL DEFAULT ''")
        ensure_column(connection, "reception_records", "identity_status", "TEXT NOT NULL DEFAULT 'PENDING'")
        ensure_column(connection, "reception_records", "identity_hold_reason", "TEXT NOT NULL DEFAULT ''")
        ensure_column(connection, "reception_records", "assets_status", "TEXT NOT NULL DEFAULT 'PENDING'")
        ensure_column(connection, "reception_records", "assets_hold_reason", "TEXT NOT NULL DEFAULT ''")
        connection.execute("UPDATE reception_records SET identity_status = 'APPROVED', assets_status = 'APPROVED' WHERE status IN ('RECEPTION_VERIFICATION','CHECKED_IN','COMPLETED') AND (identity_status = 'PENDING' OR assets_status = 'PENDING')")
        connection.execute("UPDATE reception_records SET identity_status = 'HOLD', identity_hold_reason = hold_reason WHERE status = 'RECEPTION_HOLD' AND identity_status = 'PENDING' AND assets_status = 'PENDING'")
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


def _persist_request(connection: sqlite3.Connection, request: dict) -> None:
    request = ensure_request_shape(request)
    columns = ["id", "request_number", "batch_id", "requester_id", "main_host_id", "main_host_name", "escorting_host_id", "escorting_host_name", "visitor_type", "facilities_contractor", "research_contractor", "visiting_site", "visitor_count", "purpose", "areas_to_visit", "purpose_type", "legacy_classification", "status", "visit_start", "visit_end", "created_at", "updated_at", "submitted_at", "approved_at", "rejected_at", "rejection_reason", "cancellation_reason"]
    values = [request["id"], request["requestNumber"], request["batchId"], request["requesterId"], request.get("mainHostId", request["requesterId"]), request["mainHostName"], request.get("escortingHostId", ""), request["escortingHostName"], request["visitorType"], int(request["faculty"]), int(request["gtr"]), request["visitingSite"], request["numberOfVisitors"], request["purpose"], request["areasToVisit"], request["visitPurposeType"], request.get("idClassification", ""), request["currentStatus"], request["visitStart"], request["visitEnd"], request["createdAt"], request["updatedAt"], request.get("submittedAt"), request.get("approvedAt"), request.get("rejectedAt"), request.get("rejectionReason", ""), request.get("cancellationReason", "")]
    assignments = ",".join(f"{column}=excluded.{column}" for column in columns[1:])
    connection.execute(f"INSERT INTO requests ({','.join(columns)}) VALUES ({','.join('?' for _ in columns)}) ON CONFLICT(id) DO UPDATE SET {assignments}", values)
    connection.execute("DELETE FROM visitor_forms WHERE request_id = ?", (request["id"],))
    connection.execute("DELETE FROM visit_days WHERE request_id = ?", (request["id"],))
    connection.execute("DELETE FROM ec_reviews WHERE request_id = ?", (request["id"],))
    for day in request["visitDays"]:
        connection.execute("INSERT INTO visit_days (id,request_id,visit_date,arrival_time,departure_time) VALUES (?,?,?,?,?)", (day["id"], request["id"], day["visitDate"], day["expectedArrivalTime"], day["expectedDepartureTime"]))
    for form in request["visitorForms"]:
        connection.execute("INSERT INTO visitor_forms (id,request_id,sequence,status,full_name,citizenship,designation,company_name,company_address,office_city,office_country,phone_country,phone_dial_code,telephone,email,id_type,other_id_type,ec_decision,classification,ec_decision_reason,ec_decision_at) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", (form["id"], request["id"], form["sequence"], form["status"], form["fullName"], form["citizenship"], form["designation"], form["companyName"], form["companyAddress"], form["officeCity"], form["officeCountry"], form["phoneCountry"], form["phoneDialCode"], form["telephone"], form["email"], form["idType"], form["otherIdType"], form["ecDecision"], form["idClassification"], form["ecDecisionReason"], form["ecDecisionAt"]))
        for position, asset in enumerate(form["assets"]):
            connection.execute("INSERT INTO assets (id,visitor_form_id,position,asset_type,description,serial_number,verification_status) VALUES (?,?,?,?,?,?,?)", (asset["id"], form["id"], position, asset["assetType"], asset["description"], asset["serialNumber"], asset["verificationStatus"]))
        for record in form["receptionRecords"]:
            connection.execute("INSERT INTO reception_records (id,visitor_form_id,visit_day_id,status,actual_arrival_time,actual_departure_time,badge,badge_type,badge_returned_at,hold_reason,identity_status,identity_hold_reason,assets_status,assets_hold_reason) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?)", (record["id"], form["id"], record["visitDayId"], record["status"], record["actualArrivalTime"], record["actualDepartureTime"], record["badge"], record.get("badgeType", ""), record.get("badgeReturnedAt"), record["holdReason"], record["identityStatus"], record["identityHoldReason"], record["assetsStatus"], record["assetsHoldReason"]))
        for version in form["versions"]:
            connection.execute("INSERT INTO form_versions (id,visitor_form_id,version,created_at,snapshot) VALUES (?,?,?,?,?)", (version.get("id", new_id("version")), form["id"], version["version"], version["createdAt"], json.dumps(version["snapshot"], ensure_ascii=False, separators=(",", ":"))))
    form_ids = {form["id"] for form in request["visitorForms"]}
    for item in request["screeningRemarks"]:
        if item["visitorFormId"] in form_ids:
            connection.execute("INSERT INTO screening_remarks (id,request_id,visitor_form_id,remark,created_by,created_at) VALUES (?,?,?,?,?,?)", (item["id"], request["id"], item["visitorFormId"], item["remark"], item["createdBy"], item["createdAt"]))
    for review in request["ecReviews"]:
        connection.execute("INSERT INTO ec_reviews (id,request_id,reviewer_id,status,decision,comments,reviewed_at) VALUES (?,?,?,?,?,?,?)", (review["id"], request["id"], review["reviewerId"], review["status"], review["decision"], review["comments"], review["reviewedAt"]))
        for position, form_id in enumerate(review.get("visitorFormIds", [])):
            if form_id in form_ids:
                connection.execute("INSERT INTO ec_review_targets (review_id,visitor_form_id,position) VALUES (?,?,?)", (review["id"], form_id, position))
    for item in request["informationRequests"]:
        if item["visitorFormId"] not in form_ids:
            continue
        connection.execute("INSERT INTO information_requests (id,request_id,visitor_form_id,comment,status,created_at,responded_at) VALUES (?,?,?,?,?,?,?)", (item["id"], request["id"], item["visitorFormId"], item["comment"], item["status"], item["createdAt"], item.get("respondedAt")))
        for position, field_name in enumerate(item["fields"]):
            connection.execute("INSERT INTO information_request_fields (information_request_id,position,field_name,original_value) VALUES (?,?,?,?)", (item["id"], position, field_name, json.dumps(item["originalValues"].get(field_name, ""), ensure_ascii=False, separators=(",", ":"))))
        for position, change in enumerate(item["changes"]):
            connection.execute("INSERT INTO information_changes (information_request_id,position,field_name,before_value,after_value) VALUES (?,?,?,?,?)", (item["id"], position, change["field"], json.dumps(change["before"], ensure_ascii=False, separators=(",", ":")), json.dumps(change["after"], ensure_ascii=False, separators=(",", ":"))))
    for item in request["scheduleChanges"]:
        connection.execute("INSERT OR IGNORE INTO schedule_changes (id,request_id,old_start,old_end,new_start,new_end,reason,changed_by,changed_at) VALUES (?,?,?,?,?,?,?,?,?)", (item["id"], request["id"], item["oldStart"], item["oldEnd"], item["newStart"], item["newEnd"], item["reason"], item["changedBy"], item["changedAt"]))
    for item in request["auditHistory"]:
        connection.execute("INSERT OR IGNORE INTO audit_events (id,request_id,action,details,created_at) VALUES (?,?,?,?,?)", (item["id"], request["id"], item["action"], item["details"], item["createdAt"]))


def _load_requests(connection: sqlite3.Connection, request_id: str | None = None) -> list[dict]:
    query = "SELECT * FROM requests"
    parameters: tuple = ()
    if request_id is not None:
        query += " WHERE id = ?"
        parameters = (request_id,)
    query += " ORDER BY created_at DESC"
    results = []
    for row in connection.execute(query, parameters):
        request = {"id":row["id"],"requestNumber":row["request_number"],"batchId":row["batch_id"],"requesterId":row["requester_id"],"mainHostId":row["main_host_id"],"mainHostName":row["main_host_name"],"escortingHostId":row["escorting_host_id"],"escortingHostName":row["escorting_host_name"],"visitorType":row["visitor_type"],"faculty":bool(row["facilities_contractor"]),"gtr":bool(row["research_contractor"]),"visitingSite":row["visiting_site"],"numberOfVisitors":row["visitor_count"],"purpose":row["purpose"],"areasToVisit":row["areas_to_visit"],"visitPurposeType":row["purpose_type"],"idClassification":row["legacy_classification"],"currentStatus":row["status"],"visitStart":row["visit_start"],"visitEnd":row["visit_end"],"createdAt":row["created_at"],"updatedAt":row["updated_at"],"submittedAt":row["submitted_at"],"approvedAt":row["approved_at"],"rejectedAt":row["rejected_at"],"rejectionReason":row["rejection_reason"],"cancellationReason":row["cancellation_reason"],"visitorForms":[],"visitDays":[],"screeningRemarks":[],"ecReviews":[],"comments":[],"informationRequests":[],"scheduleChanges":[],"auditHistory":[]}
        for day in connection.execute("SELECT * FROM visit_days WHERE request_id = ? ORDER BY visit_date", (request["id"],)):
            request["visitDays"].append({"id":day["id"],"visitDate":day["visit_date"],"expectedArrivalTime":day["arrival_time"],"expectedDepartureTime":day["departure_time"]})
        for form_row in connection.execute("SELECT * FROM visitor_forms WHERE request_id = ? ORDER BY sequence", (request["id"],)):
            form = {"id":form_row["id"],"visitorRequestId":request["id"],"sequence":form_row["sequence"],"status":form_row["status"],"fullName":form_row["full_name"],"citizenship":form_row["citizenship"],"designation":form_row["designation"],"companyName":form_row["company_name"],"companyAddress":form_row["company_address"],"officeCity":form_row["office_city"],"officeCountry":form_row["office_country"],"phoneCountry":form_row["phone_country"],"phoneDialCode":form_row["phone_dial_code"],"telephone":form_row["telephone"],"email":form_row["email"],"idType":form_row["id_type"],"otherIdType":form_row["other_id_type"],"ecDecision":form_row["ec_decision"],"idClassification":form_row["classification"],"ecDecisionReason":form_row["ec_decision_reason"],"ecDecisionAt":form_row["ec_decision_at"],"assets":[],"receptionRecords":[],"versions":[]}
            for asset in connection.execute("SELECT * FROM assets WHERE visitor_form_id = ? ORDER BY position", (form["id"],)):
                form["assets"].append({"id":asset["id"],"assetType":asset["asset_type"],"description":asset["description"],"serialNumber":asset["serial_number"],"verificationStatus":asset["verification_status"]})
            for record in connection.execute("SELECT * FROM reception_records WHERE visitor_form_id = ?", (form["id"],)):
                form["receptionRecords"].append({"id":record["id"],"visitorFormId":form["id"],"visitDayId":record["visit_day_id"],"status":record["status"],"actualArrivalTime":record["actual_arrival_time"],"actualDepartureTime":record["actual_departure_time"],"badge":record["badge"],"badgeType":record["badge_type"],"badgeReturnedAt":record["badge_returned_at"],"holdReason":record["hold_reason"],"identityStatus":record["identity_status"],"identityHoldReason":record["identity_hold_reason"],"assetsStatus":record["assets_status"],"assetsHoldReason":record["assets_hold_reason"]})
            for version in connection.execute("SELECT * FROM form_versions WHERE visitor_form_id = ? ORDER BY version", (form["id"],)):
                snapshot = json.loads(version["snapshot"])
                if "citizenship" not in snapshot:
                    snapshot["citizenship"] = snapshot.pop("nationality", "")
                form["versions"].append({"id":version["id"],"version":version["version"],"createdAt":version["created_at"],"snapshot":snapshot})
            request["visitorForms"].append(form)
        names = {form["id"]: form["fullName"] or f"Visitor {form['sequence']}" for form in request["visitorForms"]}
        for item in connection.execute("SELECT * FROM screening_remarks WHERE request_id = ? ORDER BY created_at DESC", (request["id"],)):
            request["screeningRemarks"].append({"id":item["id"],"visitorFormId":item["visitor_form_id"],"visitorName":names.get(item["visitor_form_id"], "Visitor"),"remark":item["remark"],"createdBy":item["created_by"],"createdAt":item["created_at"]})
        for review in connection.execute("SELECT * FROM ec_reviews WHERE request_id = ? ORDER BY reviewed_at DESC", (request["id"],)):
            targets = [target["visitor_form_id"] for target in connection.execute("SELECT visitor_form_id FROM ec_review_targets WHERE review_id = ? ORDER BY position", (review["id"],))]
            request["ecReviews"].append({"id":review["id"],"reviewerId":review["reviewer_id"],"status":review["status"],"decision":review["decision"],"comments":review["comments"],"reviewedAt":review["reviewed_at"],"visitorFormIds":targets,"visitorNames":[names.get(form_id, "Visitor") for form_id in targets]})
        for item in connection.execute("SELECT * FROM information_requests WHERE request_id = ? ORDER BY created_at DESC", (request["id"],)):
            fields = list(connection.execute("SELECT * FROM information_request_fields WHERE information_request_id = ? ORDER BY position", (item["id"],)))
            changes = list(connection.execute("SELECT * FROM information_changes WHERE information_request_id = ? ORDER BY position", (item["id"],)))
            field_names = [field["field_name"] for field in fields]
            request["informationRequests"].append({"id":item["id"],"visitorFormId":item["visitor_form_id"],"visitorName":names.get(item["visitor_form_id"], "Visitor"),"fields":field_names,"fieldLabels":[REQUESTABLE_FIELDS.get(name, name) for name in field_names],"comment":item["comment"],"originalValues":{field["field_name"]:json.loads(field["original_value"]) for field in fields},"changes":[{"field":change["field_name"],"before":json.loads(change["before_value"]),"after":json.loads(change["after_value"])} for change in changes],"status":item["status"],"createdAt":item["created_at"],"respondedAt":item["responded_at"]})
        for item in connection.execute("SELECT * FROM schedule_changes WHERE request_id = ? ORDER BY changed_at DESC", (request["id"],)):
            request["scheduleChanges"].append({"id":item["id"],"oldStart":item["old_start"],"oldEnd":item["old_end"],"newStart":item["new_start"],"newEnd":item["new_end"],"reason":item["reason"],"changedBy":item["changed_by"],"changedAt":item["changed_at"]})
        for item in connection.execute("SELECT * FROM audit_events WHERE request_id = ? ORDER BY created_at DESC", (request["id"],)):
            request["auditHistory"].append({"id":item["id"],"action":item["action"],"details":item["details"],"createdAt":item["created_at"]})
        results.append(ensure_request_shape(request))
    return results


def load_requests() -> list[dict]:
    with database_connection() as connection:
        return _load_requests(connection)


def save_request(request: dict) -> None:
    with database_connection() as connection:
        _persist_request(connection, request)


def next_sequence() -> int:
    with database_connection() as connection:
        connection.execute("BEGIN IMMEDIATE")
        value = connection.execute("SELECT value FROM app_meta WHERE key = 'request_sequence'").fetchone()[0] + 1
        connection.execute("UPDATE app_meta SET value = ? WHERE key = 'request_sequence'", (value,))
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


def list_item(request: dict) -> dict:
    forms = request["visitorForms"]
    visitor_names = [form["fullName"] or "Visitor details pending" for form in forms]
    company_names = list(dict.fromkeys(form["companyName"] for form in forms if form["companyName"]))
    visitor_count = len(forms)
    today = datetime.now(IST).date().isoformat()
    entries = reception_entries([request])
    return {
        "id": request["id"],
        "requestNumber": request["requestNumber"],
        "batchId": request["batchId"],
        "visitorName": visitor_names[0] if visitor_count == 1 else "",
        "companyName": company_names[0] if visitor_count == 1 and company_names else "",
        "visitorCount": visitor_count,
        "visitorNames": visitor_names,
        "companyNames": company_names,
        "completedVisitorForms": sum(form["status"] == "SUBMITTED" for form in forms),
        "currentStatus": request["currentStatus"],
        "createdAt": request["createdAt"],
        "visitDate": request["visitDays"][0]["visitDate"] if request["visitDays"] else "",
        "hostName": request["mainHostName"],
        "currentStage": request["currentStatus"],
        "lastUpdated": request["updatedAt"],
        "requesterId": request["requesterId"],
        "hasToday": any(day["visitDate"] == today for _, _, day, _ in entries),
        "hasCheckedIn": any(record["status"] == "CHECKED_IN" for _, _, _, record in entries),
        "hasCheckedOut": any(record["status"] == "COMPLETED" for _, _, _, record in entries),
        "hasNoShow": any(record["status"] == "NO_SHOW" for _, _, _, record in entries),
        "hasReceptionHold": any(record["status"] == "RECEPTION_HOLD" for _, _, _, record in entries),
    }


def request_detail(request: dict, include_screening: bool = False) -> dict:
    assets = []
    versions = []
    visitors = []
    for visitor_form in request["visitorForms"]:
        assets.extend(deepcopy(visitor_form["assets"]))
        phone = ""
        if visitor_form["telephone"]:
            prefix = f"+{visitor_form['phoneDialCode']} " if visitor_form["phoneDialCode"] else ""
            phone = f"{prefix}{visitor_form['telephone']}"
        visitors.append({
            "id": visitor_form["id"],
            "sequence": visitor_form["sequence"],
            "fullName": visitor_form["fullName"] or "Visitor details pending",
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
            "assets": deepcopy(visitor_form["assets"]),
            "receptionRecords": deepcopy(visitor_form["receptionRecords"]),
        })
        for version in visitor_form["versions"]:
            snapshot = version["snapshot"]
            versions.append({"id": version.get("id", ""), "visitorFormId": visitor_form["id"], "version": version["version"], "fullName": snapshot.get("fullName", ""), "citizenship": snapshot.get("citizenship", snapshot.get("nationality", "")), "company": snapshot.get("companyName", ""), "designation": snapshot.get("designation", ""), "idType": snapshot.get("idType", ""), "assets": json.dumps(snapshot.get("assets", [])), "createdAt": version["createdAt"]})
    return {
        "id": request["id"],
        "requestNumber": request["requestNumber"],
        "batchId": request["batchId"],
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
        "escortingHostName": request["escortingHostName"],
        "faculty": request["faculty"],
        "gtr": request["gtr"],
        "idClassification": request["idClassification"],
        "currentStatus": request["currentStatus"],
        "cancellationReason": request.get("cancellationReason", ""),
        "visitorFormIds": [item["id"] for item in request["visitorForms"]],
        "visitorForms": [{"id": item["id"], "status": item["status"], "fullName": item["fullName"], "ecDecision": item["ecDecision"], "idClassification": item["idClassification"], "personType": person_type(request, item), "badgeType": badge_type(request, item), "ecDecisionReason": item["ecDecisionReason"], "receptionRecords": deepcopy(item["receptionRecords"])} for item in request["visitorForms"]],
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
    return {
        "totalRequests": len(requests),
        "pendingActions": sum(item["currentStatus"] in pending for item in requests),
        "todaysVisits": sum(day["visitDate"] == today for _, _, day, _ in entries),
        "currentlyInside": sum(record["status"] == "CHECKED_IN" for _, _, _, record in entries),
        "upcomingVisits": sum(day["visitDate"] > today and record["status"] == "UPCOMING" for _, _, day, record in entries),
        "noShows": sum(record["status"] == "NO_SHOW" for _, _, _, record in entries),
        "pendingEcReviews": sum(item["currentStatus"] in {"PENDING_EC_REVIEW", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"} for item in requests),
        "pendingDocumentation": sum(item["currentStatus"] == "PENDING_DOCUMENTATION" for item in requests),
        "approved": sum(item["currentStatus"] in {"APPROVED", "PARTIALLY_APPROVED"} for item in requests),
        "checkedOut": sum(record["status"] == "COMPLETED" for _, _, _, record in entries),
        "recentRequests": [list_item(item) for item in sorted(requests, key=lambda value: value["createdAt"], reverse=True)[:10]],
    }


def compliance_dashboard() -> dict:
    requests = load_requests()
    pending = [item for item in requests if item["currentStatus"] in {"PENDING_EC_REVIEW", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"}]
    docs = [item for item in requests if item["currentStatus"] == "PENDING_DOCUMENTATION"]
    holds = sum(record["status"] == "RECEPTION_HOLD" for _, _, _, record in reception_entries(requests))
    return {
        "pendingEcReviews": len(pending),
        "pendingDocumentation": len(docs),
        "receptionHolds": holds,
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
    for request, form, day, record in reception_entries(requests):
        if form["ecDecision"] != "APPROVED":
            continue
        if day["visitDate"] != today and record["status"] not in {"CHECKED_IN", "RECEPTION_HOLD"}:
            continue
        items.append({
            "id": record["id"],
            "visitDayId": day["id"],
            "visitorFormId": form["id"],
            "visitDate": day["visitDate"],
            "status": record["status"],
            "requestId": request["id"],
            "requestNumber": request["requestNumber"],
            "batchId": request["batchId"],
            "visitorName": form["fullName"] or "Visitor details pending",
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
            "assets": deepcopy(form["assets"]),
        })
    return {
        "todaysVisitors": sum(item["visitDate"] == today for item in items),
        "expected": sum(item["status"] in {"UPCOMING", "VERIFICATION_IN_PROGRESS", "RECEPTION_VERIFICATION"} for item in items),
        "onHold": sum(item["status"] == "RECEPTION_HOLD" for item in items),
        "currentlyInside": sum(item["status"] == "CHECKED_IN" for item in items),
        "checkedOut": sum(item["status"] == "COMPLETED" for item in items),
        "noShow": sum(item["status"] == "NO_SHOW" for item in items),
        "items": items,
    }


def analytics_data() -> dict:
    statuses: dict[str, int] = {}
    rows = []
    requests = load_requests()
    for request in requests:
        statuses[request["currentStatus"]] = statuses.get(request["currentStatus"], 0) + 1
        for form in request["visitorForms"]:
            assigned_badge = next((record["badge"] for record in form["receptionRecords"] if record["badge"]), "")
            for day in request["visitDays"] or [{}]:
                rows.append({"requestNumber": request["requestNumber"], "visitor": form["fullName"], "company": form["companyName"], "visitDate": day.get("visitDate", ""), "status": request["currentStatus"], "screeningDecision": form["ecDecision"], "tag": form["idClassification"], "personType": person_type(request, form), "badgeType": badge_type(request, form), "badgeId": assigned_badge, "createdAt": request["createdAt"]})
    return {"totalRequests": len(requests), "byStatus": statuses, "rows": rows}


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
    "verify-identity": "RECEPTION",
    "verify-assets": "RECEPTION",
    "check-in": "RECEPTION",
    "check-out": "RECEPTION",
    "no-show": "RECEPTION",
}
WORKFLOW_ACTION_STATES = {
    "host-review": {"VISITOR_FORM_SUBMITTED"},
    "send-to-ec": {"HOST_REVIEW"},
    "reschedule": {"DRAFT", "VISITOR_FORM_PENDING", "VISITOR_FORM_SUBMITTED", "HOST_REVIEW", "PENDING_EC_REVIEW", "PENDING_DOCUMENTATION", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED", "APPROVED", "PARTIALLY_APPROVED"},
    "cancel": {"DRAFT", "VISITOR_FORM_PENDING", "VISITOR_FORM_SUBMITTED", "HOST_REVIEW", "PENDING_EC_REVIEW", "PENDING_DOCUMENTATION", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED", "APPROVED", "PARTIALLY_APPROVED"},
    "ec-approve": {"PENDING_EC_REVIEW", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"},
    "ec-reject": {"PENDING_EC_REVIEW", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"},
    "ec-request-documents": {"PENDING_EC_REVIEW", "DOCUMENTATION_SUBMITTED", "EC_RE_REVIEW_REQUIRED"},
    "verify-identity": {"APPROVED", "PARTIALLY_APPROVED"},
    "verify-assets": {"APPROVED", "PARTIALLY_APPROVED"},
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
    required = ["visitingSite", "visitPurposeType", "purpose", "areasToVisit", "mainHostName"]
    if any(not str(payload.get(key, "")).strip() for key in required):
        raise ApiError(400, "Complete all required request fields.")
    visiting_site = str(payload["visitingSite"]).strip()
    if visiting_site not in {"Bengaluru", "Delhi"}:
        raise ApiError(400, "Site/facility must be Bengaluru or Delhi.")
    purpose_type = str(payload["visitPurposeType"]).strip()
    if purpose_type not in PURPOSE_TYPES:
        raise ApiError(400, "Select a valid purpose of visit.")
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
    sequence = next_sequence()
    request_id = new_id("request")
    now = iso_now()
    request = {
        "id": request_id,
        "requestNumber": f"VIS-{datetime.now(IST).year}-{sequence:06d}",
        "batchId": f"BATCH-{datetime.now(IST).year}-{uuid.uuid4().hex[:6].upper()}",
        "requesterId": user["id"],
        "mainHostId": user["id"],
        "mainHostName": str(payload["mainHostName"]).strip(),
        "escortingHostId": "",
        "escortingHostName": str(payload.get("escortingHostName", "")).strip(),
        "visitorType": visitor_type,
        "faculty": visitor_type == "External" and bool(payload.get("faculty")),
        "gtr": visitor_type == "External" and bool(payload.get("gtr")),
        "visitingSite": visiting_site,
        "numberOfVisitors": count,
        "purpose": str(payload["purpose"]).strip(),
        "areasToVisit": str(payload["areasToVisit"]).strip(),
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
    return request_detail(request, user["role"] == "EXPORT_CONTROL")


def validate_visitor_form(payload: dict) -> None:
    required = ["fullName", "designation", "companyName", "companyAddress", "officeCity", "officeCountry", "phoneCountry", "telephone", "idType"]
    if any(not str(payload.get(key, "")).strip() for key in required):
        raise ApiError(400, "Complete all required visitor fields.")
    citizenship = str(payload.get("citizenship", "")).strip()
    if citizenship and citizenship not in COUNTRY_NAMES:
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


def submit_form(form_id: str, payload: dict, user: dict) -> dict:
    located = find_form(form_id)
    if located is None:
        raise ApiError(404, "Visitor form was not found.")
    request, form = located
    require_host_owner(request, user)
    if request["currentStatus"] in {"APPROVED", "REJECTED", "CANCELLED", "VISIT_PROCESS_COMPLETED"}:
        raise ApiError(409, "This visitor form can no longer be changed.")
    if form["status"] == "SUBMITTED" and request["currentStatus"] not in {"VISITOR_FORM_PENDING", "VISITOR_FORM_SUBMITTED", "HOST_REVIEW", "PENDING_DOCUMENTATION", "DOCUMENTATION_SUBMITTED"}:
        raise ApiError(409, "This visitor form is locked while Export Control reviews the request.")
    validate_visitor_form(payload)
    fields = ["fullName", "citizenship", "designation", "companyName", "companyAddress", "officeCity", "officeCountry", "phoneCountry", "telephone", "email", "idType", "otherIdType"]
    candidate = {key: str(payload.get(key, "")).strip() for key in fields}
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
        add_audit(request, "ADDITIONAL_INFORMATION_SUBMITTED", f"Visitor form version {len(form['versions'])} submitted.")
    else:
        request["currentStatus"] = "VISITOR_FORM_SUBMITTED" if all(item["status"] == "SUBMITTED" for item in request["visitorForms"]) else "VISITOR_FORM_PENDING"
        add_audit(request, "VISITOR_FORM_SUBMITTED", f"Visitor {form['sequence']} submitted their form.")
    save_request(request)
    return request_detail(request)


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


def update_reception_verification(record: dict) -> None:
    if record["status"] in {"CHECKED_IN", "COMPLETED", "NO_SHOW", "ENTRY_REJECTED", "CANCELLED"}:
        return
    if "HOLD" in {record["identityStatus"], record["assetsStatus"]}:
        record["status"] = "RECEPTION_HOLD"
        record["holdReason"] = record["identityHoldReason"] or record["assetsHoldReason"]
    elif record["identityStatus"] == "APPROVED" and record["assetsStatus"] == "APPROVED":
        record["status"] = "RECEPTION_VERIFICATION"
        record["holdReason"] = ""
    elif "APPROVED" in {record["identityStatus"], record["assetsStatus"]}:
        record["status"] = "VERIFICATION_IN_PROGRESS"
        record["holdReason"] = ""
    else:
        record["status"] = "UPCOMING"
        record["holdReason"] = ""


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
        if any(record["status"] in {"VERIFICATION_IN_PROGRESS", "RECEPTION_VERIFICATION", "RECEPTION_HOLD", "CHECKED_IN", "COMPLETED", "NO_SHOW"} for record in all_reception_records(request)):
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
                        if record["status"] == "ENTRY_REJECTED":
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
    elif action in {"verify-identity", "verify-assets", "check-in", "check-out", "no-show"}:
        form, day, record = selected_visit(request, str(payload.get("visitorFormId", "")), str(payload.get("visitDayId", "")))
        visitor_name = form["fullName"] or f"Visitor {form['sequence']}"
        audit_context = f"Visitor: {visitor_name}. Visit date: {day['visitDate']}."
        if form["ecDecision"] != "APPROVED":
            raise ApiError(409, "Only an approved visitor can be processed at reception.")
        today = datetime.now(IST).date().isoformat()
        if action in {"verify-identity", "verify-assets", "check-in"} and day["visitDate"] != today:
            raise ApiError(409, "Reception processing is available only on the scheduled visit date.")
        if action == "no-show" and day["visitDate"] > today:
            raise ApiError(409, "A future visit cannot be marked as a no-show.")
        if action in {"verify-identity", "verify-assets"}:
            if record["status"] not in {"UPCOMING", "VERIFICATION_IN_PROGRESS", "RECEPTION_VERIFICATION", "RECEPTION_HOLD"}:
                raise ApiError(409, "Verification is not available for this visit.")
            decision = str(payload.get("decision", "")).strip().upper()
            if decision not in {"APPROVE", "HOLD"}:
                raise ApiError(400, "Select Approve or Hold.")
            reason = str(payload.get("reason", "")).strip()
            if decision == "HOLD" and not reason:
                raise ApiError(400, "Enter a hold reason.")
            if action == "verify-identity":
                record["identityStatus"] = "APPROVED" if decision == "APPROVE" else "HOLD"
                record["identityHoldReason"] = "" if decision == "APPROVE" else reason
                audit_context += f" Identity: {decision.title()}." + (f" Reason: {reason}." if reason else "")
            else:
                record["assetsStatus"] = "APPROVED" if decision == "APPROVE" else "HOLD"
                record["assetsHoldReason"] = "" if decision == "APPROVE" else reason
                for asset in form["assets"]:
                    asset["verificationStatus"] = "Verified" if decision == "APPROVE" else "Hold"
                serial_number = str(payload.get("assetSerial", "")).strip()
                if decision == "HOLD" and serial_number:
                    if any(item["serialNumber"].casefold() == serial_number.casefold() for item in form["assets"]):
                        raise ApiError(409, "This asset is already recorded for the visitor.")
                    form["assets"].append({"id": new_id("asset"), "assetType": "Undeclared asset", "description": reason, "serialNumber": serial_number, "verificationStatus": "Hold"})
                audit_context += f" Assets: {decision.title()}." + (f" Reason: {reason}." if reason else "")
            update_reception_verification(record)
        elif action == "check-in":
            if record["identityStatus"] != "APPROVED" or record["assetsStatus"] != "APPROVED" or record["status"] != "RECEPTION_VERIFICATION":
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
            if record["status"] not in {"UPCOMING", "VERIFICATION_IN_PROGRESS", "RECEPTION_VERIFICATION"}:
                raise ApiError(409, "This visit cannot be marked as a no-show.")
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
        "verify-identity": "Identity verification updated",
        "verify-assets": "Asset verification updated",
        "check-in": "Visitor checked in",
        "check-out": "Visitor checked out",
        "no-show": "Visitor marked as a no-show",
    }
    add_audit(request, action.upper().replace("-", "_"), f"{user['name']}: {action_labels[action]}." + (f" {audit_context}" if audit_context else ""))
    save_request(request)
    return request_detail(request, user["role"] == "EXPORT_CONTROL")


def spreadsheet_text(value: object) -> str:
    text = str(value)
    return f"'{text}" if text.startswith(("=", "+", "-", "@", "\t", "\r")) else text


def csv_bytes() -> bytes:
    output = io.StringIO()
    writer = csv.writer(output)
    writer.writerow(["Request", "Visitor", "Company", "Person type", "Visit date", "Request status", "Screening decision", "Badge type", "Badge ID", "Created"])
    for row in analytics_data()["rows"]:
        writer.writerow([spreadsheet_text(row["requestNumber"]), spreadsheet_text(row["visitor"]), spreadsheet_text(row["company"]), spreadsheet_text(row["personType"]), spreadsheet_text(row["visitDate"]), spreadsheet_text(row["status"]), spreadsheet_text(row["screeningDecision"]), spreadsheet_text(row["badgeType"]), spreadsheet_text(row["badgeId"]), spreadsheet_text(row["createdAt"])])
    return output.getvalue().encode("utf-8-sig")


def xlsx_bytes() -> bytes:
    rows = [["Request", "Visitor", "Company", "Person type", "Visit date", "Request status", "Screening decision", "Badge type", "Badge ID", "Created"]]
    rows.extend([[row["requestNumber"], row["visitor"], row["company"], row["personType"], row["visitDate"], row["status"], row["screeningDecision"], row["badgeType"], row["badgeId"], row["createdAt"]] for row in analytics_data()["rows"]])
    sheet_rows = []
    for row_number, row in enumerate(rows, 1):
        cells = []
        for column_number, value in enumerate(row, 1):
            number = column_number
            letters = ""
            while number:
                number, remainder = divmod(number - 1, 26)
                letters = chr(65 + remainder) + letters
            style = ' s="1"' if row_number == 1 else ""
            cells.append(f'<c r="{letters}{row_number}" t="inlineStr"{style}><is><t>{xml_escape(str(value))}</t></is></c>')
        sheet_rows.append(f'<row r="{row_number}">{"".join(cells)}</row>')
    files = {
        "[Content_Types].xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>',
        "_rels/.rels": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>',
        "xl/workbook.xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Visitor Analytics" sheetId="1" r:id="rId1"/></sheets></workbook>',
        "xl/_rels/workbook.xml.rels": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>',
        "xl/styles.xml": '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><fonts count="2"><font><sz val="11"/><name val="Arial"/></font><font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Arial"/></font></fonts><fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF10069F"/><bgColor indexed="64"/></patternFill></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/></cellXfs></styleSheet>',
        "xl/worksheets/sheet1.xml": f'<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><cols><col min="1" max="10" width="24" customWidth="1"/></cols><sheetData>{"".join(sheet_rows)}</sheetData><autoFilter ref="A1:J{len(rows)}"/></worksheet>',
    }
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, content in files.items():
            archive.writestr(name, content)
    return buffer.getvalue()




def rendered_html() -> bytes:
    countries = [{**item, "lengths": list(phone_lengths(item["name"]))} for item in COUNTRIES]
    return INDEX_TEMPLATE.replace("__USERS__", json.dumps(USERS, separators=(",", ":"))).replace("__COUNTRIES__", json.dumps(countries, ensure_ascii=False, separators=(",", ":"))).encode("utf-8")


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
            self.send_json(200, request_detail(request, user["role"] == "EXPORT_CONTROL"))
            return
        if path.startswith("/api/visitor-forms/"):
            actor = require_role(user, "HOST_REQUESTER")
            form_id = path.removeprefix("/api/visitor-forms/")
            located = find_form(form_id)
            if located is None:
                raise ApiError(404, "Visitor form was not found.")
            request, form = located
            require_host_owner(request, actor)
            result = deepcopy(form)
            result["requestNumber"] = request["requestNumber"]
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
            require_role(user, "EXPORT_CONTROL")
            self.send_json(200, analytics_data())
            return
        if path == "/api/analytics/export.csv":
            require_role(user, "EXPORT_CONTROL")
            self.send_bytes(200, csv_bytes(), "text/csv; charset=utf-8", "visitor-analytics.csv")
            return
        if path == "/api/analytics/export.xlsx":
            require_role(user, "EXPORT_CONTROL")
            self.send_bytes(200, xlsx_bytes(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "visitor-analytics.xlsx")
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
    parser = argparse.ArgumentParser(description="Run the self-contained visitor management application.")
    parser.add_argument("--host", default=HOST)
    parser.add_argument("--port", type=int, default=PORT)
    parser.add_argument("--database", type=Path, default=DATABASE_PATH)
    parser.add_argument("--no-browser", action="store_true")
    args = parser.parse_args()
    if not 1 <= args.port <= 65535:
        parser.error("port must be between 1 and 65535")
    DATABASE_PATH = args.database.expanduser().resolve()
    initialize_database()
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