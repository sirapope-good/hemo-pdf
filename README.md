# Hemo-PDF

บริการสร้าง PDF แยกสำหรับ **HemodialysisPro** — ออกแบบให้ทำงานเป็น service ของตัวเอง ไม่ปนกับ business API หลัก

---

## Hemo-PDF คืออะไร

Repo นี้เป็น **โมดูลเสริม** สำหรับออกเอกสาร PDF ของระบบ HemodialysisPro โดย:

- รับข้อมูลรายงาน (DTO) จาก HemodialysisPro ผ่าน REST API
- สร้าง PDF ฝั่ง server ด้วย QuestPDF
- คืนไฟล์ `application/pdf` ให้ Angular เปิดหรือดาวน์โหลด

HemodialysisPro ยังคงดูแลข้อมูลคนไข้ สิทธิ์การใช้งาน และ workflow หลัก — Hemo-PDF ดูแลเฉพาะเรื่อง **layout, branding และการ render PDF**

---

## สถาปัตยกรรมโดยย่อ

```
┌─────────────────────┐         HTTP          ┌─────────────────────┐
│  HemodialysisPro    │  POST /api/pdf/...    │  Hemo.Pdf.Api       │
│  Angular + Web.Api  │ ────────────────────► │  (Standalone)       │
│  โหลดข้อมูล + DTO   │  JWT + X-Tenant-Code │  Render PDF เท่านั้น │
└─────────────────────┘                       └─────────────────────┘
```

| ส่วน | หน้าที่ |
|------|---------|
| **Hemo.Pdf.Api** | API หลัก — deploy แยก (เช่น port `5090`) |
| **Libraries** | Core, Sections, Layouts, Branding, Rendering |
| **@hemo/pdf-client** | Angular library สำหรับเรียก PDF API |

---

## จุดเด่น

- **แยกความรับผิดชอบ** — PDF logic ไม่อยู่ใน `Hemo-backend`
- **Custom header ต่อลูกค้า** — logo, ชื่อหน่วยงาน, ที่อยู่ ตาม tenant
- **12 Report Template** — โครงสร้างเนื้อหาแยกจาก branding (dummy ไว้ก่อน รอ finalize ชื่อจริง)
- **Component นำกลับใช้ได้** — Header, Content, Footer, Helpers แยกชัด
- **รองรับลายเซ็น** — template ที่ต้อง sign ก่อนออก PDF

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| PDF Engine | QuestPDF |
| API | ASP.NET Core (.NET 8) |
| Client | Angular (`@hemo/pdf-client`) |
| Font | Sarabun (รองรับภาษาไทย) |

---

## โครงสร้างที่วางแผนไว้

```
Hemo-PDF/
├── src/
│   ├── Hemo.Pdf.Api/           # Standalone API
│   ├── Hemo.Pdf.Core/          # Interfaces & contracts
│   ├── Hemo.Pdf.Sections/      # Header, Footer, Content blocks
│   ├── Hemo.Pdf.Layouts/       # Report templates (12 แบบ)
│   ├── Hemo.Pdf.Branding/      # Tenant branding config
│   ├── Hemo.Pdf.Rendering/     # QuestPDF implementation
│   └── client/                 # Angular library
├── assets/fonts/               # Sarabun
├── assets/branding/            # Branding JSON ต่อ tenant
└── docs/
```

---

## เอกสารประกอบ

| ไฟล์ | เนื้อหา |
|------|---------|
| [HEMO-PDF-SUB-MODULE.md](./HEMO-PDF-SUB-MODULE.md) | แผนออกแบบโมดูล, API contract, phase implement |
| [ฺIMPLEMENT-PLANNING.md](./ฺIMPLEMENT-PLANNING.md) | สรุประบบ PDF จากโปรเจกต์ NSS (อ้างอิง) |

---

## สถานะปัจจุบัน

อยู่ในขั้น **วางแบบและออกแบบ** — ยังไม่ได้ scaffold code

ลำดับ implement ที่วางไว้:

1. Standalone `Hemo.Pdf.Api` + endpoint `POST /api/pdf/generate`
2. Section system + branding ต่อ tenant (mock ก่อน)
3. Report template แรก + Angular client
4. เพิ่ม template จนครบ 12 แบบ

---

## Repo ที่เกี่ยวข้อง

| Repo | บทบาท |
|------|--------|
| [Hemo-backend](../Hemo-backend) | HemodialysisPro API — ส่ง DTO มาให้ PDF service |
| [Hemo-frontend](../Hemo-frontend) | Angular app — ใช้ `@hemo/pdf-client` |
| [NSS](../NSS) | แหล่งอ้างอิงแพทเทิร์น PDF (QuestPDF + Factory/Strategy) |
