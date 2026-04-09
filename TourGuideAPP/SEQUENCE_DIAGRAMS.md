# Sequence Diagrams - TourGuideAPP

## 1. Mở app và kiểm tra session

```mermaid
sequenceDiagram
    actor User
    participant App
    participant Preferences
    participant Timer
    participant SubscriptionPage
    participant MainShell as Main App

    User->>App: Mở ứng dụng
    App->>Preferences: Đọc DeviceId, SessionId, ExpiresAt
    Preferences-->>App: Trả dữ liệu session

    alt Session tồn tại và còn hạn
        App->>MainShell: Điều hướng vào app chính
        App->>Timer: Khởi động timer kiểm tra hết hạn
    else Không có session hoặc đã hết hạn
        App->>SubscriptionPage: Điều hướng tới trang chọn gói
    end
```

## 2. Chọn gói và tạo session chờ thanh toán

```mermaid
sequenceDiagram
    actor User
    participant SubscriptionPage
    participant AccessSessionService
    participant Preferences
    participant PaymentQRPage

    User->>SubscriptionPage: Chọn gói sử dụng
    SubscriptionPage->>Preferences: Lấy DeviceId

    alt Chưa có DeviceId
        Preferences-->>SubscriptionPage: Không có dữ liệu
        SubscriptionPage->>Preferences: Tạo và lưu DeviceId mới
    else Đã có DeviceId
        Preferences-->>SubscriptionPage: Trả DeviceId
    end

    SubscriptionPage->>AccessSessionService: Tạo session chờ kích hoạt
    AccessSessionService-->>SubscriptionPage: Trả SessionId và thông tin gói
    SubscriptionPage->>PaymentQRPage: Điều hướng sang trang QR
```

## 3. Thanh toán QR và polling kích hoạt

```mermaid
sequenceDiagram
    actor User
    participant PaymentQRPage
    participant AccessSessionService
    participant Supabase
    actor Admin
    participant Preferences
    participant MainShell as Main App

    User->>PaymentQRPage: Xem QR và chuyển khoản
    loop Mỗi 5 giây
        PaymentQRPage->>AccessSessionService: Kiểm tra session đã active chưa
        AccessSessionService->>Supabase: Query AccessSessions
        Supabase-->>AccessSessionService: Trả trạng thái session
        AccessSessionService-->>PaymentQRPage: Session chưa active / đã active
    end

    Admin->>Supabase: Chạy activate_session(DeviceId)
    Supabase-->>Admin: Session được kích hoạt

    PaymentQRPage->>AccessSessionService: Polling lần kế tiếp
    AccessSessionService->>Supabase: Query session
    Supabase-->>AccessSessionService: IsActive = true, ExpiresAt
    AccessSessionService-->>PaymentQRPage: Session hợp lệ
    PaymentQRPage->>Preferences: Lưu SessionId, ExpiresAt, PackageId
    PaymentQRPage->>MainShell: Điều hướng vào app chính
```

## 4. Timer kiểm tra hết hạn gói

```mermaid
sequenceDiagram
    participant Timer
    participant Preferences
    participant App
    participant SubscriptionPage
    actor User

    loop Mỗi 60 giây
        Timer->>Preferences: Đọc ExpiresAt
        Preferences-->>Timer: Trả ExpiresAt
        Timer->>App: So sánh với thời gian hiện tại

        alt Session còn hạn
            App-->>Timer: Tiếp tục chạy
        else Session hết hạn
            App->>User: Hiển thị alert hết hạn
            App->>SubscriptionPage: Điều hướng về trang chọn gói
        end
    end
```

## 5. Tải danh sách Places và cache

```mermaid
sequenceDiagram
    participant MainPage
    participant PlaceService
    participant MemoryCache as In-memory Cache
    participant Supabase

    MainPage->>PlaceService: Yêu cầu danh sách Places
    PlaceService->>MemoryCache: Kiểm tra cache

    alt Có cache
        MemoryCache-->>PlaceService: Trả danh sách Places
        PlaceService-->>MainPage: Trả dữ liệu
    else Chưa có cache
        PlaceService->>Supabase: Lấy Places active/approved
        Supabase-->>PlaceService: Trả dữ liệu Places
        PlaceService->>MemoryCache: Lưu cache
        PlaceService-->>MainPage: Trả dữ liệu
    end
```

## 6. Tìm kiếm và lọc địa điểm

```mermaid
sequenceDiagram
    actor User
    participant MainPage
    participant PlaceService

    User->>MainPage: Nhập từ khóa / chọn danh mục
    MainPage->>PlaceService: Lọc theo tên, địa chỉ, mô tả, tags
    PlaceService-->>MainPage: Trả danh sách đã lọc
    MainPage-->>User: Hiển thị kết quả realtime
```

