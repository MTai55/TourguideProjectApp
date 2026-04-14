# Product Requirements Document (PRD)
## TourGuideAPP — Ứng dụng Hướng dẫn Du lịch Thông minh TP.HCM

**Phiên bản:** 2.2  
**Ngày cập nhật:** Tháng 4, 2026  
**Trạng thái:** Đang phát triển  

---

## 1. Tổng quan

**TourGuideAPP** là ứng dụng hướng dẫn du lịch thông minh dành cho TP. Hồ Chí Minh, xây dựng trên nền tảng .NET MAUI. Ứng dụng kết hợp GPS, Geofencing và Text-to-Speech để tự động phát thuyết minh khi người dùng tiến đến gần địa điểm du lịch, mang lại trải nghiệm như có hướng dẫn viên cá nhân.

### Vấn đề cần giải quyết
- Khách du lịch không có hướng dẫn viên riêng thường bỏ lỡ thông tin thú vị về địa điểm
- Thuê hướng dẫn viên tốn kém và phụ thuộc lịch trình cố định
- Các app hiện có (Google Maps, TripAdvisor) chỉ cung cấp thông tin tĩnh, không chủ động

### Giải pháp
Tự động phát thuyết minh đúng lúc — khi người dùng đi đến gần địa điểm — không cần bấm nút, không cần đọc, chỉ cần đi và lắng nghe.

---

## 2. Đối tượng người dùng

### 2.1 Khách du lịch nội địa & quốc tế
| | Chi tiết |
|---|---|
| **Đặc điểm** | Đến TP.HCM 1–3 ngày, không quen đường, muốn trải nghiệm nhiều nhất |
| **Nhu cầu** | Biết mình đang đứng gần địa điểm gì, nghe giới thiệu ngắn gọn, tìm đường nhanh |
| **Pain point** | Phải tự tra cứu từng nơi, dễ bỏ lỡ điểm thú vị vì không biết |
| **Kỳ vọng** | App hoạt động tự động, không cần thao tác nhiều khi đang đi |

### 2.2 Sinh viên & người trẻ địa phương
| | Chi tiết |
|---|---|
| **Đặc điểm** | Sống tại TP.HCM nhưng chưa khám phá hết, thích trải nghiệm mới |
| **Nhu cầu** | Khám phá địa điểm theo chủ đề (cà phê, ẩm thực, lịch sử) |
| **Pain point** | Không biết bắt đầu từ đâu, thiếu thông tin chiều sâu về từng nơi |
| **Kỳ vọng** | Gợi ý tour theo danh mục, thông tin phong phú, dễ tìm đường |

### 2.3 Admin (Người quản trị hệ thống)
| | Chi tiết |
|---|---|
| **Đặc điểm** | Nhân viên vận hành, quản lý nội dung địa điểm và gói dịch vụ |
| **Nhu cầu** | Kích hoạt gói cho khách sau khi nhận thanh toán, cập nhật thông tin địa điểm |
| **Công cụ** | Supabase Dashboard + SQL function `activate_session()` |

---

## 3. Phạm vi tính năng (Use Case)

### Actor & Use Case

| Actor | Use Case |
|---|---|
| **Khách du lịch** | Chọn & thanh toán gói sử dụng |
| | Xem bản đồ & marker địa điểm |
| | Tìm kiếm & lọc địa điểm |
| | Xem chi tiết địa điểm |
| | Chỉ đường đến địa điểm |
| | Nghe thuyết minh tự động khi đến gần POI |
| | Chọn ngôn ngữ thuyết minh |
| | Xem & theo tour có sẵn |
| **Owner** | Đăng ký / Đăng nhập |
| | Quản lý địa điểm (thêm, sửa, xoá) |
| | Cập nhật trạng thái mở/đóng cửa |
| | Cập nhật nội dung TTS script |
| | Quản lý khuyến mãi |
| | Xem & trả lời đánh giá |
| | Xem thống kê địa điểm |
| **Admin** | Kích hoạt gói sử dụng cho khách |
| | Duyệt / từ chối địa điểm |
| | Ẩn / hiện đánh giá vi phạm |
| | Khoá / mở tài khoản người dùng |
| | Xem thống kê toàn hệ thống |

### Use Case Diagram

```mermaid
graph LR
    Tourist(["👤 Khách du lịch"])
    Owner(["👤 Owner"])
    Admin(["👤 Admin"])
    System(["⚙️ Hệ thống"])

    subgraph APP["  TourGuideAPP (Mobile)  "]
        UC1(["Chọn gói truy cập"])
        UC2(["Thanh toán VietQR"])
        UC3(["Xem bản đồ & POI"])
        UC4(["Tìm kiếm & lọc địa điểm"])
        UC5(["Xem chi tiết địa điểm"])
        UC6(["Chỉ đường đến địa điểm"])
        UC7(["Nghe thuyết minh tự động"])
        UC8(["Chọn ngôn ngữ thuyết minh"])
        UC9(["Xem & theo tour"])
        UC10(["Gọi điện / mở website"])
        UC13(["Polling xác nhận thanh toán"])
        UC14(["Tự động khóa khi hết hạn"])
        UC15(["Phát thuyết minh theo GPS"])
    end

    subgraph WEB["  TourGuideAPI (Web)  "]
        UC11(["Kích hoạt phiên truy cập"])
        UC12(["Quản lý địa điểm & TTS"])
        UC16(["Duyệt / từ chối địa điểm"])
        UC17(["Quản lý tài khoản người dùng"])
        UC18(["Xem thống kê hệ thống"])
    end

    Tourist --> UC1
    Tourist --> UC2
    Tourist --> UC3
    Tourist --> UC4
    Tourist --> UC5
    Tourist --> UC6
    Tourist --> UC7
    Tourist --> UC8
    Tourist --> UC9
    Tourist --> UC10

    Owner --> UC12

    Admin --> UC11
    Admin --> UC16
    Admin --> UC17
    Admin --> UC18

    System --> UC13
    System --> UC14
    System --> UC15

    UC1 -. include .-> UC2
    UC6 -. include .-> UC3
    UC7 -. include .-> UC15
    UC2 -. include .-> UC13
    UC13 -. include .-> UC11
```

---

## 4. Kiến trúc hệ thống

### Mobile Application
- **Framework:** .NET MAUI 10.0
- **Target chính:** Android (API 21+), iOS (15+)
- **Pattern:** Layered Architecture + Code-behind (không dùng MVVM)
- **Dependency Injection:** Microsoft.Extensions.DependencyInjection — Services là Singleton, Pages là Transient

### Backend & Database
- **Database:** PostgreSQL qua Supabase (không có backend riêng — app gọi trực tiếp REST API)
- **Client:** postgrest-csharp — map C# model sang Supabase table qua `[Column]` attribute
- **Local storage:** MAUI Preferences — lưu session token, DeviceId, settings

### Thư viện bên thứ ba
| Thư viện | Phiên bản | Mục đích |
|---|---|---|
| Mapsui | 5.x | Bản đồ tương tác (CartoDB Voyager tiles) |
| BruTile | - | Tile source provider cho Mapsui |
| OSRM API | - | Tính toán & vẽ tuyến đường |
| MAUI TextToSpeech | built-in | Text-to-Speech đa ngôn ngữ |
| SkiaSharp | - | Render marker POI tùy chỉnh |
| postgrest-csharp | - | ORM giao tiếp Supabase |

---

## 5. Tính năng chi tiết

### 5.1 Kiểm soát truy cập theo gói thời gian
**Mục tiêu:** Monetize app — người dùng phải thanh toán trước khi sử dụng

**Luồng hoạt động:**
```
Mở app → Kiểm tra Preferences (còn session hợp lệ không?)
  ├── Còn hạn → Vào app bình thường, khởi động timer
  └── Hết hạn / chưa có → Hiện SubscriptionPage

SubscriptionPage → Chọn gói → PaymentQRPage
  → Hiện QR VietQR (chuyển khoản ngân hàng)
  → App polling Supabase mỗi 5 giây
  → Admin kích hoạt → app nhận ExpiresAt → lưu Preferences → vào app

Timer background (mỗi 60 giây):
  → Kiểm tra DateTime.UtcNow < ExpiresAt
  → Hết hạn → Alert → về SubscriptionPage
```

