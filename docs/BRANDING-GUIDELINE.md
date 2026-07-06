# Branding Guideline (DbBrandingStore — อนาคต)

ปัจจุบันใช้ `JsonFileBrandingStore` อ่านจาก `assets/branding/{tenantCode}.json`

## เมื่อต้องการเชื่อม Hemopro DB

1. สร้าง `DbBrandingStore` ใน Hemopro (ไม่ใช่ใน Hemo-PDF) หรือ microservice แยก
2. Implement `IBrandingStore` ที่ query ตาราง `CustomerBranding`
3. สลับ DI ใน `AddHemoPdf()` — ไม่ต้องแก้ `ConfigurableHeaderSection`

## โครงสร้าง JSON (อ้างอิง)

ดู `assets/branding/tenant-demo-a.json`

## Level 3 Header Override

ลงทะเบียน class ใน `Hemo.Pdf.Sections/Headers/Customers/` และเพิ่มใน `SectionResolver` registry:

```csharp
(tenantCode: "hospital-x", templateId: "*", typeof(HospitalXHeaderSection))
```
