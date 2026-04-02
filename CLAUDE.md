# CLAUDE.md — TourGuideAPP

Đây là file hướng dẫn cho AI assistant. Đọc file này trước khi làm bất cứ thứ gì.

---

## Tổng quan dự án

**TourGuideAPP** — ứng dụng hướng dẫn du lịch TP.HCM, xây dựng bằng .NET MAUI.
- Backend: Supabase (PostgreSQL + postgrest-csharp, không dùng Supabase Auth)
- Bản đồ: Mapsui (OpenStreetMap tiles)
- Định tuyến: OSRM API
- Barcode: ZXing.Net.MAUI
- Target: Android (chính), iOS

---

## Cấu trúc thư mục

```
TourGuideAPP/
├── Views/
│   ├── MapPage.xaml/.cs          — Tab bản đồ, POI markers, GPS, chỉ đường
│   ├── MainPage.xaml/.cs         — Tab nổi bật, danh sách địa điểm, search/filter
│   ├── ToursPage.xaml/.cs        — Tab tour, tạo tour tự động từ Places
│   ├── TourDetailPage.xaml/.cs   — Chi tiết tour, danh sách điểm dừng
│   ├── AccountPage.xaml/.cs      — Tab cài đặt (chưa có chức năng thực)
│   ├── PlaceDetailPage.xaml/.cs  — Chi tiết địa điểm (push từ MainPage/MapPage)
│   └── QRScanPage.xaml/.cs       — Quét QR check-in
├── Services/
│   ├── PlaceService.cs           — Load Places từ Supabase, cache trong bộ nhớ
│   ├── POIService.cs             — Wrapper mỏng của PlaceService (dùng bởi TourDetailPage)
│   ├── LocationService.cs        — GPS, LocationChanged event
│   ├── GeofenceEngine.cs         — Phát hiện POI gần nhất theo GPS
│   ├── NarrationService.cs       — TTS (TextToSpeech MAUI), mặc định locale "vi-VN"
│   ├── AuthService.cs            — Không dùng login, chỉ lưu email local
│   ├── UserProfileService.cs     — Favorites/Wishlist/History/Notes lưu bằng Preferences
│   ├── FavoriteService.cs        — (legacy, UserProfileService là chính)
│   └── WishlistService.cs        — (legacy)
└── Data/Models/
    ├── Places.cs                 — Model chính, map bảng "Places" trên Supabase
    ├── PlaceImage.cs             — Ảnh địa điểm, bảng "PlaceImages"
    ├── User.cs                   — Bảng "Users"
    ├── UserProfileModels.cs      — FavoritePlace, WishlistPlace, TripHistoryItem, PlaceNote
    ├── Favorite.cs               — (DB model, ít dùng)
    └── Wishlist.cs               — (DB model, ít dùng)
```

---

## Quy tắc quan trọng — ĐỌC TRƯỚC KHI SỬA CODE

### 1. postgrest-csharp và `[Column]` attribute
**TUYỆT ĐỐI KHÔNG** thêm `[Column]` attribute cho các field không có trong DB.
postgrest-csharp tự build câu SELECT dựa trên `[Column]` attributes → nếu thêm cột không tồn tại → lỗi 400 → danh sách trống.

```csharp
// ✅ ĐÚNG — field runtime không có [Column]
public string? TtsScript { get; set; }

// ❌ SAI — sẽ gây lỗi 400 nếu cột không có trong DB
[Column("tts_script")]
public string? TtsScript { get; set; }
```

### 2. Navigation giữa các tab
Dùng Shell navigation với route tuyệt đối:
```csharp
await Shell.Current.GoToAsync("//MainTabs/MapPage");
```
Truyền destination sang MapPage qua static property:
```csharp
MapPage.PendingRoute = (lat, lon, name);
await Shell.Current.GoToAsync("//MainTabs/MapPage");
```

### 3. MapPage — follow user location
Hai flag quan trọng để phân biệt user kéo map vs code zoom:
```csharp
private bool _followUserLocation = true;   // false khi user kéo map
private bool _programmaticNav = false;     // true trong khi code đang navigate
```
GPS callback chỉ center map nếu `_followUserLocation == true`.
Khi đang chỉ đường (`CancelRoutePanel.IsVisible`), GPS callback return early.