**Gói sử dụng:**
| Gói | Thời lượng | Giá |
|---|---|---|
| Cơ bản | 1 tiếng | 10.000đ |
| Phổ biến | 2 tiếng | 18.000đ |
| Ngày | 1 ngày (24h) | 50.000đ |
| Dài ngày | 3 ngày (72h) | 120.000đ |

**Bảng DB:** `AccessSessions` | **Services:** `AccessSessionService` | **Views:** `SubscriptionPage`, `PaymentQRPage`

---

### 5.2 GPS & Định vị thời gian thực
**Mục tiêu:** Theo dõi vị trí người dùng liên tục, chính xác, kể cả khi tắt màn hình

**Luồng hoạt động:**
- **Android:** Foreground Service chạy nền — nhận vị trí từ platform → bắn event `LocationChanged`
- **iOS:** Polling `Geolocation.GetLocationAsync()` mỗi 3 giây
- Reverse geocoding hiển thị tên đường hiện tại (cache 15 giây, không gọi lại nếu dưới 30m)

**Services:** `LocationService`, `LocationForegroundService` (Android)

---

### 5.3 Geofence & Thuyết minh tự động (TTS)
**Mục tiêu:** Tự động phát thuyết minh khi người dùng tiến đến gần địa điểm — tính năng cốt lõi

**Thuật toán:**
```
GPS update → GeofenceEngine.FindNearestPOI()
  → Lọc POI có TtsScript, trong bán kính, chưa hết cooldown
  → Sắp xếp: Priority cao trước, cùng Priority → gần hơn trước
  → Debounce 2 giây (phải ở trong vùng liên tục)
  → Trả về POI ưu tiên nhất

MapPage nhận POI → kiểm tra _lastSpokenPlaceId (tránh đọc lại)
  → NarrationService.SpeakAsync(TtsScript, TtsLocale)
  → Cập nhật LastPlayedAt → bắt đầu cooldown
```

**Cấu hình per-place (trong DB):**
| Tham số | Mặc định | Mô tả |
|---|---|---|
| radius | 50m | Bán kính phát hiện |
| cooldown_minutes | 10 phút | Thời gian chờ trước khi đọc lại |
| priority | 1 | Độ ưu tiên khi nhiều POI trong vùng |
| tts_locale | vi-VN | Ngôn ngữ đọc (vi-VN, en-US, zh-CN...) |

**Services:** `GeofenceEngine`, `NarrationService`

---

### 5.4 Bản đồ tương tác
**Mục tiêu:** Hiển thị toàn bộ địa điểm trên bản đồ, hỗ trợ chỉ đường

**Tính năng:**
- Render POI markers 2 lớp (SkiaSharp): glow đỏ mờ + chấm đặc có viền trắng
- Marker người dùng: glow xanh mờ + chấm xanh có viền trắng
- Tap marker → bottom card nổi lên (Google Maps style): tên, rating, khoảng cách, 4 nút hành động
- Nút hành động: **Chỉ đường** / **Thuyết minh** / **Gọi điện** / **Chi tiết**
- Chỉ đường: gọi OSRM API → vẽ polyline lên bản đồ → hiện panel hủy route
- Auto-follow GPS (flag `_followUserLocation`) — tự tắt khi user kéo map

**Views:** `MapPage` | **External API:** OSRM (`router.project-osrm.org`)

---

### 5.5 Danh sách địa điểm nổi bật
**Mục tiêu:** Duyệt và tìm kiếm địa điểm theo danh mục, không cần mở bản đồ

**Tính năng:**
- Hiển thị card: ảnh đại diện, tên, rating sao, khoảng giá, giờ mở cửa
- Tìm kiếm realtime theo: tên, địa chỉ, mô tả, đặc sản
- Lọc nhanh theo danh mục: Tất cả / Cà phê / Cơm / Nhậu / Trà sữa
- Dữ liệu được cache in-memory sau lần load đầu (không gọi Supabase lại khi chuyển tab)

**Views:** `MainPage` | **Services:** `PlaceService`

---

### 5.6 Chi tiết địa điểm
**Mục tiêu:** Xem đầy đủ thông tin một địa điểm, thực hiện hành động nhanh

**Tính năng:**
- Gallery ảnh cuộn ngang (từ bảng `PlaceImages`)
- Hiển thị: địa chỉ, giờ mở/đóng, trạng thái mở-đóng thời gian thực, giá, số điện thoại, website
- Nút **Chỉ đường**: set `MapPage.PendingRoute` → chuyển sang tab Bản đồ → tự động tính route
- Nút **Gọi điện**: mở dialer với số điện thoại địa điểm
- Nút **Thuyết minh**: phát TTS ngay lập tức

**Views:** `PlaceDetailPage`

---

### 5.7 Tour có sẵn
**Mục tiêu:** Gợi ý lộ trình tour theo chủ đề, người dùng theo từng điểm dừng

**Tính năng:**
- Danh sách tour: tên, mô tả, số điểm dừng
- Chi tiết tour: danh sách điểm dừng theo thứ tự với tên và mô tả ngắn
- Tap điểm dừng → xem PlaceDetailPage
- Khi di chuyển thực tế: geofence tự đọc thuyết minh tại từng điểm (không cần thao tác thêm)

**Views:** `ToursPage`, `TourDetailPage` | **Services:** `POIService`

---

## 6. Cấu trúc dữ liệu

### Bảng `Places`
| Cột | Kiểu | Ghi chú |
|---|---|---|
| PlaceId | int | Khóa chính |
| Name | text | Tên địa điểm |
| Description | text | Mô tả |
| Address | text | Địa chỉ |
| Latitude / Longitude | float | Tọa độ GPS |
| Phone | text | Số điện thoại |
| OpenTime / CloseTime | text | Định dạng HH:mm |
| PriceMin / PriceMax | decimal | VND |
| AverageRating | float | 0.0 – 5.0 |
| TotalReviews | int | Số lượt đánh giá |
| Specialty | text | Tags, phân cách dấu phẩy |
| IsActive / IsApproved | bool | Chỉ hiện khi cả hai = true |
| tts_script | text | Nội dung thuyết minh TTS |
| tts_locale | text | Ngôn ngữ TTS (mặc định vi-VN) |
| radius | float | Bán kính geofence (m) |
| cooldown_minutes | int | Cooldown giữa 2 lần đọc |
| priority | int | Độ ưu tiên geofence |

### Bảng `PlaceImages`
| Cột | Kiểu | Ghi chú |
|---|---|---|
| ImageId | int | Khóa chính |
| PlaceId | int | FK → Places |
| ImageUrl | text | URL ảnh |
| IsMain | bool | Ảnh đại diện chính |
| SortOrder | int | Thứ tự hiển thị |

### Bảng `AccessSessions`
| Cột | Kiểu | Ghi chú |
|---|---|---|
| SessionId | UUID | Khóa chính (auto gen) |
| DeviceId | text | Mã thiết bị 10 ký tự |
| PackageId | text | 1h / 2h / 1day / 3day |
| DurationHours | numeric | Thời lượng (giờ) |
| PriceVnd | int | Giá thanh toán |
| CreatedAt | timestamptz | Thời điểm tạo session |
| ActivatedAt | timestamptz | Thời điểm admin kích hoạt |
| ExpiresAt | timestamptz | Thời điểm hết hạn |
| IsActive | bool | Đã kích hoạt chưa |

---

## 7. Kịch bản sử dụng (User Journeys)

### Kịch bản 1: Lần đầu dùng app — đăng ký gói
```
1. Mở app → hiện trang chọn gói (không thể bỏ qua)
2. Chọn "2 Tiếng - 18.000đ"
3. Hiện QR chuyển khoản VietQR + Mã thiết bị (VD: A3F8B2C1D4)
4. Mở app ngân hàng → quét QR → điền mã thiết bị vào nội dung CK
5. Chuyển khoản 18.000đ
6. Admin thấy tiền → vào Supabase → chạy: SELECT activate_session('A3F8B2C1D4')
7. App phát hiện IsActive = true → lưu ExpiresAt → mở app chính
8. Sau 2 tiếng → hiện alert "Hết hạn" → về trang chọn gói gia hạn
```

