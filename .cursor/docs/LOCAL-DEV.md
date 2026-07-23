# คู่มือใช้งาน Hemo-PDF แบบ Local

ขั้นตอนรัน stack ครบเพื่อดู **Hemosheet preview / print / download** ผ่าน Hemo-PDF  
(แทน Telerik เมื่อเปิด flag)

> เอกสารสถาปัตยกรรม: [PDF-REPORT-SYSTEM.md](./PDF-REPORT-SYSTEM.md)

---

## สิ่งที่ต้องมี

| Service | Port | Repo |
|---------|------|------|
| Postgres + Redis (+ MQTT ถ้า Web.Api ต้องการ) | 5432 / 6379 / 8883 | infra ตาม `Hemo-backend` AGENTS |
| HemoAdmin.Api | 8600 | Hemo-backend |
| Web.Api | 8200 | Hemo-backend |
| Hemo.Pdf.Api | 5090 | Hemo-PDF |
| Frontend (`nx serve`) | 4200 | Hemo-frontend |

Login ใช้ user ที่ seed ใน tenant `local` (เช่น `rootadmin` หลัง reset รหัสตาม AGENTS ของ backend)

---

## 1) รัน Hemo-PDF

```bash
cd d:/GoodRepo/Hemo-PDF/src/Hemo.Pdf.Api
dotnet run --urls "http://localhost:5090"
```

ตรวจสุขภาพ:

```bash
curl http://localhost:5090/health
```

Swagger (Development): http://localhost:5090/swagger

### Auth ตอน Development (ค่า default)

`appsettings.Development.json` ตั้ง `HemoPdf:UseMockServices=true` → ใช้ **MockAuth**

- **ต้อง**ส่ง `Authorization: Bearer …` (ค่าอะไรก็ได้ เช่น `dev`) — ไม่ส่ง Bearer = 401
- tenant อ่านจาก header `X-Tenant-Code` (เช่น `local` หรือ `tenant-demo-a` สำหรับ branding mock)
- `UseServerFetch=true` (Dev default) → Hemo-PDF ดึง DTO จาก Web.Api `:8200` เอง (forward JWT + `X-Tenant-Code`)

ไม่ต้องตั้ง JWT key ในโหมด mock แต่ **Web.Api ต้องรัน** เมื่อเปิด server fetch

### S2S tenant resolution (spike / verified)

Web.Api `TenantRequestMetadataResolver` ลำดับ: AdminOps path → JWT `tenant_code` → `X-Tenant-Code` → Origin/host  
ดังนั้น Hemo-PDF → Web.Api **ไม่ต้อง**ส่ง `Origin: localhost:4200` ถ้า forward `Authorization` (มี claim) และ/หรือ `X-Tenant-Code`

Kill-switch S2S: `HemoPdf:UseServerFetch=false` (กลับไปเชื่อ `data` จาก client — ไม่แนะนำนอกเทส)

### Auth แบบจริง (JWT ร่วม Web.Api) — optional

เมื่อต้องการทดสอบ token จริงจาก frontend:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export HemoPdf__UseMockServices=false
export HemoPdf__Jwt__Issuer=http://localhost/
export HemoPdf__Jwt__Key=<ค่าเดียวกับ Authentication__Key ของ Web.Api>
```

ข้อควรจำ:

- `Audience` ว่าง = ใช้ค่า Issuer (ห้ามตั้ง `hemo-pdf`)
- JWT ต้องมี claim `tenant_code`
- `X-Tenant-Code` และ body `tenantCode` ต้องตรงกับ claim (normalize: `localhost` → `local`)
- เปิด `UseMockServices=true` นอก Development → **process ไม่ขึ้น**

---

## 2) เปิด feature flag ฝั่ง frontend config

หลัง P1 ค่าใน `offline-bootstrap.json` เป็น **`useHemoPdfPreview: false`** (ปลอดภัยเมื่อ HemoAdmin ล่ม)

สำหรับ local ที่ใช้ config จาก storage จริง ให้แก้:

`Hemo-backend/HemoDialysisPro/.local-storage/customer-data/local/config.json`

```json
{
  "pdfApiUrl": "http://localhost:5090",
  "useHemoPdfPreview": true
}
```

หรือเปิดผ่าน **HemoAdmin → Tenant detail → Tenant config** (checkbox Use Hemo-PDF preview + PDF API URL) แล้ว publish

> ต้องมี **HemoAdmin (:8600)** ทำงาน — `nx serve` อ่าน config ผ่าน proxy `/dev/config.json`  
> ถ้า HemoAdmin ล่ม จะตกไป offline-bootstrap ที่ flag = false → กลับไป Telerik

หลังแก้ config ให้ **hard refresh** หน้าเว็บ (หรือ clear cache แล้ว login ใหม่)

---

## 3) รัน backend + frontend

ลำดับที่แนะนำ:

1. Infra (Postgres, Redis-stack, MQTT)
2. HemoAdmin.Api `:8600`
3. Web.Api `:8200` (+ JobServer ถ้าต้องการงานพื้นหลัง)
4. Hemo.Pdf.Api `:5090`
5. Frontend:

```bash
cd d:/GoodRepo/Hemo-frontend
npm start
# หรือ: npx nx serve --host=0.0.0.0 --disable-host-check
```

เปิด http://localhost:4200 แล้ว login

---

## 4) ลองใช้งานใน UI

### Reports page

1. ไปเมนู Report / Hemosheet ตามที่โปรเจกต์มี
2. เลือก hemosheet ของผู้ป่วย
3. ถ้า flag เปิดและเป็น hemosheet ใน catalog → เห็น **Hemo-PDF viewer** (ไม่ใช่ Telerik)

| Layout profile | Preview บนจอ | Print / Download |
|----------------|--------------|------------------|
| Default / Rama / อื่น ๆ | DOM จาก `POST /api/report/preview` | PDF จาก `POST /api/pdf/generate` |
| **ThaiUr** | PDF-as-preview (`generate` + canvas) | PDF เดียวกัน |

Toolbar: Zoom, Reload, Print, Download

### Embedded hemosheet (Doctor view)

เปิด overview ผู้ป่วยที่มี hemosheet — viewer ฝังใช้ path เดียวกันกับ flag ด้านบน

---

## 5) Smoke แบบเร็วด้วย curl (ไม่ผ่าน UI)

Development + MockAuth:

```bash
curl -X POST http://localhost:5090/api/report/preview \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer dev" \
  -H "X-Tenant-Code: tenant-demo-a" \
  -d '{
    "reportTemplateId": "template-02-lab-result",
    "tenantCode": "tenant-demo-a",
    "entityId": "test-1",
    "data": { "patientName": "Test Patient", "value": 42 }
  }'
