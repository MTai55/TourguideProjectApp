# CLAUDE.md — TourGuideAPP

Đây là file hướng dẫn cho AI assistant. Đọc file này trước khi làm bất cứ thứ gì.

---

## Tổng quan dự án

**TourGuideAPP** — ứng dụng hướng dẫn du lịch TP.HCM, xây dựng bằng .NET MAUI.
- Backend: Supabase (PostgreSQL + postgrest-csharp, **không** dùng Supabase Auth)
- Bản đồ: Mapsui (CartoDB Voyager tiles)
- Định tuyến: OSRM public API
- Barcode: ZXing.Net.MAUI
- Target: Android (chính), iOS

---

## Cấu trúc thư mục

```
TourGuideAPP/
├── App.xaml/.cs                  — Entry point, kiểm tra access, chọn MainPage
├── AppShell.xaml/.cs             — Tab bar chính (4 tab)
├── Views/
│   ├── LanguageSelectionPage     — Chọn ngôn ngữ (vi/en), hiện trước subscription
│   ├── SubscriptionPage          — Chọn gói trả phí (1h/2h/1day/3day)
│   ├── PaymentQRPage             — Hiện QR VietQR, polling chờ admin kích hoạt
│   ├── MapPage                   — Tab bản đồ, GPS, POI markers, chỉ đường OSRM
│   ├── MainPage                  — Tab nổi bật, danh sách Places, search/filter
│   ├── ToursPage                 — Tab tour, tạo gợi ý tour từ Places
│   ├── TourDetailPage            — Chi tiết tour, danh sách điểm dừng
│   ├── AccountPage               — Tab cài đặt, lịch sử, chọn ngôn ngữ TTS
│   ├── PlaceDetailPage           — Chi tiết địa điểm (push từ MainPage/MapPage)
│   └── QRScanPage                — Quét QR check-in (ZXing)
├── Services/
│   ├── AccessSessionService.cs   — Quản lý subscription: DeviceId, session, polling, expiry timer, heartbeat online
│   ├── PlaceService.cs           — Load Places + PlaceImages + PlaceTtsContents từ Supabase, cache RAM
│   ├── LocationService.cs        — GPS (ForegroundService Android / polling iOS), LocationChanged event
│   ├── GeofenceEngine.cs         — Tìm POI gần nhất, debounce 2s, cooldown mặc định 30 phút
│   ├── NarrationService.cs       — TTS (TextToSpeech MAUI), PreferredLocale lưu Preferences
│   ├── UserProfileService.cs     — History/Notes lưu Preferences (local only)
│   ├── LocalizationService.cs    — Lưu & apply CultureInfo (vi/en), dùng Preferences
│   ├── AuthService.cs            — Chỉ lưu email local, không login thực sự
│   ├── POIService.cs             — Wrapper mỏng của PlaceService (dùng bởi TourDetailPage)
│   ├── FavoriteService.cs        — (legacy, chưa xóa)
│   └── WishlistService.cs        — (legacy, chưa xóa)
└── Data/Models/
    ├── Places.cs                 — Model chính, map bảng "Places" trên Supabase
    ├── PlaceImage.cs             — Bảng "PlaceImages"
    ├── PlaceTtsContent.cs        — Bảng "PlaceTtsContents" (TTS đa ngôn ngữ)
    ├── AccessSession.cs          — Bảng "AccessSessions" (subscription)
    ├── User.cs                   — Bảng "Users"
    ├── UserProfileModels.cs      — TripHistoryItem, PlaceNote (chỉ local)
    ├── Favorite.cs               — (DB model, ít dùng)
    └── Wishlist.cs               — (DB model, ít dùng)
```

---

## Quy tắc quan trọng — ĐỌC TRƯỚC KHI SỬA CODE

### 1. postgrest-csharp và `[Column]` attribute
**TUYỆT ĐỐI KHÔNG** thêm `[Column]` attribute cho field không có trong DB.
postgrest-csharp tự build câu SELECT từ `[Column]` → cột không tồn tại → lỗi 400 → danh sách trống.

```csharp
// ✅ ĐÚNG — field runtime không có [Column]
public DateTime? LastPlayedAt { get; set; }

// ❌ SAI — gây lỗi 400 nếu cột không có trong DB
[Column("last_played_at")]
public DateTime? LastPlayedAt { get; set; }
```