### Kịch bản 2: Khám phá bản đồ & chỉ đường
```
1. Vào tab Bản đồ → thấy các marker đỏ (POI) và marker xanh (vị trí mình)
2. Tap marker "Nhà thờ Đức Bà" → hiện bottom card
3. Bấm "Chỉ đường" → OSRM tính route → vẽ đường trên bản đồ
4. Bấm "Chi tiết" → PlaceDetailPage với gallery ảnh và thông tin đầy đủ
5. Từ PlaceDetailPage bấm "Chỉ đường" → tự chuyển sang MapPage và route
```

### Kịch bản 3: Thuyết minh tự động khi đi bộ
```
1. Bật GPS → đi bộ trên đường Đồng Khởi
2. Đi qua Nhà hát Lớn (trong bán kính 50m)
3. Đứng 2 giây → app tự đọc: "Chào mừng bạn đến Nhà hát Thành phố..."
4. Đi tiếp → vào bán kính Bưu điện Trung tâm
5. App đọc thuyết minh Bưu điện (Nhà hát đang trong cooldown 10 phút)
```

### Kịch bản 4: Tìm quán cà phê gần đây
```
1. Vào tab Nổi bật → bấm chip "Cà phê"
2. Danh sách lọc còn toàn quán cà phê
3. Gõ "Bệt" → tìm thấy "Cà Phê Bệt Nguyễn Huệ"
4. Tap card → xem ảnh, giờ mở cửa, giá, địa chỉ
5. Bấm "Chỉ đường" → navigate về MapPage, tự route đến quán
```

### Kịch bản 5: Theo tour Quận 1 - Di tích lịch sử
```
1. Vào tab Tour → chọn "Tour Di tích Quận 1"
2. Xem 5 điểm dừng theo thứ tự: Dinh Thống Nhất → Nhà thờ Đức Bà → ...
3. Bấm điểm dừng 1 → xem chi tiết, bấm "Chỉ đường"
4. Di chuyển thực tế → đến gần Dinh Thống Nhất → app tự đọc thuyết minh
5. Tiếp tục đến điểm dừng tiếp theo
```

---

## 8. Yêu cầu phi chức năng

### 8.1 Hiệu năng
| Tiêu chí | Yêu cầu |
|---|---|
| Khởi động app | < 3 giây |
| Load danh sách Places lần đầu | < 2 giây |
| Geofence trigger từ lúc vào vùng | < 5 giây |
| Polling xác nhận thanh toán | Mỗi 5 giây |
| Timer kiểm tra hết hạn | Mỗi 60 giây |
| Cache Places in-memory | Không load lại khi chuyển tab |

### 8.2 Độ tin cậy
- GPS foreground service không bị Android kill khi chạy nền
- Polling tự retry khi mất mạng (try/catch + delay)
- Session lưu local Preferences — không mất khi tắt/mở lại app
- Supabase timeout: app hiện thông báo lỗi thay vì crash

### 8.3 Bảo mật
- Supabase Anon Key nhúng trong build (giới hạn bởi RLS trên Supabase)
- DeviceId là GUID ngẫu nhiên — không liên kết thông tin cá nhân
- Không lưu thông tin thanh toán trong app
- Session expiry check phía client (độ trễ tối đa 60 giây)

### 8.4 Khả năng sử dụng
- Giao diện Dark Gold theme — đọc tốt ngoài trời
- Toàn bộ thao tác chính thực hiện được bằng 1 tay
- Không yêu cầu tạo tài khoản — dùng ngay sau thanh toán
- TTS tự động — không cần bấm gì khi đang đi bộ

---

## 9. Thiết kế giao diện

### Bảng màu (Dark Gold Theme)
| Tên | Hex | Dùng cho |
|---|---|---|
| Background | `#0F0E0D` | Nền trang |
| Surface | `#1A1410` | Card, header, panel |
| Surface2 | `#26201A` | Icon bg, chip, button phụ |
| Border | `#2A2018` | Viền card |
| Primary text | `#F0E6D3` | Tiêu đề, tên địa điểm |
| Secondary text | `#5A4A3A` | Mô tả, subtitle |
| Gold accent | `#C8A96E` | Nút chính, label, sao rating |
| Red accent | `#E94560` | Marker POI, trạng thái đóng cửa |
| Blue | `#1E90FF` | Marker vị trí người dùng |
| Green | `#4CAF50` | Trạng thái đang mở cửa |

### Nguyên tắc thiết kế
- **Location-First:** Mọi thao tác đều có thể bắt đầu từ bản đồ
- **Tự động hóa tối đa:** Geofence + TTS không cần bấm nút
- **Thông tin tối giản:** Chỉ hiển thị những gì cần thiết tại thời điểm đó
- **Một tay:** Tất cả nút hành động trong tầm với ngón cái

---

## 10. Rủi ro & Giải pháp

| Rủi ro | Khả năng | Mức ảnh hưởng | Giải pháp |
|---|---|---|---|
| GPS không chính xác trong tòa nhà / ngõ hẻm | Cao | Trung bình | Cho phép cấu hình bán kính per-place, debounce 2 giây giảm false trigger |
| TTS tiếng Việt chất lượng thấp trên một số máy | Trung bình | Thấp | Hướng dẫn user bật Google Neural TTS trong cài đặt |
| Android kill foreground service khi pin yếu | Trung bình | Cao | Request quyền `FOREGROUND_SERVICE`, hiển thị notification thường trực |
| Mất kết nối khi polling kích hoạt session | Cao | Cao | Retry tự động mỗi 5 giây, hiện trạng thái "Đang kết nối lại..." |
| User chỉnh giờ máy để kéo dài session | Thấp | Trung bình | Chấp nhận ở phiên bản hiện tại, sửa bằng server time ở giai đoạn sau |
| Người dùng không muốn trả phí vì có Google Maps miễn phí | Cao | Cao | Nhấn mạnh tính năng TTS tự động — Google Maps không có |
| Supabase free tier giới hạn 500MB storage | Thấp | Thấp | Ảnh địa điểm lưu URL ngoài (Google Photos, CDN), không lưu file trực tiếp |

---

## 11. Đối thủ cạnh tranh

| Ứng dụng | Điểm mạnh | Thiếu gì so với TourGuideAPP |
|---|---|---|
| Google Maps | Bản đồ chính xác, routing toàn cầu | Không có thuyết minh, không tự động |
| TripAdvisor | Review và rating phong phú | Không có GPS geofence, không tự trigger |
| Klook / GetYourGuide | Đặt tour chuyên nghiệp | Không có app hướng dẫn realtime |
| Izi.travel | App audio tour | Phải bấm thủ công, UX phức tạp |

**Lợi thế cạnh tranh của TourGuideAPP:**
- **Tự động 100%** — geofence trigger TTS, không cần bấm nút
- **Không cần tài khoản** — thanh toán xong dùng ngay
- **Gói linh hoạt** — từ 1 tiếng đến 3 ngày, phù hợp nhiều nhu cầu
- **Địa phương hóa** — nội dung tập trung TP.HCM, chi tiết hơn app toàn cầu

---

## 12. Hằng số kỹ thuật (Key Constants)

| Hằng số | Giá trị | Vị trí trong code |
|---|---|---|
| Geofence Radius | 50m (mặc định, per-place) | `GeofenceEngine.cs` |
| Geofence Cooldown | 10 phút | `GeofenceEngine.cs` |
| Geofence Debounce | 2 giây | `GeofenceEngine.cs` |
| Geofence Priority | 1 (mặc định) | `Places.Priority` |
| Polling kích hoạt | 5 giây | `AccessSessionService.cs` |
| Timer hết hạn | 60 giây | `AccessSessionService.cs` |
| Reverse geocode cache | 20 giây | `MainPage.cs` |
| OSRM Endpoint | `router.project-osrm.org` | `MapPage.xaml.cs` |
| Map Center | 10.7615, 106.7033 (TP.HCM) | `MapPage.xaml.cs` |
| Map Tile | CartoDB Voyager | `MapPage.xaml.cs` |
| TTS Locale mặc định | `vi-VN` | `NarrationService.cs` |
| TTS Locales hỗ trợ | vi-VN, en-US, zh-CN, ko-KR, ja-JP, fr-FR, th-TH | `NarrationService.cs` |
| Supabase Region | ap-southeast-1 (Singapore) | `MauiProgram.cs` |
| Session DeviceId | GUID 10 ký tự | `AccessSessionService.cs` |
| Gói ngắn nhất | 1 tiếng — 10.000đ | `SubscriptionPage.xaml` |
| Gói dài nhất | 3 ngày — 120.000đ | `SubscriptionPage.xaml` |
| Bank VietQR | MB Bank | `Constants.cs` |

