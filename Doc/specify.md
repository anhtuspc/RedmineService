# RedmineMonitorService - Service Specification

## 1. Tổng quan

**RedmineMonitorService** là một Windows Service viết bằng Visual Basic .NET (.NET Framework 4.8), được thiết kế để tự động hóa quy trình tạo và cập nhật ticket trên hệ thống Redmine nội bộ của MISUMI.

- **Tên service:** `WAR_Redmine`
- **Ngôn ngữ:** Visual Basic .NET
- **Framework:** .NET Framework 4.8
- **Loại:** Windows Service (có thể chạy console mode để debug)
- **Server Redmine:** `https://srg-redmine-prd.internal.misumi.jp` (yêu cầu VPN)

---

## 2. Chức năng chính

### 2.1 Giám sát folder XML (XmlFolderMonitor)
- Dùng `FileSystemWatcher` để theo dõi folder được cấu hình trong `App.config`
- Tự động phát hiện file `.xml` mới và xử lý ngay lập tức
- Thread-safe: tránh xử lý trùng lặp bằng `HashSet`
- Chờ file được ghi hoàn tất (500ms delay + kiểm tra file access) trước khi xử lý

### 2.2 Tạo ticket mới từ XML
- Parse file XML để lấy thông tin ticket
- Gọi Redmine REST API để tạo ticket với đầy đủ thông tin:
  - Tiêu đề, mô tả, độ ưu tiên, người phụ trách
  - Hơn 10 custom field: ngày nhận, người yêu cầu, tiêu đề email, số lượng, v.v.
  - Chọn server (Test/Production): **ưu tiên dùng `<TestServer>`/`<ProductionServer>` từ XML**, fallback về Subject prefix nếu XML không có
- Cập nhật thêm thông tin vào ticket sau khi tạo (update folder path, ghi chú)

### 2.3 Cập nhật ticket từ XML
- Tìm ticket theo `TicketNo` và cập nhật: trạng thái, người phụ trách, độ ưu tiên
- Giữ nguyên các custom field hiện có
- Khi ticket đóng (Close): tự động ghi time entry với tỷ lệ 75% cho Test, 25% cho Production

### 2.4 Giám sát và tạo báo cáo Redmine
- Chạy định kỳ theo interval cấu hình (mặc định: 1 phút)
- Kết nối Redmine, lấy danh sách ticket từ saved query
- Tạo báo cáo HTML (`Output.html`) nhóm theo PIC (Person In Charge)

### 2.5 Backup và quản lý file
- Sau khi xử lý, backup file XML vào folder: `BackupFolder/YYYYMM/YYYYMMDD/TicketNo/`
- Sao chép file dữ liệu liên quan lên network share
- Tạo bản sao dự phòng ở local master folder
- Tên folder: `YYYYMM/YYYYMMDD/NoXXXXX[Subject][Qty]Qty`
- File lỗi được đổi tên với suffix: `.error`, `.invalid`, `.skipped`

---

## 3. Cấu trúc project

```
RedmineService/
├── RedmineMonitorService/
│   ├── RedmineMonitorService.vb     # Main service class
│   ├── RedmineClient.vb             # HTTP client gọi Redmine API
│   ├── RedmineTicketCreator.vb      # Tạo/cập nhật ticket qua API
│   ├── XmlFolderMonitor.vb          # Giám sát folder, xử lý XML
│   ├── TicketXmlParser.vb           # Parse file XML thành object
│   ├── TicketXmlData.vb             # Data model của ticket XML
│   ├── TicketInfo.vb                # Data model ticket từ Redmine
│   ├── HtmlReportGenerator.vb       # Tạo báo cáo HTML
│   ├── EncryptionHelper.vb          # AES-256 encrypt/decrypt
│   ├── EncryptCredentials.vb        # Tool mã hóa credentials
│   ├── Logger.vb                    # Ghi log thread-safe
│   ├── ProjectInstaller.vb          # Windows service installer
│   └── App.config                   # Cấu hình service
├── RedmineMonitorService.Tests/     # Unit tests
├── Doc/                             # Tài liệu
├── build.bat                        # Build script (Debug)
├── release.bat                      # Build script (Release)
└── RedmineMonitorService.sln        # Visual Studio solution
```

---

## 4. Cấu hình (App.config)

| Key | Mô tả |
|-----|-------|
| `RedmineUrl` | URL query Redmine lấy danh sách ticket |
| `ApiKey` | API key xác thực với Redmine |
| `Username` / `Password` | Thông tin đăng nhập (fallback, hỗ trợ mã hóa AES) |
| `TimerIntervalMinutes` | Chu kỳ polling Redmine (phút) |
| `MonitorFolder` | Folder theo dõi file XML đầu vào |
| `BackupFolder` | Folder lưu backup file XML đã xử lý và báo cáo HTML |
| `ProjectId` | ID project Redmine (29) |

---

## 5. Cấu trúc file XML đầu vào

File XML đặt vào `MonitorFolder` với cấu trúc:

```xml
<Root>
  <UpdateStatus>New</UpdateStatus>           <!-- New hoặc Update -->
  <TicketNo>12345</TicketNo>                 <!-- Bắt buộc nếu UpdateStatus=Update -->
  <Subject>Tiêu đề ticket</Subject>          <!-- Bắt buộc nếu UpdateStatus=New -->
  <Status>In Progress</Status>
  <Assign>SRG</Assign>                       <!-- SRG hoặc GSupport -->
  <Priority>medium</Priority>                <!-- low/medium/high/urgent/critical -->
  <ReceiptionDate>2026-03-27</ReceiptionDate>
  <Requester>Tên người yêu cầu</Requester>
  <EmailTitle>Tiêu đề email</EmailTitle>
  <EmailTime>10:00</EmailTime>
  <RegistDate>2026-03-27</RegistDate>
  <RegistPerson>Tên người đăng ký</RegistPerson>
  <Qty>1</Qty>
  <Detail>Mô tả chi tiết</Detail>
  <ItemType>Loại item</ItemType>
  <DueDate>2026-04-01</DueDate>
  <FolderNo>001</FolderNo>
  <EstimateTime>2</EstimateTime>
  <UpdateFolder>đường dẫn folder</UpdateFolder>
  <TestServer>272</TestServer>               <!-- ID server Test, ví dụ: 272 → CL272. Nếu bỏ trống, tự suy từ Subject prefix -->
  <ProductionServer>322</ProductionServer>   <!-- ID server Production, ví dụ: 322 → CL322 -->
  <TeamsUrl>https://...</TeamsUrl>
  <Description>Ghi chú thêm</Description>
</Root>
```

**Logic chọn Test/Production Server (theo thứ tự ưu tiên):**

| Điều kiện | Test | Production |
|-----------|------|------------|
| `<TestServer>` có giá trị trong XML | Dùng giá trị XML (tự thêm prefix "CL" nếu thiếu) | Dùng giá trị XML |
| Subject bắt đầu bằng `SH2` | CL272 | CL322 |
| Subject bắt đầu bằng `JP` | CL266 | CL325 |
| Mặc định (NT hoặc khác) | CL271 | CL323 |

---

## 6. Mapping dữ liệu

### Trạng thái ticket (Status → ID Redmine)
| Status | ID |
|--------|-----|
| New | 1 |
| In Progress | 2 |
| Resolved | 3 |
| Feedback | 4 |
| Closed / Close | 5 |
| Rejected | 6 |

### Người phụ trách (Assign → User ID Redmine)
| Assign | User ID |
|--------|---------|
| SRG | 177 |
| GSupport | 102 |

### Độ ưu tiên (Priority → ID Redmine)
| Priority | ID |
|----------|-----|
| low | 1 |
| medium | 2 |
| high | 3 |
| urgent | 4 |
| critical | 5 |

---

## 7. Xác thực và bảo mật

- **Ưu tiên:** API Key (`X-Redmine-API-Key` header)
- **Fallback:** Username/Password (hỗ trợ mã hóa AES-256 trong App.config)
- **Mã hóa:** AES-256 CBC, key "gsupport" (SHA256 hash), IV = 16 bytes zero
- **SSL:** Certificate validation bị bypass cho server nội bộ

---

## 8. Logging

- File log: `log.txt` trong thư mục chứa exe
- Format: `[YYYY-MM-DD HH:MM:SS] Message`
- Thread-safe với `SyncLock`
- Fallback sang Windows EventLog nếu không ghi được file

---

## 9. Build & Deploy

### Build
```bat
# Debug build
build.bat

# Release build
release.bat
```

Output: `RedmineMonitorService\bin\Debug\RedmineMonitorService.exe`

### Cài đặt Windows Service (lần đầu)
```bat
# Chạy với quyền Administrator
InstallUtil.exe "C:\path\to\RedmineMonitorService.exe"
sc start WAR_Redmine
```

### Uninstall và cài lại (khi cần thay exe mới)
```bat
# 1. Dừng service
sc stop WAR_Redmine

# 2. Uninstall
InstallUtil.exe /u "C:\path\to\RedmineMonitorService.exe"

# 3. Copy file exe mới vào thư mục deploy

# 4. Cài lại
InstallUtil.exe "C:\path\to\RedmineMonitorService.exe"

# 5. Khởi động lại
sc start WAR_Redmine
```

### Cập nhật exe mà KHÔNG cần uninstall/install lại
> Đây là cách nhanh nhất khi chỉ thay đổi code, không thay đổi service name hay config.

```bat
# 1. Dừng service
sc stop WAR_Redmine

# 2. Copy file exe mới đè lên file cũ
copy /Y "RedmineMonitorService.exe" "C:\path\to\deploy\RedmineMonitorService.exe"

# 3. Khởi động lại service
sc start WAR_Redmine
```

> **Lưu ý:** Phải dừng service trước khi copy vì Windows lock file exe đang chạy.

### Kiểm tra trạng thái service
```bat
sc query WAR_Redmine
```

### Chạy console mode (debug)
```bat
RedmineMonitorService.exe /console
```

### Chạy test mode (1 chu kỳ rồi thoát)
```bat
RedmineMonitorService.exe /test
```

---

## 10. Output

| File | Mô tả |
|------|-------|
| `log.txt` | Log hoạt động của service |
| `Output.html` | Báo cáo HTML danh sách ticket theo PIC |
| `BackupFolder/YYYYMM/YYYYMMDD/TicketNo/` | Backup file XML đã xử lý |