```

ข้อบังคับ request (P1):

- `entityId` ต้องมีค่า
- ถ้า `data.id` มีค่า ต้องตรงกับ `entityId`
- `tenantCode` ใน body ต้องตรงกับ tenant ที่ resolve ได้จาก header/claim

---

## 6) Branding ต่อ tenant

ไฟล์: `Hemo-PDF/assets/branding/{tenantCode}.json`

- ตัวอย่าง local: `assets/branding/local.json`
- mock demo: `tenant-demo-a.json`, `tenant-demo-b.json`
- **ไม่มีไฟล์** → HTTP 500 (ยังไม่มี default profile)

ถ้า FE ส่ง `X-Tenant-Code: local` ต้องมี `local.json`

---

## 7) ทดสอบอัตโนมัติ

```bash
# Hemo-PDF
cd d:/GoodRepo/Hemo-PDF
dotnet test Hemo.Pdf.sln

# Frontend (Jest ที่เกี่ยวกับ Hemo-PDF)
cd d:/GoodRepo/Hemo-frontend
npx nx test app --configuration=ci --testPathPattern="hemo-pdf-(report-catalog|preview.controller)" --skip-nx-cache
```

---

## ปัญหาที่พบบ่อย

| อาการ | สาเหตุที่พบบ่อย | แก้ |
|--------|------------------|-----|
| ยังเห็น Telerik | `useHemoPdfPreview` ไม่ใช่ `true` ใน tenant config / offline fallback | เปิด flag ใน `.local-storage/.../config.json` หรือ HemoAdmin แล้ว refresh |
| Preview 401 | ปิด mock แต่ JWT key/issuer ไม่ตรง Web.Api | ตั้ง `HemoPdf__Jwt__*` ให้ตรง หรือเปิด mock กลับใน Dev |
| Preview 403 | `X-Tenant-Code` / body `tenantCode` ไม่ตรง claim | ใช้ tenant จาก token; Dev+Mock ใช้ header ให้ตรง body |
| Preview 400 | ไม่มี `entityId` หรือไม่ตรง `data.id` | ส่ง hemoId = id ใน DTO |
| CORS | origin ไม่ใช่ 4200 | เพิ่มใน `HemoPdf:CorsOrigins` |
| Branding 500 | ไม่มี `assets/branding/{tenant}.json` | คัดลอกจาก `local.json` แล้วเปลี่ยนชื่อ |
| Hemo-PDF ไม่ขึ้น | `UseMockServices=true` นอก Development | ปิด mock หรือรัน Development |
| Config ไม่เปลี่ยน | อ่าน cache / HemoAdmin ไม่รัน | เปิด HemoAdmin + hard refresh |

**Kill-switch:** ตั้ง `useHemoPdfPreview: false` ใน tenant config → กลับ Telerik โดยไม่ต้องปิด Hemo-PDF

---

## สรุปลำดับหนึ่งหน้า

```text
1. Infra + HemoAdmin :8600 + Web.Api :8200
2. Hemo.Pdf.Api :5090  (Dev = mock auth)
3. เปิด useHemoPdfPreview + pdfApiUrl ใน local config.json
4. npm start frontend :4200 → login
5. เปิด Hemosheet report → DOM (หรือ PDF canvas ถ้า ThaiUr)
6. Print/Download → PDF จาก Hemo-PDF
```