---

## 13. Lộ trình phát triển

### Giai đoạn 1 — MVP (Q2 2026) ✅ Hoàn thành
- ✅ Kiểm soát truy cập theo gói thời gian (VietQR + polling)
- ✅ GPS Foreground Service (Android) + định vị realtime
- ✅ Geofence + TTS tự động (debounce, cooldown, priority)
- ✅ Bản đồ Mapsui CartoDB Voyager + marker POI tùy chỉnh SkiaSharp
- ✅ Chỉ đường OSRM + polyline
- ✅ Danh sách địa điểm + tìm kiếm/lọc theo danh mục
- ✅ Chi tiết địa điểm + gallery ảnh
- ✅ Tour có sẵn + chi tiết điểm dừng
- ✅ TTS đa ngôn ngữ 7 ngôn ngữ (bảng PlaceTtsContents)

### Giai đoạn 2 — Dữ liệu thật & Admin (Q3 2026)
- [ ] Script enrich dữ liệu từ Google Places API (ảnh, rating, giờ mở cửa thật)
- [ ] Giao diện admin web kích hoạt session (thay vì SQL thủ công)
- [ ] Nội dung TTS cho tất cả địa điểm, đủ 7 ngôn ngữ
- [ ] Hỗ trợ audio file MP3 thật thay vì TTS tổng hợp

### Giai đoạn 3 — Bảo mật & Offline (Q4 2026)

**Bảo mật:**
- [ ] Xác thực session bằng server time (chống chỉnh giờ máy)
- [ ] Rate limiting trên Supabase RLS
- [ ] API key rotation cho Supabase Anon Key

**Offline:**
- [ ] Cache map tiles Mapsui khi còn mạng để dùng offline
- [ ] Cache Places in-memory persist khi tắt app (local JSON)
- [ ] Background sync khi có mạng trở lại

**Tính năng mở rộng:**
- [ ] Hệ thống đánh giá địa điểm (1–5 sao) tích hợp web
- [ ] Tối ưu pin: giảm tần suất GPS khi không di chuyển
- [ ] Mở rộng khu vực: Quận 1, Quận 7, Thủ Đức
- [ ] Push notification khi gần POI mới chưa từng ghé

---

## 14. UML Diagrams

### 13.1 ER Diagram — Toàn bộ Database

```mermaid
erDiagram
    Users {
        int UserId PK
        string FullName
        string Email UK
        string Phone
        string PasswordHash
        string Role
        string AvatarUrl
        bool IsActive
        datetime CreatedAt
        datetime LastLoginAt
    }

    Categories {
        int CategoryId PK
        string Name
        string Icon
        string ColorHex
    }

    Places {
        int PlaceId PK
        string Name
        string Description
        string Address
        double Latitude
        double Longitude
        string Phone
        string Website
        string OpenTime
        string CloseTime
        decimal PriceMin
        decimal PriceMax
        string Specialty
        string District
        bool HasParking
        bool HasAircon
        double AverageRating
        int TotalReviews
        int TotalVisits
        string Status
        string OpenStatus
        bool IsActive
        int CategoryId FK
        int OwnerId FK
        string TtsScript
        string TtsLocale
        float Radius
        int CooldownMinutes
        int Priority
        datetime CreatedAt
        datetime UpdatedAt
    }

    PlaceImages {
        int ImageId PK
        int PlaceId FK
        string ImageUrl
        bool IsMain
        int SortOrder
    }

    Reviews {
        int ReviewId PK
        int UserId FK
        int PlaceId FK
        int Rating
        string Comment
        string OwnerReply
        int TasteRating
        int PriceRating
        int SpaceRating
        bool IsHidden
        string HiddenNote
        datetime CreatedAt
    }

    RefreshTokens {
        int Id PK
        int UserId FK
        string Token UK
        datetime ExpiresAt
        datetime CreatedAt
        bool IsRevoked
        string DeviceInfo
    }

    PlaceTtsContents {
        int Id PK
        int PlaceId FK
        string Locale
        string Script
    }

    Promotions {
        int PromoId PK
        int PlaceId FK
        string Title
        string Description
        int Discount
        string VoucherCode
        datetime StartDate
        datetime EndDate
        bool IsActive
        datetime CreatedAt
    }

    UserTracking {
        long TrackId PK
        int UserId FK
        double Latitude
        double Longitude
        double Accuracy
        datetime RecordedAt
    }

    AccessSessions {
        string SessionId PK
        string DeviceId
        string PackageId
        double DurationHours
        int PriceVnd
        datetime CreatedAt
        datetime ActivatedAt
        datetime ExpiresAt
        bool IsActive
    }

    Users ||--o{ Places : "owns (OwnerId)"
    Users ||--o{ Reviews : "writes"
    Users ||--o{ RefreshTokens : "has"
    Users ||--o{ UserTracking : "generates"
    Categories ||--o{ Places : "classifies"
    Places ||--o{ PlaceImages : "has (Cascade)"
    Places ||--o{ Reviews : "receives"
    Places ||--o{ Promotions : "has (Cascade)"
    Places ||--o{ PlaceTtsContents : "has scripts"
```

---

### 13.2 Class Diagram — Mobile App (TourGuideAPP)

```mermaid
classDiagram
    class Place {
        +int PlaceId
        +string Name
        +string Address
        +double Latitude
        +double Longitude
        +string Phone
        +string OpenTime
        +string CloseTime
        +decimal PriceMin
        +decimal PriceMax
        +float AverageRating
        +int TotalReviews
        +string Specialty
        +bool IsActive
        +bool IsApproved
        +string TtsScript
        +string TtsLocale
        +float Radius
        +int CooldownMinutes
        +int Priority
        +string ImageUrl
        +DateTime LastPlayedAt
        +GetScriptForLocale(locale) string
    }

    class PlaceImage {
        +int ImageId
        +int PlaceId
        +string ImageUrl
        +bool IsMain
        +int SortOrder
    }

    class AccessSession {
        +string SessionId
        +string DeviceId
        +string PackageId
        +double DurationHours
        +int PriceVnd
        +DateTime CreatedAt
        +DateTime ActivatedAt
        +DateTime ExpiresAt
        +bool IsActive
    }

    class PlaceService {
        -Supabase.Client _supabase
        -List~Place~ _cache
        +GetAllPlacesAsync() Task~List~Place~~
        +GetCachedPlaces() List~Place~
    }

    class LocationService {
        +event Action~Location~ LocationChanged
        +StartAsync() Task
        +StopAsync() Task
    }

    class GeofenceEngine {
        +FindNearestPOI(lat, lon, places) Place
        -Haversine(lat1, lon1, lat2, lon2) double
        -IsInCooldown(place) bool
    }

    class NarrationService {
        +static List SupportedLocales
        +string PreferredLocale
        +SpeakAsync(text) Task
        +StopAsync() Task
    }

    class AccessSessionService {
        -Supabase.Client _supabase
        +GetDeviceId() string
        +IsAccessValid() bool
        +CreatePendingSessionAsync(pkg) Task~AccessSession~
        +StartPollingForActivation(sessionId) void
        +StartExpiryTimer() void
        +ClearLocalSession() void
        +event Action AccessExpired
    }

    class AuthService {
        +GetLocalEmail() string
        +SaveLocalEmail(email) void
    }

    class POIService {
        -PlaceService _placeService
        +GetAllPlacesAsync() Task~List~Place~~
        +GetCachedPlaces() List~Place~
    }

    class MainPage {
        -PlaceService _placeService
        -LocationService _locationService
        -GeofenceEngine _geofenceEngine
        -NarrationService _narrationService
        -AuthService _authService
        -List~Place~ _allPlaces
        -string _selectedCategory
        +LoadPlaces() Task
        +ApplyFilters() void
        +SelectCategory(cat) void
    }

    class MapPage {
        -PlaceService _placeService
        -LocationService _locationService
        -GeofenceEngine _geofenceEngine
        -NarrationService _narrationService
        -bool _followUserLocation
        -bool _programmaticNav
        +static PendingRoute
        +ShowRouteToDestinationAsync(lat, lon) Task
        +ShowPlaceCard(place) void
    }

    class PlaceDetailPage {
        -Place _place
        -AuthService _authService
        -LocationService _locationService
        -NarrationService _narrationService
        +LoadPlaceDetail() void
        +OnDirectionsClicked() void
    }

    class ToursPage {
        -List~Place~ _places
        -TourFilters _filters
        +ObservableCollection~TourCard~ Tours
        +RebuildTours() void
    }

    class TourDetailPage {
        -TourCard _tour
        -LocationService _locationService
        -NarrationService _narrationService
    }

    class SubscriptionPage {
        -AccessSessionService _accessService
        +OnPackageSelected(pkg) void
    }

    class PaymentQRPage {
        -AccessSessionService _accessService
        -AccessSession _session
        +StartPolling() void
    }

    PlaceService "1" --> "*" Place : loads
    PlaceService "1" --> "*" PlaceImage : loads
    POIService --> PlaceService : wraps
    GeofenceEngine ..> Place : analyzes
    NarrationService ..> Place : reads TtsScript
    AccessSessionService --> AccessSession : manages
    MainPage --> PlaceService
    MainPage --> LocationService
    MainPage --> GeofenceEngine
    MainPage --> NarrationService
    MapPage --> PlaceService
    MapPage --> LocationService
    MapPage --> GeofenceEngine
    MapPage --> NarrationService
    PlaceDetailPage --> NarrationService
    PlaceDetailPage --> Place
    ToursPage ..> TourDetailPage : navigates
    TourDetailPage --> NarrationService
    SubscriptionPage --> AccessSessionService
    PaymentQRPage --> AccessSessionService
```