### 4. Dependency Injection
Tất cả Services đăng ký là **Singleton** trong `MauiProgram.cs`.
Pages đăng ký là **Transient**.
`PlaceDetailPage` và `TourDetailPage` nhận services qua constructor (không dùng DI container trực tiếp).

### 5. Không có đăng nhập
App đã bỏ yêu cầu đăng nhập. Tất cả chức năng dùng được ngay.
`AuthService` vẫn tồn tại nhưng chỉ để lưu email local.

---

## Database — Bảng chính

### Bảng `Places`
Bảng duy nhất cho địa điểm (đã gộp với bảng `pois` cũ).

| Column | Type | Ghi chú |
|---|---|---|
| PlaceId | int | PK |
| Name | string | Tên địa điểm |
| Address | string? | Địa chỉ |
| Latitude / Longitude | double | Tọa độ |
| Phone | string? | Số điện thoại |
| OpenTime / CloseTime | string? | Định dạng "HH:mm" |
| PriceMin / PriceMax | decimal? | VND |
| AverageRating | float? | 0–5 |
| TotalReviews | int? | |
| Specialty | string? | Tags, phân cách bằng dấu phẩy |
| IsActive / IsApproved | bool | Chỉ hiện nếu cả hai đều true |
| TtsScript | string? | Script đọc TTS khi geofence |

### Bảng `PlaceImages`
| Column | Type |
|---|---|
| ImageId | int |
| PlaceId | int |
| ImageUrl | string |

---

## Luồng hoạt động chính

### GPS + Geofence + TTS
```
LocationService.LocationChanged
  → GeofenceEngine.FindNearestPOI()       — tìm Place có TtsScript trong bán kính
  → NarrationService.SpeakAsync(ttsScript) — đọc TTS
  → UserProfileService.AddHistoryByGpsAsync()
```

### Tap POI trên Map → Bottom Card
```
MapPage.OnMapInfo()
  → lấy id từ feature["id"]
  → PlaceService.GetCachedPlaces().FirstOrDefault(id)
  → ShowPlaceCard(place)                  — hiện card Google Maps style
    → OnCardDirections → ShowRouteToDestinationAsync (OSRM)
    → OnCardNarrate   → NarrationService.SpeakAsync
    → OnCardCall      → PhoneDialer.Open
    → OnCardDetail    → Navigation.PushAsync(PlaceDetailPage)
```

### Chỉ đường từ PlaceDetailPage
```
OnDirectionsClicked
  → MapPage.PendingRoute = (lat, lon, name)
  → Shell.GoToAsync("//MainTabs/MapPage")
  → MapPage.OnAppearing() → đọc PendingRoute → ShowRouteToDestinationAsync
```

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
| Red accent | `#E94560` | Status đóng cửa, marker POI |
| Blue | `#1E90FF` | Marker vị trí người dùng |
| Green | `#4CAF50` | Status đang mở |

### Marker trên bản đồ
- **Vị trí người dùng**: 2 lớp — glow xanh mờ (`#2D8CFF` alpha 45) + chấm đặc (`#1E90FF`) viền trắng
- **POI**: 2 lớp — glow đỏ mờ (`#E94560` alpha 45) + chấm đặc (`#E94560`) viền trắng
- Layer `"POIsGlow"` (visual only) + `"POIs"` (hit-test)

### Section headers
```xml
<Label Text="— TÊN SECTION"
       FontSize="9" CharacterSpacing="3"
       TextColor="#C8A96E" FontAttributes="Bold"/>
```

---

## Các quyết định kỹ thuật đã chốt

- **POIService** giữ lại như wrapper của PlaceService để không phá `TourDetailPage`
- **`_lastSpokenPlaceId`** dùng `place.PlaceId.ToString()` (không phải int)
- **PlaceCard** overlay lên bottom panel của MapPage (cùng Grid, `VerticalOptions="End"`)
- **Hover effect** trên card buttons: `PointerGestureRecognizer` + `ReferenceEquals` (không dùng `Border.Name`)
- **`TourDetailPage`** nhận services qua constructor, không resolve từ DI trong page

---

## Chưa implement (biết để tránh nhầm)

- `AccountPage` — các switch/row chưa có handler thực, chỉ là UI
- `FavoriteService.cs`, `WishlistService.cs` — legacy, chưa xóa
- Lịch sử / Yêu thích / Wishlist trong AccountPage chưa hiển thị
- Ngôn ngữ TTS chưa có UI chọn locale
- Bán kính geofence chưa có UI điều chỉnh