### 2. Navigation giữa các tab
Dùng Shell route tuyệt đối. Truyền data qua static property trước khi navigate:
```csharp
MapPage.PendingRoute = (lat, lon, name);
await Shell.Current.GoToAsync("//MainTabs/MapPage");
```

### 3. GPS handler — chỉ đăng ký 1 lần
`MapPage._gpsStarted` flag ngăn đăng ký handler trùng khi `OnAppearing()` gọi nhiều lần (mỗi lần vào tab). Không xóa flag này.

### 4. Dependency Injection
- Services: **Singleton** trong `MauiProgram.cs`
- Pages: **Transient**
- `PlaceDetailPage`, `TourDetailPage`: nhận services qua constructor
- `ToursPage`, `AccountPage`: resolve services từ `Handler?.MauiContext?.Services` trong `OnAppearing()`

### 5. Không có đăng nhập thực sự
`AuthService` chỉ lưu email local (Preferences). Không có JWT, không có Supabase session. Access control dùng `AccessSessionService`.

### 6. PlaceService cache
`GetCachedPlaces()` trả về list objects **đang được cache trong RAM**. `LastPlayedAt` set trên object này persist cho đến khi `GetAllPlacesAsync()` được gọi lại (reload từ Supabase tạo object mới). `GetAllPlacesAsync()` chỉ được gọi trong `OnAppearing()` của `MapPage` và `ToursPage`.

---

## Luồng khởi động app

```
App.CreateWindow()
  ├─ AccessSessionService.IsAccessValid() == true
  │    → new AppShell()          — vào thẳng 4-tab interface
  │    → AccessSessionService.StartExpiryTimer() — kiểm tra mỗi 60s
  └─ IsAccessValid() == false
       → LanguageSelectionPage   — chọn vi/en
           → SubscriptionPage    — chọn gói 1h/2h/1day/3day
               → PaymentQRPage   — hiện QR VietQR + polling 5s/lần
                   → admin kích hoạt IsActive=true trên Supabase
                   → polling detect → lưu ExpiresAt → new AppShell()

Mọi trường hợp (cả có và không có access):
  → RegisterDeviceAsync()     — upsert DeviceRegistrations (FirstSeenAt + LastSeenAt)
  → StartHeartbeatTimer()     — cập nhật LastSeenAt Supabase mỗi 5s khi foreground
```

App lifecycle:
- `OnSleep()` → `StopHeartbeat()` — dừng timer khi vào background/tắt
- `OnResume()` → `StartHeartbeatTimer()` — cập nhật ngay + chạy lại timer

**KHÔNG gọi `RegisterDeviceAsync()` trong `OnResume`** — sẽ overwrite `FirstSeenAt`.

Khi hết hạn: `AccessSessionService.AccessExpired` → `App.OnAccessExpired()` → quay về `LanguageSelectionPage`.

---

## Luồng GPS + Geofence + TTS (logic quan trọng)

```
Android ForegroundService → LocationForegroundService.LocationUpdated
  → LocationService.OnBackgroundLocationUpdated()
  → LocationService.LocationChanged event
  → MapPage GPS handler (chỉ 1 handler duy nhất nhờ _gpsStarted flag)
      → GeofenceEngine.FindNearestPOI()
          [lọc place trong bán kính (radius ?? 50m)]
          [lọc cooldown: (now - LastPlayedAt) >= (CooldownMinutes ?? 30) phút]
          [debounce 2s: phải liên tục detect 2s mới trả về]
          → trả về Place hoặc null
      → nearest != null && _lastSpokenPlaceId != nearest.PlaceId.ToString()
          → _lastSpokenPlaceId = nearestId
          → _lastSpokenPlace = nearest        (để check radius khi cooldown active)
          → nearest.LastPlayedAt = DateTime.Now
          → NarrationService.SpeakAsync(place.GetScriptForLocale(locale))
          → UserProfileService.AddHistoryByGpsAsync(nearest)
      → nearest == null
          → check: user masih dalam radius _lastSpokenPlace?
              → YA: giữ _lastSpokenPlaceId (cooldown đang active, KHÔNG reset)
              → KHÔNG: reset _lastSpokenPlaceId = null (user thực sự đã rời đi)
```

**Lý do quan trọng của `_lastSpokenPlace`**: Khi cooldown active, `nearest = null` nhưng user vẫn đứng trong bán kính. Nếu reset `_lastSpokenPlaceId = null` lúc này, sau khi cooldown hết TTS sẽ phát lại. Fix: chỉ reset khi `distToLast > (radius ?? 50)`.