---

### 13.3 Class Diagram — Web API (TourGuideAPI)

```mermaid
classDiagram
    class AuthController {
        -IAuthService _auth
        +Register(RegisterDto) Task~ActionResult~
        +Login(LoginDto) Task~ActionResult~
        +Refresh(RefreshTokenRequestDto) Task~ActionResult~
        +Revoke() Task~ActionResult~
        +Me() Task~ActionResult~
        +DebugLogin(userId) Task~ActionResult~
    }

    class PlacesController {
        -AppDbContext _db
        -IGeoLocationService _geo
        +GetAll(filters) Task~ActionResult~
        +GetNearby(NearbyQueryDto) Task~ActionResult~
        +GetMine(search, page) Task~ActionResult~
        +GetById(id) Task~ActionResult~
        +Create(CreatePlaceDto) Task~ActionResult~
        +Update(id, UpdatePlaceDto) Task~ActionResult~
        +UpdateStatus(id, status) Task~ActionResult~
        +AddImage(id, AddImageDto) Task~ActionResult~
        +DeleteImage(id, imageId) Task~ActionResult~
        +Delete(id) Task~ActionResult~
    }

    class ReviewsController {
        -AppDbContext _db
        -INotificationService _notify
        +GetByPlace(placeId, page) Task~ActionResult~
        +Create(CreateReviewDto) Task~ActionResult~
        +Reply(id, reply) Task~ActionResult~
    }

    class TrackingController {
        -ITrackingService _tracking
        +LogLocation(LocationDto) Task~ActionResult~
    }

    class AdminController {
        -AppDbContext _db
        +GetUsers(search, role) Task~ActionResult~
        +ToggleLock(id) Task~ActionResult~
        +ChangeRole(id, role) Task~ActionResult~
        +GetPlaces(pendingOnly) Task~ActionResult~
        +ApprovePlace(id) Task~ActionResult~
        +SuspendPlace(id) Task~ActionResult~
        +GetReviews(hiddenOnly) Task~ActionResult~
        +HideReview(id) Task~ActionResult~
        +GetStats() Task~ActionResult~
    }

    class IAuthService {
        <<interface>>
        +RegisterAsync(dto) Task~AuthResponseDto~
        +LoginAsync(dto) Task~AuthResponseDto~
        +RefreshAsync(token) Task~AuthResponseDto~
        +RevokeAsync(token) Task
        +GenerateTokenAsync(userId) Task~AuthResponseDto~
    }

    class AuthService {
        -AppDbContext _db
        -IConfiguration _config
        +RegisterAsync(dto) Task~AuthResponseDto~
        +LoginAsync(dto) Task~AuthResponseDto~
        +RefreshAsync(token) Task~AuthResponseDto~
        -BuildResponse(user) AuthResponseDto
        -GenerateJwt(user) string
    }

    class IGeoLocationService {
        <<interface>>
        +GetNearbyAsync(dto) Task~List~PlaceDto~~
        +DetectNearestPlaceAsync(lat, lng, radius) Task~Place~
        +CalcDistanceKm(lat1, lng1, lat2, lng2) double
    }

    class GeoLocationService {
        -AppDbContext _db
        +GetNearbyAsync(dto) Task~List~PlaceDto~~
        +DetectNearestPlaceAsync(lat, lng, radius) Task~Place~
        +CalcDistanceKm(lat1, lng1, lat2, lng2) double
    }

    class ITrackingService {
        <<interface>>
        +LogLocationAsync(userId, dto) Task
    }

    class TrackingService {
        -AppDbContext _db
        -IGeoLocationService _geo
        -INotificationService _notify
    }

    class INotificationService {
        <<interface>>
        +SendNewCheckIn(ownerId, placeName, userName) Task
        +SendNewReview(ownerId, placeName, rating) Task
    }

    class NotificationHub {
        +JoinOwnerGroup(ownerId) Task
        +LeaveOwnerGroup(ownerId) Task
    }

    class AppDbContext {
        +DbSet~User~ Users
        +DbSet~Place~ Places
        +DbSet~PlaceImage~ PlaceImages
        +DbSet~Category~ Categories
        +DbSet~Review~ Reviews
        +DbSet~RefreshToken~ RefreshTokens
        +DbSet~Promotion~ Promotions
        +DbSet~UserTracking~ UserTracking
        +DbSet~PlaceTtsContent~ PlaceTtsContents
        +OnModelCreating(builder) void
    }

    AuthController --> IAuthService
    PlacesController --> AppDbContext
    PlacesController --> IGeoLocationService
    ReviewsController --> AppDbContext
    ReviewsController --> INotificationService
    TrackingController --> ITrackingService
    AdminController --> AppDbContext
    IAuthService <|.. AuthService
    IGeoLocationService <|.. GeoLocationService
    ITrackingService <|.. TrackingService
    TrackingService --> IGeoLocationService
    TrackingService --> INotificationService
    AuthService --> AppDbContext
    GeoLocationService --> AppDbContext
```

---

### 13.4 Sequence Diagrams — Mobile App

#### 13.4.1 Chọn & thanh toán gói sử dụng

```mermaid
sequenceDiagram
    actor User
    actor Admin
    participant App
    participant SubscriptionPage
    participant PaymentQRPage
    participant AccessSessionService
    participant Supabase
    participant Preferences
    participant AppShell as Main App

    Note over User,App: Bước 1 — Mở app, kiểm tra session
    User->>App: Mở ứng dụng
    App->>AccessSessionService: IsAccessValid()
    AccessSessionService->>Preferences: Đọc ExpiresAt
    Preferences-->>AccessSessionService: DateTime
    alt Session còn hạn
        AccessSessionService-->>App: true
        App->>AppShell: Vào giao diện chính
        App->>AccessSessionService: StartExpiryTimer()
    else Hết hạn hoặc chưa có
        AccessSessionService-->>App: false
        App->>SubscriptionPage: Hiện trang chọn gói
    end

    Note over User,PaymentQRPage: Bước 2 — Chọn gói & tạo QR
    User->>SubscriptionPage: Chọn gói (1h / 2h / 1day / 3day)
    SubscriptionPage->>PaymentQRPage: Navigate(package)
    PaymentQRPage->>AccessSessionService: CreatePendingSessionAsync(package)
    AccessSessionService->>Preferences: Lấy hoặc tạo DeviceId
    Preferences-->>AccessSessionService: DeviceId
    AccessSessionService->>Supabase: INSERT AccessSessions (IsActive=false)
    Supabase-->>AccessSessionService: SessionId
    AccessSessionService-->>PaymentQRPage: AccessSession (SessionId, DeviceId)
    PaymentQRPage-->>User: Hiện mã QR VietQR

    Note over User,AppShell: Bước 3 — Polling & kích hoạt
    User->>PaymentQRPage: Chuyển khoản theo mã QR
    PaymentQRPage->>AccessSessionService: StartPollingForActivation(sessionId)
    Note over AccessSessionService,Supabase: Nội bộ — poll mỗi 5 giây
    Admin->>Supabase: UPDATE IsActive=true, ExpiresAt=NOW+duration
    AccessSessionService->>Supabase: SELECT session (internal poll)
    Supabase-->>AccessSessionService: IsActive=true, ExpiresAt
    AccessSessionService->>Preferences: Lưu ExpiresAt, SessionId
    AccessSessionService-->>PaymentQRPage: OnActivated callback
    PaymentQRPage->>AccessSessionService: StartExpiryTimer()
    PaymentQRPage->>AppShell: Application.MainPage = new AppShell()
    AppShell-->>User: Vào giao diện chính

    Note over App,AppShell: Bước 4 — Timer tự động khóa khi hết hạn
    Note over AccessSessionService: StartExpiryTimer() chạy ngầm sau khi kích hoạt
    loop Nội bộ — mỗi 60 giây
        AccessSessionService->>Preferences: Đọc ExpiresAt
        AccessSessionService->>AccessSessionService: IsAccessValid()
        alt Hết hạn
            AccessSessionService->>App: AccessExpired event
            App->>User: Alert "Gói đã hết hạn"
            App->>SubscriptionPage: Quay về trang chọn gói
        end
    end
```

