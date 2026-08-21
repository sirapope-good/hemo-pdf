# Branding Guideline (DbBrandingStore — อนาคต)

ปัจจุบันใช้ `JsonFileBrandingStore` อ่านจาก `assets/branding/{tenantCode}.json`

## เมื่อต้องการเชื่อม Hemopro DB

1. สร้าง `DbBrandingStore` ใน Hemopro (ไม่ใช่ใน Hemo-PDF) หรือ microservice แยก
2. Implement `IBrandingStore` ที่ query ตาราง `CustomerBranding`
3. สลับ DI ใน `AddHemoPdf()` — ไม่ต้องแก้ `ConfigurableHeaderSection`

## โครงสร้าง JSON (อ้างอิง)

ดู `assets/branding/tenant-demo-a.json` / `local.json`

```json
"style": {
  "primaryFontFamily": "Sarabun",
  "accentColor": "#1a5276",
  "sectionHeaderBackground": "#C0C0FF"
}
```

- `sectionHeaderBackground` — สี fill ของ column / section header ทุก widget report ของ tenant (ว่าง = ใช้ค่า default ของแต่ละ layout เช่น ThaiUr `#C0C0FF`)
- แก้ผ่าน HemoAdmin (Tenant → Tenant config → Report section header color) หรือ `PUT /api/branding/style` ของ Hemo-PDF

## Level 3 Header Override

ลงทะเบียน class ใน `Hemo.Pdf.Sections/Headers/Customers/` และเพิ่มใน `SectionResolver` registry:

```csharp
(tenantCode: "hospital-x", templateId: "*", typeof(HospitalXHeaderSection))
```