## 7. Mở chi tiết địa điểm

```mermaid
sequenceDiagram
    actor User
    participant MainPage
    participant PlaceDetailPage
    participant PlaceService
    participant Supabase

    User->>MainPage: Chạm vào card địa điểm
    MainPage->>PlaceDetailPage: Điều hướng với PlaceId
    PlaceDetailPage->>PlaceService: Lấy chi tiết địa điểm
    PlaceService->>Supabase: Query Places + PlaceImages
    Supabase-->>PlaceService: Trả dữ liệu chi tiết
    PlaceService-->>PlaceDetailPage: Trả model địa điểm
    PlaceDetailPage-->>User: Hiển thị gallery và thông tin
```

## 8. Hiển thị bản đồ và marker

```mermaid
sequenceDiagram
    actor User
    participant MapPage
    participant PlaceService
    participant LocationService
    participant Mapsui

    User->>MapPage: Mở tab bản đồ
    MapPage->>PlaceService: Lấy danh sách POI
    PlaceService-->>MapPage: Trả Places
    MapPage->>LocationService: Lấy vị trí hiện tại
    LocationService-->>MapPage: Trả tọa độ
    MapPage->>Mapsui: Render POI markers + user marker
    Mapsui-->>User: Hiển thị bản đồ
```

## 9. Chạm marker và mở bottom card

```mermaid
sequenceDiagram
    actor User
    participant MapPage

    User->>MapPage: Chạm vào marker POI
    MapPage->>MapPage: Xác định PlaceId từ marker
    MapPage->>MapPage: Nạp dữ liệu địa điểm tương ứng
    MapPage-->>User: Hiển thị bottom card với hành động
```

## 10. Chỉ đường từ bản đồ

```mermaid
sequenceDiagram
    actor User
    participant MapPage
    participant LocationService
    participant RoutingService
    participant OSRM

    User->>MapPage: Bấm "Chỉ đường"
    MapPage->>LocationService: Lấy vị trí hiện tại
    LocationService-->>MapPage: Trả origin
    MapPage->>RoutingService: Tính route tới destination
    RoutingService->>OSRM: Gửi origin + destination
    OSRM-->>RoutingService: Trả polyline
    RoutingService-->>MapPage: Trả route
    MapPage-->>User: Vẽ đường đi trên bản đồ
```

## 11. Chỉ đường từ PlaceDetailPage

```mermaid
sequenceDiagram
    actor User
    participant PlaceDetailPage
    participant MapPage
    participant RoutingService
    participant OSRM

    User->>PlaceDetailPage: Bấm "Chỉ đường"
    PlaceDetailPage->>MapPage: Set PendingRoute và chuyển tab
    MapPage->>RoutingService: Tính route tới PlaceId đã chọn
    RoutingService->>OSRM: Gửi thông tin route
    OSRM-->>RoutingService: Trả polyline
    RoutingService-->>MapPage: Trả route
    MapPage-->>User: Hiển thị đường đi
```

## 12. Phát thuyết minh thủ công

```mermaid
sequenceDiagram
    actor User
    participant PlaceDetailPage
    participant NarrationService
    participant TTS as TextToSpeech

    User->>PlaceDetailPage: Bấm "Thuyết minh"
    PlaceDetailPage->>NarrationService: SpeakAsync(tts_script, tts_locale)
    NarrationService->>TTS: Phát nội dung
    TTS-->>User: Phát âm thanh thuyết minh
```

## 13. GPS realtime và geofence tự động

```mermaid
sequenceDiagram
    participant OS as Hệ điều hành
    participant LocationService
    participant GeofenceEngine
    participant MapPage
    participant NarrationService
    actor User

    OS->>LocationService: Trả vị trí mới
    LocationService->>MapPage: Phát sự kiện LocationChanged
    LocationService->>GeofenceEngine: Gửi tọa độ hiện tại
    GeofenceEngine->>GeofenceEngine: Lọc POI có TTS, trong bán kính, chưa cooldown
    GeofenceEngine->>GeofenceEngine: Sắp xếp theo priority và khoảng cách
    GeofenceEngine->>GeofenceEngine: Debounce 2 giây

    alt Có POI phù hợp
        GeofenceEngine->>NarrationService: SpeakAsync cho POI ưu tiên nhất
        NarrationService-->>User: Phát thuyết minh tự động
    else Không có POI phù hợp
        GeofenceEngine-->>LocationService: Không trigger
    end
```