---

#### 13.4.2 Tìm kiếm & lọc địa điểm

```mermaid
sequenceDiagram
    actor User
    participant MainPage
    participant PlaceService
    participant Cache as In-memory Cache
    participant Supabase

    Note over MainPage,Supabase: Bước 1 — Tải dữ liệu vào cache
    MainPage->>PlaceService: GetAllPlacesAsync()
    PlaceService->>Cache: Kiểm tra _cache.Count > 0
    alt Cache trống
        PlaceService->>Supabase: SELECT Places (IsActive, IsApproved)
        Supabase-->>PlaceService: Places[]
        PlaceService->>Supabase: SELECT PlaceImages
        Supabase-->>PlaceService: PlaceImages[]
        PlaceService->>Cache: Lưu vào _cache
    end
    PlaceService-->>MainPage: Places[]
    MainPage-->>User: Hiện danh sách đầy đủ

    Note over User,MainPage: Bước 2 — Tìm kiếm theo tên
    User->>MainPage: Nhập từ khoá vào SearchBar
    MainPage->>MainPage: Lọc theo Name.Contains(keyword)
    MainPage-->>User: Cập nhật danh sách

    Note over User,MainPage: Bước 3 — Lọc theo loại / khu vực
    User->>MainPage: Chọn chip lọc
    MainPage->>MainPage: Lọc thêm theo CategoryId / District
    MainPage-->>User: Cập nhật danh sách
```

---

#### 13.4.3 Xem bản đồ & marker địa điểm

```mermaid
sequenceDiagram
    actor User
    participant MapPage
    participant PlaceService
    participant LocationService
    participant Mapsui

    Note over User,Mapsui: Bước 1 — Hiển thị bản đồ
    User->>MapPage: Mở tab Bản đồ
    MapPage->>PlaceService: GetCachedPlaces()
    PlaceService-->>MapPage: Places[]
    MapPage->>LocationService: Lấy vị trí hiện tại
    LocationService-->>MapPage: (lat, lon)
    MapPage->>Mapsui: Vẽ marker POI đỏ + marker user xanh
    Mapsui-->>User: Hiển thị bản đồ + vị trí

    Note over User,MapPage: Bước 2 — Tap marker xem thông tin
    User->>Mapsui: Tap marker POI
    Mapsui->>MapPage: OnMapInfo(e)
    MapPage->>PlaceService: GetCachedPlaces().Find(PlaceId)
    PlaceService-->>MapPage: Place
    MapPage-->>User: Hiện bottom card (tên, ảnh, giờ, nút chỉ đường)
```

---

#### 13.4.4 Chỉ đường đến địa điểm

```mermaid
sequenceDiagram
    actor User
    participant MapPage
    participant PlaceDetailPage
    participant LocationService
    participant OSRM

    Note over User,OSRM: Luồng 1 — Chỉ đường từ bottom card bản đồ
    User->>MapPage: Bấm "Chỉ đường" trên card
    MapPage->>LocationService: Lấy vị trí hiện tại
    LocationService-->>MapPage: (lat, lon) origin
    MapPage->>OSRM: GET /route origin → destination
    OSRM-->>MapPage: GeoJSON polyline
    MapPage->>MapPage: Vẽ polyline + zoom vào route
    MapPage-->>User: Hiển thị đường đi + nút Hủy

    User->>MapPage: Bấm "Hủy route"
    MapPage->>MapPage: Xóa polyline, ẩn CancelRoutePanel

    Note over User,OSRM: Luồng 2 — Chỉ đường từ PlaceDetailPage
    User->>PlaceDetailPage: Bấm "Chỉ đường"
    PlaceDetailPage->>MapPage: PendingRoute = (lat, lon, name)
    PlaceDetailPage->>PlaceDetailPage: Shell.GoToAsync("//MainTabs/MapPage")
    MapPage->>MapPage: OnAppearing() — đọc PendingRoute
    MapPage->>LocationService: Lấy vị trí hiện tại
    LocationService-->>MapPage: (lat, lon) origin
    MapPage->>OSRM: GET /route origin → destination
    OSRM-->>MapPage: Polyline
    MapPage-->>User: Hiển thị đường đi
```

---

#### 13.4.5 Nghe thuyết minh tự động (Geofence + TTS)

```mermaid
sequenceDiagram
    actor User
    participant OS as Hệ điều hành
    participant LocationService
    participant MapPage
    participant GeofenceEngine
    participant NarrationService
    participant Preferences
    participant AccountPage

    Note over OS,NarrationService: Bước 1 — Phát hiện POI gần vị trí
    OS->>LocationService: Vị trí GPS mới
    LocationService->>MapPage: LocationChanged event
    MapPage->>GeofenceEngine: FindNearestPOI(lat, lon, places)
    GeofenceEngine->>GeofenceEngine: Lọc POI trong radius + cooldown
    GeofenceEngine->>GeofenceEngine: Sắp xếp Priority ↓, Distance ↑
    GeofenceEngine->>GeofenceEngine: Debounce 2 giây
    alt Tìm được POI phù hợp
        GeofenceEngine-->>MapPage: Place (nearest)
        MapPage->>NarrationService: SpeakAsync(place.GetScriptForLocale(locale))
        NarrationService-->>User: Phát thuyết minh tự động
        MapPage->>MapPage: Ghi _lastSpokenPlaceId, LastPlayedAt
    else Không có POI
        GeofenceEngine-->>MapPage: null
    end

    Note over User,Preferences: Bước 2 — Chọn ngôn ngữ thuyết minh
    User->>AccountPage: Mở tab Cài đặt
    AccountPage->>NarrationService: SupportedLocales
    NarrationService-->>AccountPage: 7 ngôn ngữ
    AccountPage->>Preferences: Get tts_preferred_locale
    Preferences-->>AccountPage: locale hiện tại
    AccountPage-->>User: Hiện Picker ngôn ngữ

    User->>AccountPage: Chọn ngôn ngữ mới (vd: en-US)
    AccountPage->>NarrationService: PreferredLocale = "en-US"
    NarrationService->>Preferences: Set tts_preferred_locale = "en-US"
    AccountPage-->>User: Cập nhật hiển thị
```

---

#### 13.4.6 Xem & theo tour có sẵn

```mermaid
sequenceDiagram
    actor User
    participant ToursPage
    participant TourDetailPage
    participant MapPage
    participant LocationService
    participant OSRM

    Note over User,ToursPage: Bước 1 — Xem danh sách tour
    User->>ToursPage: Mở tab Tour
    ToursPage->>ToursPage: EnsurePlacesLoadedAsync()
    ToursPage->>ToursPage: RebuildTours() — tạo 3 tour từ Places
    ToursPage-->>User: Hiện danh sách (Quick / Balanced / Full)

    Note over User,TourDetailPage: Bước 2 — Xem chi tiết tour
    User->>ToursPage: Chọn tour
    ToursPage->>TourDetailPage: Navigate(TourCard)
    TourDetailPage-->>User: Danh sách điểm dừng theo thứ tự

    Note over User,OSRM: Bước 3 — Bắt đầu theo tour
    User->>TourDetailPage: Bấm "Bắt đầu tour"
    TourDetailPage->>MapPage: PendingRoute = điểm dừng đầu tiên
    TourDetailPage->>TourDetailPage: Shell.GoToAsync("//MainTabs/MapPage")
    MapPage->>MapPage: OnAppearing() — đọc PendingRoute
    MapPage->>LocationService: Lấy vị trí hiện tại
    LocationService-->>MapPage: (lat, lon) origin
    MapPage->>OSRM: GET /route origin → điểm dừng 1
    OSRM-->>MapPage: Polyline
    MapPage-->>User: Hiển thị đường đi đến điểm dừng 1
```