---

## Luồng Tap POI trên Map → Place Card

```
MyMap.Info event → OnMapInfo()
  → lấy feature["id"] từ layer "POIs"
  → PlaceService.GetCachedPlaces().FirstOrDefault(p => p.PlaceId == id)
  → ShowPlaceCard(place)       — hiện bottom card (overlay trong Grid)
      BtnDirections → ShowRouteToDestinationAsync(OSRM)
      BtnNarrate    → NarrationService.SpeakAsync
      BtnCall       → PhoneDialer.Open
      BtnDetail     → Navigation.PushAsync(new PlaceDetailPage(...))
```

Hover effect dùng `PointerGestureRecognizer` + `ReferenceEquals(btn, BtnDirections)` (không dùng Name).

---

## Luồng chỉ đường (OSRM)

```
OnCardDirections hoặc OnDirectionsClicked (PlaceDetailPage)
  → MapPage.PendingRoute = (lat, lon, name)
  → Shell.GoToAsync("//MainTabs/MapPage")
  → MapPage.OnAppearing() → đọc PendingRoute → ShowRouteToDestinationAsync()
      → OSRM API: /route/v1/driving/{origin};{dest}?overview=full&geometries=geojson
      → Vẽ LineString layer "Route" (#E94560)
      → Vẽ PointFeature layer "Destination"
      → Zoom to route bounds
      → Hiện CancelRoutePanel
```

Khi `CancelRoutePanel.IsVisible == true`, GPS callback return early (không ghi đè label).

---

## Luồng QR Check-in

```
QRScanPage.OnBarcodesDetected()
  → parse value: nếu là số → tìm Place theo PlaceId
      → UserProfileService.AddHistoryByQRAsync(place)
      → NarrationService.SpeakAsync(text) — đọc raw text QR
  → nếu không phải số → thông báo "raw text only"
  → delay 3s → tiếp tục scan
```

---

## Luồng Tour

```
ToursPage.OnAppearing()
  → PlaceService.GetAllPlacesAsync() (lọc IsActive && IsApproved)
  → RebuildTours() — tạo 3 TourCard từ cùng 1 pool places:
      tour-quick    (2 stops)
      tour-balanced (3 stops)
      tour-full     (4 stops)
  Filters: query text, budget slider (k), duration picker, low-walking toggle

OnSelectTourClicked → TourDetailPage(tour, services...)
  → OnStartClicked → MapPage.PendingRoute = first stop → GoToAsync MapPage
  → OnStopDirectionsClicked → MapPage.PendingRoute = stop → GoToAsync MapPage
```

---

## Database — Bảng và cột đang dùng

### Bảng `Places`

| Column | Type | Ghi chú |
|---|---|---|
| PlaceId | int | PK |
| Name | string | |
| Description | string? | |
| Address | string? | |
| Latitude / Longitude | double | Tọa độ WGS84 |
| Phone | string? | |
| Website | string? | |
| OpenTime / CloseTime | string? | Format "HH:mm" |
| PriceMin / PriceMax | decimal? | VND |
| AverageRating | float? | 0–5 |
| TotalReviews | int? | |
| Specialty | string? | Tags, phân cách dấu phẩy |
| IsActive / IsApproved | bool | ToursPage filter cả hai; MapPage/MainPage không filter |
| tts_script | string? | **Legacy fallback** — ưu tiên dùng PlaceTtsContents |
| radius | double? | Bán kính geofence (mét), mặc định 50 |
| cooldown_minutes | int? | Cooldown TTS, mặc định 30 phút |
| priority | int? | Ưu tiên khi nhiều POI cùng vùng, mặc định 1 |
| CategoryId | int? | Dùng bên web, mobile không đọc |
| OwnerId | int? | Dùng bên web, mobile không đọc |

**Không cần thiết cho mobile** (có thể để trống hoặc xóa khỏi DB nếu web không dùng):
`PricePerPerson`, `District`, `HasParking`, `HasAircon`, `audio_file_url`

### Bảng `PlaceImages`

| Column | Type | Ghi chú |
|---|---|---|
| ImageId | int | PK |
| PlaceId | int | FK → Places |
| ImageUrl | string | |
| IsMain | bool | PlaceService chỉ load ảnh có IsMain=true |

