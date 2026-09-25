"""Microsoft Office automation for the JARVIS Windows agent (COM first, UI fallback).

All COM imports are lazy so this module imports cleanly on non-Windows hosts.
"""
from __future__ import annotations

import os
import sys
from datetime import datetime
from pathlib import Path
from typing import Any, Dict

from memory_store import MemoryStore

IS_WINDOWS = sys.platform == "win32"
memory = MemoryStore()


class OfficeError(Exception):
    pass


def _com(app_name: str):
    if not IS_WINDOWS:
        raise OfficeError(f"{app_name} automation requires Windows 11 with Office installed.")
    try:
        import pythoncom  # type: ignore
        import win32com.client  # type: ignore
    except ImportError as e:  # noqa: BLE001
        raise OfficeError("pywin32 is not installed in the agent environment.") from e
    pythoncom.CoInitialize()
    try:
        return win32com.client.gencache.EnsureDispatch(f"{app_name}.Application")
    except Exception:
        return win32com.client.Dispatch(f"{app_name}.Application")


def _documents_dir() -> Path:
    base = Path(os.environ.get("USERPROFILE", str(Path.home()))) / "Documents" / "JARVIS"
    base.mkdir(parents=True, exist_ok=True)
    return base


# ---------------------------------------------------------------------------
# Word
# ---------------------------------------------------------------------------

def word_create(params: Dict[str, Any]) -> Dict[str, Any]:
    body = params.get("body")
    if not body:
        raise OfficeError("No document text was supplied to write into Word.")

    title = str(params.get("title") or "").strip()
    save = bool(params.get("save", True))
    export_pdf = bool(params.get("export_pdf", False))
    filename = str(params.get("filename") or "").strip()

    word = _com("Word")
    word.Visible = True
    doc = word.Documents.Add()

    rng = doc.Content
    if title:
        para = doc.Paragraphs.Add()
        para.Range.Text = title
        para.Range.Font.Size = 16
        para.Range.Font.Bold = True
        para.Range.ParagraphFormat.SpaceAfter = 12

    para = doc.Paragraphs.Add()
    para.Range.Text = str(body)
    para.Range.Font.Size = 11
    para.Range.Font.Bold = False
    para.Range.ParagraphFormat.SpaceAfter = 8

    result: Dict[str, Any] = {"created": True, "visible": True, "chars": len(str(body))}

    if save:
        name = filename or f"jarvis-document-{datetime.now().strftime('%Y%m%d-%H%M%S')}.docx"
        path = _documents_dir() / name
        doc.SaveAs2(str(path))
        result["saved_to"] = str(path)
        memory.remember_document(str(path), title or str(body)[:120])

        if export_pdf:
            pdf_path = path.with_suffix(".pdf")
            try:
                doc.ExportAsFixedFormat(str(pdf_path), 17)  # 17 = wdExportFormatPDF
                result["pdf"] = str(pdf_path)
            except Exception as e:  # noqa: BLE001
                result["pdf_error"] = str(e)[:200]

    memory.remember_task("office.word.create", True, result.get("saved_to", ""))
    _ = rng
    return result


def word_save(params: Dict[str, Any]) -> Dict[str, Any]:
    path = str(params.get("path") or "").strip()
    word = _com("Word")
    if word.Documents.Count == 0:
        raise OfficeError("No open Word document to save.")
    doc = word.ActiveDocument
    if path:
        doc.SaveAs2(path)
    else:
        doc.Save()
        path = doc.FullName
    return {"saved_to": path}


# ---------------------------------------------------------------------------
# Excel
# ---------------------------------------------------------------------------

def excel_create(params: Dict[str, Any]) -> Dict[str, Any]:
    rows = params.get("rows")
    if not rows or not isinstance(rows, list):
        raise OfficeError("No table rows were supplied to write into Excel.")

    sheet_name = str(params.get("sheet_name") or "JARVIS")[:31]
    save = bool(params.get("save", True))
    filename = str(params.get("filename") or "").strip()

    excel = _com("Excel")
    excel.Visible = True
    wb = excel.Workbooks.Add()
    ws = wb.Worksheets(1)
    ws.Name = sheet_name

    for r, row in enumerate(rows, start=1):
        for c, value in enumerate(list(row), start=1):
            ws.Cells(r, c).Value = value

    # Header formatting + autofit
    header = ws.Range(ws.Cells(1, 1), ws.Cells(1, max(1, len(rows[0]))))
    header.Font.Bold = True
    ws.Columns.AutoFit()

    result: Dict[str, Any] = {"created": True, "rows": len(rows), "sheet": sheet_name}

    if save:
        name = filename or f"jarvis-workbook-{datetime.now().strftime('%Y%m%d-%H%M%S')}.xlsx"
        path = _documents_dir() / name
        wb.SaveAs(str(path))
        result["saved_to"] = str(path)
        memory.remember_document(str(path), f"Excel workbook: {sheet_name}")

    memory.remember_task("office.excel.create", True, result.get("saved_to", ""))
    return result