---

#### 13.4.7 Xem chi tiết địa điểm

```mermaid
sequenceDiagram
    actor User
    participant MainPage
    participant MapPage
    participant PlaceDetailPage
    participant PlaceService
    participant NarrationService
    participant LocationService
    participant OSRM

    Note over User,PlaceService: Bước 1 — Mở chi tiết
    User->>MainPage: Tap địa điểm trong danh sách
    MainPage->>PlaceDetailPage: Navigation.PushAsync(PlaceDetailPage)
    PlaceDetailPage->>PlaceService: GetCachedPlaces().Find(PlaceId)
    PlaceService-->>PlaceDetailPage: Place + PlaceImages
    PlaceDetailPage-->>User: Hiện tên, ảnh, giờ mở cửa, giá, đánh giá

    Note over User,NarrationService: Bước 2 — Nghe thuyết minh
    User->>PlaceDetailPage: Bấm "Thuyết minh"
    PlaceDetailPage->>NarrationService: SpeakAsync(GetScriptForLocale(locale))
    NarrationService-->>User: Đọc thuyết minh theo ngôn ngữ đã chọn

    Note over User,MapPage: Bước 3 — Chỉ đường từ chi tiết
    User->>PlaceDetailPage: Bấm "Chỉ đường"
    PlaceDetailPage->>MapPage: PendingRoute = (lat, lon, name)
    PlaceDetailPage->>PlaceDetailPage: Shell.GoToAsync("//MainTabs/MapPage")
    MapPage->>MapPage: OnAppearing() — đọc PendingRoute
    MapPage->>LocationService: Lấy vị trí hiện tại
    LocationService-->>MapPage: (lat, lon) origin
    MapPage->>OSRM: GET /route origin → destination
    OSRM-->>MapPage: Polyline
    MapPage-->>User: Hiển thị đường đi
```

---

### 13.5 Sequence Diagrams — Web API

#### 13.5.1 Đăng ký và đăng nhập

```mermaid
sequenceDiagram
    actor Client
    participant AuthController
    participant AuthService
    participant AppDbContext
    participant JWT

    Client->>AuthController: POST /api/auth/register
    AuthController->>AuthService: RegisterAsync(dto)
    AuthService->>AppDbContext: Kiểm tra email tồn tại
    alt Email đã tồn tại
        AppDbContext-->>AuthService: Đã tồn tại
        AuthService-->>AuthController: Lỗi
        AuthController-->>Client: 409 "Email đã được sử dụng"
    else Email chưa tồn tại
        AppDbContext-->>AuthService: Không tồn tại
        AuthService->>AuthService: BCrypt hash password
        AuthService->>AppDbContext: INSERT Users
        AuthService->>JWT: GenerateJwt(user, 15min)
        JWT-->>AuthService: AccessToken
        AuthService->>AppDbContext: INSERT RefreshToken (7 ngày)
        AuthService-->>AuthController: AuthResponseDto
        AuthController-->>Client: 201 {accessToken, refreshToken}
    end

    Client->>AuthController: POST /api/auth/login
    AuthController->>AuthService: LoginAsync(dto)
    AuthService->>AppDbContext: SELECT User WHERE Email
    alt Email không tồn tại
        AppDbContext-->>AuthService: null
        AuthService-->>AuthController: Lỗi
        AuthController-->>Client: 401 Unauthorized
    else Email tồn tại
        AppDbContext-->>AuthService: User
        AuthService->>AuthService: BCrypt.Verify(password, hash)
        alt Sai mật khẩu
            AuthService-->>AuthController: Lỗi xác thực
            AuthController-->>Client: 401 Unauthorized
        else Đúng mật khẩu
            AuthService->>JWT: GenerateJwt(user)
            JWT-->>AuthService: AccessToken
            AuthService->>AppDbContext: INSERT RefreshToken
            AuthService-->>AuthController: AuthResponseDto
            AuthController-->>Client: 200 {accessToken, refreshToken}
        end
    end
```

#### 13.5.2 Refresh token và rotation

```mermaid
sequenceDiagram
    actor Client
    participant AuthController
    participant AuthService
    participant AppDbContext
    participant JWT

    Client->>AuthController: POST /api/auth/refresh {refreshToken}
    AuthController->>AuthService: RefreshAsync(token)
    AuthService->>AppDbContext: SELECT RefreshToken WHERE Token
    AppDbContext-->>AuthService: RefreshToken

    alt Token hợp lệ và chưa hết hạn
        AuthService->>AppDbContext: UPDATE IsRevoked=true (token cũ)
        AuthService->>AppDbContext: INSERT RefreshToken mới (rotation)
        AuthService->>JWT: GenerateJwt(user)
        JWT-->>AuthService: AccessToken
        AuthService-->>AuthController: AuthResponseDto mới
        AuthController-->>Client: 200 {accessToken mới, refreshToken mới}
    else Token không hợp lệ
        AuthService-->>AuthController: null
        AuthController-->>Client: 401 Unauthorized
    end
```

#### 13.5.3 Tạo và duyệt địa điểm

```mermaid
sequenceDiagram
    actor Owner
    participant PlacesController
    participant AppDbContext
    participant AdminController
    actor Admin

    Owner->>PlacesController: POST /api/places [Bearer token]
    PlacesController->>AppDbContext: INSERT Places (Status="Pending")
    AppDbContext-->>PlacesController: Place
    PlacesController-->>Owner: 201 PlaceDto

    Admin->>AdminController: GET /api/admin/places?pendingOnly=true
    AdminController->>AppDbContext: SELECT Places WHERE Status="Pending"
    AppDbContext-->>AdminController: Places[]
    AdminController-->>Admin: Danh sách chờ duyệt

    Admin->>AdminController: PUT /api/admin/places/{id}/approve
    AdminController->>AppDbContext: UPDATE Status="Active", IsApproved=true
    AppDbContext-->>AdminController: OK
    AdminController-->>Admin: 200 "Đã duyệt"
```


#### 13.5.4 Theo dõi vị trí và auto check-in

```mermaid
sequenceDiagram
    actor Client
    participant TrackingController
    participant TrackingService
    participant GeoLocationService
    participant AppDbContext
    participant INotificationService

    Client->>TrackingController: POST /api/tracking/location {lat, lng}
    TrackingController->>TrackingService: LogLocationAsync(userId, dto)
    TrackingService->>AppDbContext: INSERT UserTracking
    TrackingService->>GeoLocationService: DetectNearestPlaceAsync(lat, lng, 100m)
    GeoLocationService->>AppDbContext: SELECT Places trong bounding box
    GeoLocationService->>GeoLocationService: Haversine distance với từng place
    GeoLocationService-->>TrackingService: Place gần nhất (nếu có)

    alt Trong bán kính 100m
        TrackingService->>INotificationService: SendNewCheckIn(ownerId, placeName, userName)
        INotificationService->>INotificationService: SignalR → owner group
    end

    TrackingService-->>TrackingController: OK
    TrackingController-->>Client: 200
```


---

### 13.6 Activity Diagrams

#### 13.6.1 Luồng khởi động app

```mermaid
flowchart TD
    A([Mở app]) --> B[Đọc Preferences]
    B --> C{SessionId tồn tại?}
    C -->|Không| D[Hiện SubscriptionPage]
    C -->|Có| E{ExpiresAt > Now?}
    E -->|Không| D
    E -->|Có| F[Khởi động AppShell]
    F --> G[Khởi động Timer 60s]
    G --> H[Load Places từ Supabase]
    H --> I[Render bản đồ + markers]
    I --> J([App sẵn sàng])

    D --> K[Chọn gói]
    K --> L[Tạo session Supabase]
    L --> M[Hiện QR VietQR]
    M --> N{Poll 5s: IsActive?}
    N -->|Chưa| N
    N -->|Có| O[Lưu ExpiresAt vào Preferences]
    O --> F
```