### Bảng `PlaceTtsContents`

| Column | Type | Ghi chú |
|---|---|---|
| Id | int | PK |
| PlaceId | int | FK → Places |
| Locale | string | "vi-VN", "en-US", v.v. |
| Script | string | Nội dung TTS |

Được load vào `Place.TtsContents` dictionary: `{ "vi-VN": "...", "en-US": "..." }`

**Priority trong `GetScriptForLocale(locale)`**:
1. `TtsContents[locale]` (ngôn ngữ user chọn)
2. `TtsContents["vi-VN"]` (fallback tiếng Việt)
3. `TtsScript` (legacy field trong Places)
4. `"Đây là địa điểm {Name}."` (mặc định cuối cùng)

### Bảng `AccessSessions`

| Column | Type | Ghi chú |
|---|---|---|
| SessionId | string | PK (UUID) |
| DeviceId | string | 10 ký tự uppercase, lưu Preferences |
| PackageId | string | "1h" / "2h" / "1day" / "3day" |
| DurationHours | double | |
| PriceVnd | int | |
| IsActive | bool | Admin set = true sau khi xác nhận thanh toán |
| CreatedAt / ActivatedAt / ExpiresAt | DateTime? | |

---

## Services — Logic quan trọng

### AccessSessionService
- `GetDeviceId()`: tạo 10-char uppercase ID lưu Preferences, bền vĩnh
- `IsAccessValid()`: check `access_expires_at` Preference so với `DateTime.UtcNow`
- `CreatePendingSessionAsync()`: insert vào Supabase với `IsActive=false`
- `StartPollingForActivation()`: poll Supabase mỗi 5s, detect `IsActive=true` → lưu `ExpiresAt` local
- `StartExpiryTimer()`: check mỗi 60s, khi hết hạn fire `AccessExpired` event
- `StartHeartbeatTimer()`: cập nhật `LastSeenAt` lên Supabase ngay lập tức rồi mỗi 5s, chạy khi foreground
- `StopHeartbeat()`: hủy heartbeat (gọi từ `App.OnSleep`)
- `UpdateLastSeenAsync()` (private): chỉ update `LastSeenAt`, KHÔNG đụng `FirstSeenAt`
- `ClearLocalSession()`: xóa `access_expires_at`, `access_session_id`, cancel cả heartbeat

### PlaceService
- `GetAllPlacesAsync()`: load Places → PlaceImages (IsMain=true) → PlaceTtsContents theo thứ tự, lỗi bước nào bỏ qua bước đó
- `GetCachedPlaces()`: trả về `_cachedPlaces` list hiện tại (cùng object references)
- `LastPlayedAt` trên Place objects tồn tại trong RAM cho đến khi `GetAllPlacesAsync()` tạo objects mới

### GeofenceEngine
- `FindNearestPOI()`: lọc candidates (trong radius + qua cooldown) → sort → debounce 2s → trả về top
- Sort order: `Priority DESC` → `Distance ASC` → `PlaceId ASC` (tiebreaker deterministic khi bằng nhau)
- Sau khi debounce satisfied: KHÔNG reset `_pendingPlaceId` (để handler thứ 2 không restart debounce)
- Khi candidates rỗng (không có POI hoặc tất cả trong cooldown): reset debounce
- `GetDistance()`: Haversine formula, trả về mét

### NarrationService
- `SpeakAsync()`: guard `_isSpeaking` tránh gọi đồng thời
- `PreferredLocale`: lưu/đọc Preferences key `"tts_preferred_locale"`, mặc định `"vi-VN"`
- Tìm locale khớp trên thiết bị: exact match (lang+country) → fallback match (lang only)

### LocationService
- Android: `LocationForegroundService` (Android Foreground Service) — GPS chạy khi app background
- iOS: polling loop mỗi 3s
- `GetAddressAsync()`: reverse geocode với cache 15s / 30m tránh spam API

### UserProfileService
- Tất cả data lưu **local** bằng `Preferences` (JSON serialized)
- `AddHistoryAsync()`: prepend vào list, giữ tối đa 100 mục
- `AddHistoryByGpsAsync(place)` → `AddHistoryAsync(place, "GPS")`
- `AddHistoryByQRAsync(place)` → `AddHistoryAsync(place, "QR Code")`
- Không sync lên Supabase (chỉ local device)

---

## MapPage — Các flag quan trọng