## 14. Tránh đọc lặp lại cùng một POI

```mermaid
sequenceDiagram
    participant GeofenceEngine
    participant NarrationService
    participant POIState as POI Cooldown State

    GeofenceEngine->>POIState: Kiểm tra LastPlayedAt và cooldown

    alt Chưa hết cooldown
        POIState-->>GeofenceEngine: Bỏ qua POI
    else Đã hết cooldown
        POIState-->>GeofenceEngine: Cho phép trigger
        GeofenceEngine->>NarrationService: SpeakAsync
        NarrationService->>POIState: Cập nhật LastPlayedAt
    end
```

## 15. Quét QR để mở PlaceDetail

```mermaid
sequenceDiagram
    actor User
    participant QRScanPage
    participant Camera
    participant ZXing
    participant PlaceDetailPage

    User->>QRScanPage: Mở màn hình quét QR
    QRScanPage->>Camera: Yêu cầu mở camera
    Camera-->>QRScanPage: Camera sẵn sàng
    User->>Camera: Đưa mã QR vào khung quét
    Camera->>ZXing: Gửi ảnh khung hình
    ZXing-->>QRScanPage: Trả nội dung QR

    alt QR hợp lệ chứa PlaceId
        QRScanPage->>PlaceDetailPage: Điều hướng tới địa điểm tương ứng
    else QR không hợp lệ
        QRScanPage-->>User: Hiển thị lỗi mã QR
    end
```

## 16. Xem tour và mở điểm dừng

```mermaid
sequenceDiagram
    actor User
    participant ToursPage
    participant TourDetailPage
    participant TourService
    participant PlaceDetailPage

    User->>ToursPage: Chọn một tour
    ToursPage->>TourDetailPage: Điều hướng sang chi tiết tour
    TourDetailPage->>TourService: Lấy danh sách điểm dừng
    TourService-->>TourDetailPage: Trả dữ liệu tour
    TourDetailPage-->>User: Hiển thị các điểm dừng
    User->>TourDetailPage: Chọn một điểm dừng
    TourDetailPage->>PlaceDetailPage: Mở chi tiết địa điểm
```

## 17. Admin kích hoạt session

```mermaid
sequenceDiagram
    actor Admin
    participant BankTransfer as Giao dịch ngân hàng
    participant Supabase
    participant AccessSessions

    Admin->>BankTransfer: Kiểm tra nội dung chuyển khoản
    BankTransfer-->>Admin: DeviceId + số tiền
    Admin->>Supabase: SELECT activate_session(DeviceId)
    Supabase->>AccessSessions: Tìm session chờ kích hoạt
    AccessSessions-->>Supabase: Trả session phù hợp
    Supabase->>AccessSessions: Update IsActive, ActivatedAt, ExpiresAt
    Supabase-->>Admin: Kích hoạt thành công
```

## 18. Mất mạng trong khi dùng app

```mermaid
sequenceDiagram
    actor User
    participant App
    participant PlaceService
    participant RoutingService
    participant Supabase
    participant OSRM

    User->>App: Tiếp tục thao tác khi mất mạng

    alt Gọi Places
        App->>PlaceService: Yêu cầu dữ liệu
        PlaceService->>Supabase: Request
        Supabase--xPlaceService: Timeout / network error
        PlaceService-->>App: Trả lỗi có kiểm soát hoặc cache cũ
    else Gọi route
        App->>RoutingService: Yêu cầu chỉ đường
        RoutingService->>OSRM: Request route
        OSRM--xRoutingService: Network error
        RoutingService-->>App: Trả lỗi có kiểm soát
    end

    App-->>User: Hiển thị thông báo, không crash
```

## 19. Từ chối quyền vị trí

```mermaid
sequenceDiagram
    actor User
    participant App
    participant OS as Hệ điều hành

    App->>OS: Yêu cầu quyền vị trí
    User->>OS: Từ chối quyền
    OS-->>App: Permission denied
    App-->>User: Hiển thị giải thích và hướng dẫn mở Settings
```

## Gợi ý dùng cho báo cáo

- Nếu báo cáo cần ngắn gọn, nên chọn 8 biểu đồ chính:
  - Mở app kiểm tra session
  - Thanh toán QR và polling
  - Tải Places và cache
  - Bản đồ và marker
  - Chỉ đường OSRM
  - Geofence tự động phát TTS
  - Quét QR
  - Admin kích hoạt session
- Nếu bạn cần, tôi có thể chuyển tiếp file này thành:
  - bản PlantUML
  - bản rút gọn đúng chuẩn UML cho báo cáo
  - hoặc bản có numbering actor/boundary/control/entity