#### 13.6.2 Luồng Geofence + TTS

```mermaid
flowchart TD
    A([GPS update]) --> B[LocationService.LocationChanged]
    B --> C[GeofenceEngine.FindNearestPOI]
    C --> D{Có POI trong radius?}
    D -->|Không| E[_lastSpokenPlaceId = null]
    E --> Z([Kết thúc])
    D -->|Có| F{POI trong cooldown?}
    F -->|Có| G[Bỏ qua POI này]
    G --> D
    F -->|Không| H[Sắp xếp theo Priority + Distance]
    H --> I{POI == _lastSpokenPlaceId?}
    I -->|Có| Z
    I -->|Không| J[Debounce 2 giây]
    J --> K[NarrationService.SpeakAsync]
    K --> L[Cập nhật _lastSpokenPlaceId]
    L --> M[Cập nhật LastPlayedAt]
    M --> Z
```

#### 13.6.3 Luồng thanh toán và kích hoạt

```mermaid
flowchart TD
    A([User mở app lần đầu]) --> B[SubscriptionPage]
    B --> C[Chọn gói thời gian]
    C --> D[Tạo AccessSession - IsActive=false]
    D --> E[Hiện QR VietQR + DeviceId]
    E --> F[User chuyển khoản ngân hàng]
    F --> G[App polling Supabase mỗi 5s]
    G --> H{Admin thấy tiền}
    H -->|Chưa| G
    H -->|Có| I[Admin: activate_session DeviceId]
    I --> J[Supabase: IsActive=true, ExpiresAt=Now+duration]
    J --> K[App poll nhận IsActive=true]
    K --> L[Lưu ExpiresAt vào Preferences]
    L --> LA[StartExpiryTimer]
    LA --> M[Application.MainPage = AppShell]
    M --> N([Dùng app bình thường])
    N --> O{Timer 60s kiểm tra}
    O -->|Còn hạn| O
    O -->|Hết hạn| P[Alert hết hạn]
    P --> B
```

#### 13.6.4 Luồng duyệt địa điểm (Web)

```mermaid
flowchart TD
    A([Owner tạo Place]) --> B[Status = Pending]
    B --> C[Admin vào /admin/places]
    C --> D[Xem danh sách chờ duyệt]
    D --> E{Quyết định}
    E -->|Duyệt| F[PUT /approve → Status=Active]
    E -->|Từ chối| G[PUT /suspend → Status=Suspended]
    F --> H[Place hiện trên app mobile]
    G --> I[Place bị ẩn]
    H --> J{Owner cập nhật?}
    J -->|Có| K[PUT /api/places/id]
    K --> H
```

#### 13.6.5 Luồng đăng nhập Web MVC

```mermaid
flowchart TD
    A([Truy cập trang có Auth]) --> B{Session có token?}
    B -->|Không| C[Redirect /Auth/Login]
    B -->|Có| D{Role đủ quyền?}
    D -->|Không| E[Redirect /Auth/AccessDenied]
    D -->|Có| F[Tiếp tục request]
    C --> G[Nhập email + password]
    G --> H[POST /Auth/Login]
    H --> I{Credentials hợp lệ?}
    I -->|Không| J[Hiện lỗi]
    J --> G
    I -->|Có| K[Lưu token vào Session]
    K --> L{Role?}
    L -->|Admin| M[Redirect /Admin]
    L -->|Owner| N[Redirect /Dashboard]
```

---

**Tổng:** 1 ER + 2 Class + 11 Sequence + 5 Activity + 1 BFD + 1 Use Case = **21 diagrams**

---

### 13.7 BFD Bậc 1 — TourGuideAPP

> **BFD (Biểu đồ phân rã chức năng)** — thể hiện toàn bộ chức năng hệ thống phân theo cấp bậc.

```mermaid
graph LR
    ROOT["Hệ thống\nTourGuideAPP"]

    ROOT --> F1["1. Quản lý\nphiên truy cập"]
    ROOT --> F2["2. Khám phá\nđịa điểm"]
    ROOT --> F3["3. Bản đồ\nvà Định vị"]
    ROOT --> F4["4. Chỉ đường"]
    ROOT --> F5["5. Thuyết minh\ntự động"]
    ROOT --> F6["6. Quản lý Tour"]

    F1 --> F1_1["1.1 Kiểm tra phiên còn hạn"]
    F1 --> F1_2["1.2 Chọn gói truy cập"]
    F1 --> F1_3["1.3 Tạo QR thanh toán VietQR"]
    F1 --> F1_4["1.4 Kích hoạt phiên sau thanh toán"]
    F1 --> F1_5["1.5 Thông báo hết hạn phiên"]

    F2 --> F2_1["2.1 Tải danh sách địa điểm"]
    F2 --> F2_2["2.2 Tìm kiếm theo tên"]
    F2 --> F2_3["2.3 Lọc theo loại / khu vực"]
    F2 --> F2_4["2.4 Xem chi tiết địa điểm"]
    F2 --> F2_5["2.5 Gọi điện và mở website"]

    F3 --> F3_1["3.1 Hiển thị bản đồ OSM"]
    F3 --> F3_2["3.2 Theo dõi vị trí GPS"]
    F3 --> F3_3["3.3 Hiển thị marker POI"]
    F3 --> F3_4["3.4 Tap marker xem thẻ thông tin"]
    F3 --> F3_5["3.5 Tìm kiếm trên bản đồ"]

    F4 --> F4_1["4.1 Tính tuyến đường qua OSRM"]
    F4 --> F4_2["4.2 Vẽ tuyến lên bản đồ"]
    F4 --> F4_3["4.3 Hiển thị khoảng cách và ETA"]
    F4 --> F4_4["4.4 Huỷ chỉ đường"]

    F5 --> F5_1["5.1 Phát hiện POI gần vị trí"]
    F5 --> F5_2["5.2 Lấy script TTS theo ngôn ngữ"]
    F5 --> F5_3["5.3 Đọc thuyết minh bằng giọng nói"]
    F5 --> F5_4["5.4 Chọn ngôn ngữ thuyết minh"]
    F5 --> F5_5["5.5 Cooldown tránh đọc lại"]

    F6 --> F6_1["6.1 Xem danh sách tour gợi ý"]
    F6 --> F6_2["6.2 Xem chi tiết tour và điểm dừng"]
    F6 --> F6_3["6.3 Chỉ đường theo tour"]
    F6 --> F6_4["6.4 Thuyết minh từng điểm dừng"]
```

---

**Tổng:** 1 ER + 2 Class + 11 Sequence + 5 Activity + 1 BFD + 1 Use Case = **21 diagrams**

---

## Phụ lục: Thuật ngữ

| Thuật ngữ | Giải thích |
|---|---|
| **POI** | Point of Interest — địa điểm du lịch đáng chú ý |
| **Geofence** | Vùng địa lý ảo hình tròn, kích hoạt hành động khi người dùng vào trong |
| **TTS** | Text-to-Speech — đọc văn bản thành giọng nói tổng hợp |
| **OSRM** | Open Source Routing Machine — engine tính toán tuyến đường mã nguồn mở |
| **Debounce** | Kỹ thuật trì hoãn xử lý để tránh trigger liên tục khi điều kiện dao động |
| **Cooldown** | Khoảng thời gian chờ tối thiểu giữa 2 lần kích hoạt cùng một sự kiện |
| **MAUI** | Multi-platform App UI — framework .NET phát triển app đa nền tảng |
| **Supabase** | Backend-as-a-Service mã nguồn mở, cung cấp PostgreSQL + REST API |
| **Preferences** | Bộ nhớ key-value cục bộ trên thiết bị (SharedPreferences trên Android) |
| **VietQR** | Chuẩn mã QR chuyển khoản ngân hàng tại Việt Nam |
| **Foreground Service** | Tiến trình Android chạy nền với notification thường trực, không bị hệ thống kill |

---

**Nhóm phát triển:** Sinh viên đồ án  
**Cập nhật lần cuối:** Tháng 4, 2026  
**Phiên bản:** 2.2 — Xóa QR Scan, Favorites, Wishlist, History, Notes; cập nhật thư viện bản đồ CartoDB Voyager