# ---------------------------------------------------------------------------
# Outlook
# ---------------------------------------------------------------------------

def _outlook_namespace():
    outlook = _com("Outlook")
    return outlook, outlook.GetNamespace("MAPI")


def outlook_inbox_summary(params: Dict[str, Any]) -> Dict[str, Any]:
    count = int(params.get("count", 10))
    _outlook, ns = _outlook_namespace()
    inbox = ns.GetDefaultFolder(6)  # olFolderInbox
    items = inbox.Items
    items.Sort("[ReceivedTime]", True)
    summary = []
    for i, item in enumerate(items):
        if i >= count:
            break
        try:
            summary.append({
                "from": getattr(item, "SenderName", ""),
                "subject": getattr(item, "Subject", ""),
                "received": str(getattr(item, "ReceivedTime", "")),
                "unread": bool(getattr(item, "UnRead", False)),
            })
        except Exception:  # noqa: BLE001
            continue
    return {"count": len(summary), "messages": summary}


def outlook_search(params: Dict[str, Any]) -> Dict[str, Any]:
    query = str(params.get("query", "")).strip()
    if not query:
        raise OfficeError("query is required")
    _outlook, ns = _outlook_namespace()
    inbox = ns.GetDefaultFolder(6)
    filt = f"@SQL=(urn:schemas:httpmail:subject LIKE '%{query}%')"
    try:
        found = inbox.Items.Restrict(filt)
    except Exception as e:  # noqa: BLE001
        raise OfficeError(f"Outlook search failed: {e}") from e
    rows = []
    for i, item in enumerate(found):
        if i >= int(params.get("limit", 15)):
            break
        rows.append({"from": getattr(item, "SenderName", ""), "subject": getattr(item, "Subject", ""),
                     "received": str(getattr(item, "ReceivedTime", ""))})
    return {"query": query, "count": len(rows), "messages": rows}


def outlook_draft(params: Dict[str, Any]) -> Dict[str, Any]:
    to = params.get("to")
    subject = params.get("subject") or ""
    body = params.get("body") or ""
    if not body:
        raise OfficeError("No email body was supplied.")

    if to and "@" not in str(to):
        remembered = memory.lookup_contact(str(to))
        if remembered:
            to = remembered

    outlook = _com("Outlook")
    mail = outlook.CreateItem(0)  # olMailItem
    if to:
        mail.To = str(to)
    mail.Subject = str(subject)
    mail.Body = str(body)
    mail.Save()
    mail.Display(False)  # show the draft to the user; never sends
    entry_id = getattr(mail, "EntryID", "")
    memory.remember_task("outlook.draft", True, str(subject)[:120])
    return {"drafted": True, "to": to, "subject": subject, "entry_id": entry_id,
            "note": "Draft created and opened in Outlook. Nothing was sent."}


def outlook_send(params: Dict[str, Any]) -> Dict[str, Any]:
    """Only reachable after the user confirmed in JARVIS (relay enforces this)."""
    entry_id = str(params.get("entry_id", "")).strip()
    outlook, ns = _outlook_namespace()
    if entry_id:
        mail = ns.GetItemFromID(entry_id)
    else:
        to = params.get("to")
        body = params.get("body")
        if not to or not body:
            raise OfficeError("entry_id, or to + body, is required to send an email.")
        mail = outlook.CreateItem(0)
        mail.To = str(to)
        mail.Subject = str(params.get("subject") or "")
        mail.Body = str(body)
    mail.Send()
    memory.remember_task("outlook.send", True, str(params.get("subject", ""))[:120])
    return {"sent": True, "to": params.get("to"), "subject": params.get("subject")}