```csharp
private bool _followUserLocation = true;   // false khi user kéo map thủ công
private bool _programmaticNav    = false;  // true khi code đang animate map
private bool _gpsStarted         = false;  // QUAN TRỌNG: chỉ đăng ký 1 LocationChanged handler
private string? _lastSpokenPlaceId;        // PlaceId của POI vừa đọc TTS
private Place?  _lastSpokenPlace;          // Ref object để check radius khi cooldown active
```

**Map layers** (theo thứ tự add):
- `"BaseMap"` — CartoDB Voyager tiles
- `"POIsGlow"` — vòng glow đỏ mờ, visual only
- `"POIs"` — chấm đỏ đặc, dùng cho hit-test (`OnMapInfo`)
- `"UserLocationGlow"` — vòng glow xanh mờ
- `"UserLocationDot"` — chấm xanh đặc
- `"Route"` — LineString OSRM (#E94560)
- `"Destination"` — marker điểm đến

---

## AccountPage — Các chức năng đã implement

- **Lịch sử**: đọc từ `UserProfileService`, hiển thị tối đa 30 mục với icon theo `VisitMethod`
- **Chọn ngôn ngữ TTS**: Picker → `NarrationService.PreferredLocale`
- **Xóa lịch sử**: `UserProfileService.ClearHistoryAsync()`
- **Dev: Deactivate** (ẩn trong production): `AccessSessionService.ClearLocalSession()` → về `LanguageSelectionPage`

---

## Style & UI Conventions

### Bảng màu (dark gold theme)

| Tên | Hex | Dùng cho |
|---|---|---|
| Background | `#0F0E0D` | Nền trang |
| Surface | `#1A1410` | Card, header, bottom panel |
| Surface2 | `#26201A` | Icon bg, chip, button phụ |
| Border | `#2A2018` | Viền card |
| Border2 | `#3A2D22` | Viền button |
| Primary text | `#F0E6D3` | Tên, tiêu đề |
| Secondary text | `#5A4A3A` | Mô tả, subtitle |
| Gold accent | `#C8A96E` | Nút chính, label accent, sao |
| Red accent | `#E94560` | Status đóng cửa, marker POI, route line |
| Blue | `#1E90FF` | Marker vị trí người dùng |
| Green | `#4CAF50` | Status đang mở |

### Section headers
```xml
<Label Text="— TÊN SECTION"
       FontSize="9" CharacterSpacing="3"
       TextColor="#C8A96E" FontAttributes="Bold"/>
```

---

## Các quyết định kỹ thuật đã chốt

- **`_lastSpokenPlace`**: giữ reference để check radius khi `nearest=null` do cooldown, tránh reset `_lastSpokenPlaceId` sai thời điểm
- **`_gpsStarted`**: flag trong MapPage ngăn đăng ký nhiều LocationChanged handlers (bug cũ: mỗi tab switch thêm 1 handler → nhiều TTS đồng thời)
- **GeofenceEngine không reset sau debounce**: sau khi trả về POI, không reset `_pendingPlaceId` (tránh handler thứ 2 restart debounce)
- **Cooldown mặc định 30 phút**: `CooldownMinutes ?? 30` — DB đang để null toàn bộ, app dùng 30p
- **POIService** giữ lại như wrapper của PlaceService để không phá `TourDetailPage`
- **PlaceCard** overlay lên bottom panel của MapPage (cùng Grid, `VerticalOptions="End"`)
- **Hover effect** trên card buttons: `PointerGestureRecognizer` + `ReferenceEquals` (không dùng `Border.Name`)
- **`tts_script`** trong Places là legacy fallback — nguồn chính là `PlaceTtsContents`
- **`PlaceDetailPage.TtsScriptLabel`**: dùng `GetScriptForLocale()` (không phải `_place.TtsScript` trực tiếp)

---

## Chưa implement / biết để tránh nhầm

- `FavoriteService.cs`, `WishlistService.cs` — legacy, chưa xóa, không dùng
- Yêu thích / Wishlist — chưa có UI trong app
- Bán kính geofence chưa có UI điều chỉnh (fix cứng 50m hoặc DB value)
- **Dev bypass** trong `SubscriptionPage` và **Dev deactivate** trong `AccountPage` — xóa trước khi release
- `TtsLocale` field trong `Places.cs` — không có `[Column]` vì cột `tts_locale` chưa có trong DB
