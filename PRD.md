# Product Requirements Document (PRD)
## TourGuideAPP — Ứng dụng Hướng dẫn Du lịch Thông minh TP.HCM

**Phiên bản:** 2.1  
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
| **Khách (User)** | Chọn & thanh toán gói sử dụng |
| | Xem bản đồ địa điểm |
| | Nghe thuyết minh tự động khi đến gần POI |
| | Xem chi tiết địa điểm |
| | Tìm kiếm & lọc địa điểm theo danh mục |
| | Xem & theo tour có sẵn |
| | Chỉ đường đến địa điểm |
| | Quét mã QR tại địa điểm |
| **Admin** | Kích hoạt gói sử dụng cho khách |
| | Quản lý nội dung địa điểm (qua Supabase) |
| **Hệ thống** | Tự động phát thuyết minh theo GPS |
| | Tự động khóa app khi hết hạn gói |
| | Polling xác nhận thanh toán |

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
| Mapsui | 5.x | Bản đồ tương tác (OpenStreetMap tiles) |
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

### 5.8 Quét mã QR
**Mục tiêu:** Cho phép truy cập thông tin địa điểm nhanh qua mã QR đặt tại chỗ

**Tính năng:**
- Mở camera → ZXing quét và giải mã QR
- QR chứa PlaceId → navigate đến PlaceDetailPage tương ứng

**Views:** `QRScanPage`

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

## 12. Lộ trình phát triển

### Giai đoạn 1 — MVP (Q2 2026) ✅ Hoàn thành
- ✅ Kiểm soát truy cập theo gói thời gian (VietQR + polling)
- ✅ GPS Foreground Service (Android) + định vị realtime
- ✅ Geofence + TTS tự động (debounce, cooldown, priority)
- ✅ Bản đồ Mapsui + marker POI tùy chỉnh SkiaSharp
- ✅ Chỉ đường OSRM + polyline
- ✅ Danh sách địa điểm + tìm kiếm/lọc theo danh mục
- ✅ Chi tiết địa điểm + gallery ảnh
- ✅ Tour có sẵn + chi tiết điểm dừng
- ✅ QR Scan cơ bản

### Giai đoạn 2 — Dữ liệu thật & Admin (Q3 2026)
- [ ] Script enrich dữ liệu từ Google Places API (ảnh, rating, giờ mở cửa thật)
- [ ] Giao diện admin web đơn giản để kích hoạt session (thay vì SQL thủ công)
- [ ] Nội dung TTS cho tất cả địa điểm (50–100 chữ/địa điểm)
- [ ] Hỗ trợ audio file MP3 thật thay vì TTS tổng hợp

### Giai đoạn 3 — Hoàn thiện (Q4 2026)
- [ ] Chế độ offline: cache dữ liệu Places khi mất mạng
- [ ] Hệ thống đánh giá địa điểm đơn giản (1–5 sao)
- [ ] Tối ưu pin: giảm tần suất GPS khi không di chuyển
- [ ] Xác thực session bằng server time (chống chỉnh giờ máy)

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
**Phiên bản:** 2.1 — Bổ sung phân tích người dùng, use case, NFR, rủi ro
