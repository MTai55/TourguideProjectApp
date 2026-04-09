# Sequence Cases - TourGuideAPP

## 1. Mở app và kiểm tra quyền truy cập gói

### Mục tiêu
Xác định người dùng còn phiên sử dụng hợp lệ hay phải quay về màn hình đăng ký gói.

### Tác nhân
- User
- App
- Preferences
- AccessSessionService

### Luồng chính
1. User mở ứng dụng.
2. App khởi tạo `AppShell` hoặc màn hình khởi động.
3. App đọc `ExpiresAt`, `SessionId`, `DeviceId` từ `Preferences`.
4. App kiểm tra dữ liệu phiên có tồn tại hay không.
5. App so sánh `DateTime.UtcNow` với `ExpiresAt`.
6. Nếu phiên còn hạn, App điều hướng vào màn hình chính.
7. App khởi động timer kiểm tra hạn dùng định kỳ.

### Luồng thay thế
1. Nếu không có dữ liệu phiên trong `Preferences`, App chuyển đến `SubscriptionPage`.
2. Nếu có dữ liệu nhưng `ExpiresAt` đã hết hạn, App xóa trạng thái phiên cũ và chuyển đến `SubscriptionPage`.

### Luồng lỗi
1. Nếu đọc `Preferences` lỗi, App coi như chưa có phiên hợp lệ.
2. App hiển thị thông báo ngắn và chuyển đến `SubscriptionPage`.

---

## 2. Chọn gói sử dụng

### Mục tiêu
Cho người dùng chọn gói thời gian trước khi thanh toán.

### Tác nhân
- User
- SubscriptionPage
- AccessSessionService

### Luồng chính
1. User mở `SubscriptionPage`.
2. App hiển thị danh sách gói: `1h`, `2h`, `1day`, `3day`.
3. User chọn một gói.
4. App tạo session chờ kích hoạt qua `AccessSessionService`.
5. App sinh hoặc đọc `DeviceId`.
6. App điều hướng sang `PaymentQRPage` kèm thông tin gói và session.

### Luồng thay thế
1. Nếu `DeviceId` chưa tồn tại, App tạo mới và lưu vào `Preferences`.
2. Nếu User đổi gói, App cập nhật thông tin gói được chọn trước khi sang trang thanh toán.

### Luồng lỗi
1. Nếu tạo session thất bại, App hiển thị lỗi và cho phép thử lại.

---

## 3. Thanh toán qua QR và chờ kích hoạt

### Mục tiêu
Hiển thị QR thanh toán và liên tục kiểm tra trạng thái kích hoạt.

### Tác nhân
- User
- PaymentQRPage
- AccessSessionService
- Supabase
- Admin

### Luồng chính
1. `PaymentQRPage` nhận thông tin gói và `DeviceId`.
2. App hiển thị QR VietQR, số tiền và nội dung chuyển khoản.
3. User dùng app ngân hàng để thanh toán.
4. `PaymentQRPage` khởi động polling mỗi 5 giây.
5. App gọi `AccessSessionService` để kiểm tra session theo `DeviceId` hoặc `SessionId`.
6. Admin xác nhận thanh toán và kích hoạt session trên Supabase.
7. Supabase cập nhật `IsActive = true`, `ActivatedAt`, `ExpiresAt`.
8. Ở lần polling kế tiếp, App nhận được session đã kích hoạt.
9. App lưu `ExpiresAt`, `SessionId`, `PackageId` vào `Preferences`.
10. App điều hướng vào màn hình chính.

### Luồng thay thế
1. Nếu Admin chưa kích hoạt, App tiếp tục polling.
2. Nếu User thoát trang rồi quay lại, App tiếp tục đọc session đang chờ và polling lại.

### Luồng lỗi
1. Nếu mất mạng, App hiển thị trạng thái `Đang kết nối lại...` và tiếp tục retry.
2. Nếu polling timeout, App không crash, chỉ giữ trạng thái chờ.
3. Nếu session không tồn tại, App hiển thị thông báo kiểm tra lại thanh toán.

---

## 4. Timer kiểm tra hết hạn gói

### Mục tiêu
Tự động khóa quyền truy cập khi phiên đã hết hạn.

### Tác nhân
- App
- Preferences
- AccessSessionService
- User

### Luồng chính
1. Sau khi vào app chính, App khởi động timer 60 giây/lần.
2. Timer đọc `ExpiresAt` từ `Preferences`.
3. App so sánh thời gian hiện tại với `ExpiresAt`.
4. Nếu vẫn còn hạn, App giữ nguyên trạng thái.
5. Timer lặp lại ở chu kỳ kế tiếp.

### Luồng thay thế
1. Nếu phiên sắp hết hạn, App có thể hiển thị cảnh báo trước.

### Luồng lỗi
1. Nếu `ExpiresAt` không hợp lệ, App coi là phiên hết hạn.
2. App hiện alert hết hạn.
3. App điều hướng về `SubscriptionPage`.

---

## 5. Tải dữ liệu địa điểm

### Mục tiêu
Lấy danh sách POI từ Supabase để dùng cho bản đồ, danh sách và geofence.

### Tác nhân
- App
- PlaceService
- Supabase
- Cache in-memory

### Luồng chính
1. App hoặc `MainPage` khởi tạo.
2. `PlaceService` kiểm tra cache in-memory.
3. Nếu chưa có cache, `PlaceService` gọi Supabase lấy `Places`.
4. Supabase trả về danh sách địa điểm đang active/approved.
5. `PlaceService` ánh xạ dữ liệu sang model C#.
6. `PlaceService` lưu danh sách vào cache in-memory.
7. App bind dữ liệu lên UI.

### Luồng thay thế
1. Nếu cache đã có, `PlaceService` trả dữ liệu từ bộ nhớ ngay.

### Luồng lỗi
1. Nếu Supabase lỗi hoặc timeout, App hiển thị thông báo tải dữ liệu thất bại.
2. Nếu có cache cũ, App dùng cache cũ thay vì chặn người dùng.

---

## 6. Xem danh sách địa điểm nổi bật

### Mục tiêu
Cho người dùng duyệt địa điểm theo dạng danh sách.

### Tác nhân
- User
- MainPage
- PlaceService

### Luồng chính
1. User mở `MainPage`.
2. `MainPage` yêu cầu dữ liệu từ `PlaceService`.
3. `PlaceService` trả danh sách địa điểm.
4. `MainPage` hiển thị card gồm ảnh, tên, rating, giá, giờ mở cửa.
5. User cuộn danh sách và xem các địa điểm.

### Luồng thay thế
1. Nếu chưa có ảnh chính, App hiển thị ảnh mặc định.

### Luồng lỗi
1. Nếu không tải được dữ liệu, `MainPage` hiển thị trạng thái trống hoặc nút thử lại.

---

## 7. Tìm kiếm địa điểm

### Mục tiêu
Cho phép tìm nhanh địa điểm theo từ khóa.

### Tác nhân
- User
- MainPage
- PlaceService

### Luồng chính
1. User nhập từ khóa vào ô tìm kiếm.
2. `MainPage` nhận sự kiện thay đổi nội dung.
3. App lọc danh sách theo tên, địa chỉ, mô tả, đặc sản.
4. `MainPage` render danh sách kết quả realtime.
5. User chọn một địa điểm từ danh sách.

### Luồng thay thế
1. Nếu ô tìm kiếm trống, App trả về toàn bộ danh sách.

### Luồng lỗi
1. Nếu không có kết quả, App hiển thị trạng thái `Không tìm thấy địa điểm phù hợp`.

---

## 8. Lọc địa điểm theo danh mục

### Mục tiêu
Cho phép thu hẹp danh sách địa điểm theo nhóm nhu cầu.

### Tác nhân
- User
- MainPage

### Luồng chính
1. User bấm một chip danh mục như `Cà phê`, `Cơm`, `Nhậu`.
2. `MainPage` cập nhật danh mục đang chọn.
3. App lọc dữ liệu danh sách theo category hoặc tags.
4. `MainPage` hiển thị danh sách đã lọc.

### Luồng thay thế
1. Nếu User chọn `Tất cả`, App bỏ bộ lọc và hiển thị toàn bộ.

### Luồng lỗi
1. Nếu danh mục không có kết quả, App hiển thị danh sách rỗng có mô tả ngắn.

---

## 9. Mở chi tiết địa điểm từ danh sách

### Mục tiêu
Xem đầy đủ thông tin của một địa điểm.

### Tác nhân
- User
- MainPage
- PlaceDetailPage
- PlaceService
- Supabase

### Luồng chính
1. User chạm vào một card địa điểm.
2. `MainPage` điều hướng tới `PlaceDetailPage` với `PlaceId`.
3. `PlaceDetailPage` gọi `PlaceService` lấy dữ liệu chi tiết.
4. `PlaceService` lấy thông tin địa điểm và danh sách ảnh.
5. `PlaceDetailPage` hiển thị gallery, địa chỉ, giờ mở cửa, giá, số điện thoại, website.

### Luồng thay thế
1. Nếu dữ liệu đã có từ danh sách, App hiển thị trước phần cơ bản rồi tải thêm ảnh.

### Luồng lỗi
1. Nếu tải ảnh thất bại, App vẫn hiển thị phần thông tin text.

---

## 10. Xem bản đồ và marker địa điểm

### Mục tiêu
Hiển thị toàn bộ POI và vị trí người dùng trên bản đồ tương tác.

### Tác nhân
- User
- MapPage
- PlaceService
- LocationService
- Mapsui

### Luồng chính
1. User mở `MapPage`.
2. `MapPage` lấy danh sách POI từ `PlaceService`.
3. `MapPage` lấy vị trí hiện tại từ `LocationService`.
4. `MapPage` render marker POI và marker người dùng trên nền bản đồ.
5. App bật chế độ tự bám vị trí nếu `_followUserLocation = true`.

### Luồng thay thế
1. Nếu chưa lấy được vị trí, App vẫn hiển thị bản đồ và POI.

### Luồng lỗi
1. Nếu tile map lỗi mạng, App hiển thị lớp bản đồ lỗi nhưng không làm treo ứng dụng.

---

## 11. Chạm marker để mở bottom card

### Mục tiêu
Cho người dùng thao tác nhanh với địa điểm ngay trên bản đồ.

### Tác nhân
- User
- MapPage

### Luồng chính
1. User chạm vào marker POI.
2. `MapPage` xác định marker tương ứng với `PlaceId`.
3. App lấy dữ liệu địa điểm.
4. App hiển thị bottom card chứa tên, rating, khoảng cách và các nút hành động.

### Luồng thay thế
1. Nếu User chạm vùng trống trên bản đồ, App đóng bottom card.

### Luồng lỗi
1. Nếu marker không ánh xạ được dữ liệu, App bỏ qua thao tác.

---

## 12. Chỉ đường từ bản đồ

### Mục tiêu
Tính và vẽ đường đi từ vị trí hiện tại đến địa điểm.

### Tác nhân
- User
- MapPage
- LocationService
- RoutingService
- OSRM API

### Luồng chính
1. User bấm nút `Chỉ đường` trên bottom card.
2. `MapPage` lấy vị trí hiện tại từ `LocationService`.
3. `MapPage` gửi origin và destination sang `RoutingService`.
4. `RoutingService` gọi OSRM API.
5. OSRM trả về polyline tuyến đường.
6. `RoutingService` parse kết quả và trả về cho `MapPage`.
7. `MapPage` vẽ route lên bản đồ.
8. `MapPage` hiển thị panel cho phép hủy route.

### Luồng thay thế
1. Nếu đã có route cũ, App xóa route cũ trước khi vẽ route mới.

### Luồng lỗi
1. Nếu chưa có GPS hiện tại, App báo chưa xác định được vị trí.
2. Nếu OSRM lỗi, App hiển thị thông báo không thể tính đường đi.

---

## 13. Hủy route trên bản đồ

### Mục tiêu
Cho phép quay lại trạng thái xem bản đồ thông thường.

### Tác nhân
- User
- MapPage

### Luồng chính
1. User bấm `Hủy route`.
2. `MapPage` xóa polyline tuyến đường.
3. `MapPage` ẩn panel route.
4. Bản đồ quay về chế độ xem POI thông thường.

---

## 14. Mở chi tiết địa điểm từ bản đồ

### Mục tiêu
Đi từ marker sang trang chi tiết địa điểm.

### Tác nhân
- User
- MapPage
- PlaceDetailPage

### Luồng chính
1. User mở bottom card của một POI.
2. User bấm `Chi tiết`.
3. `MapPage` điều hướng đến `PlaceDetailPage` với `PlaceId`.
4. `PlaceDetailPage` tải và hiển thị thông tin chi tiết.

---

## 15. Chỉ đường từ trang chi tiết

### Mục tiêu
Cho phép tính route từ `PlaceDetailPage`.

### Tác nhân
- User
- PlaceDetailPage
- MapPage

### Luồng chính
1. User bấm `Chỉ đường` trên `PlaceDetailPage`.
2. `PlaceDetailPage` lưu `PendingRoute` hoặc tham số route mục tiêu cho `MapPage`.
3. App chuyển sang tab bản đồ.
4. `MapPage` đọc `PendingRoute`.
5. `MapPage` tự động gọi luồng tính route.
6. App vẽ đường đi đến địa điểm đích.

### Luồng lỗi
1. Nếu `PendingRoute` không hợp lệ, `MapPage` hiển thị lỗi ngắn và không vẽ route.

---

## 16. Gọi điện từ trang chi tiết hoặc bản đồ

### Mục tiêu
Mở trình gọi điện với số điện thoại của địa điểm.

### Tác nhân
- User
- App
- Dialer hệ điều hành

### Luồng chính
1. User bấm `Gọi điện`.
2. App kiểm tra địa điểm có số điện thoại hay không.
3. App gọi launcher mở dialer với số điện thoại tương ứng.
4. Hệ điều hành mở ứng dụng gọi điện.

### Luồng lỗi
1. Nếu địa điểm không có số điện thoại, App hiển thị thông báo không có dữ liệu.
2. Nếu thiết bị không hỗ trợ dialer, App hiển thị lỗi tương ứng.

---

## 17. Phát thuyết minh thủ công

### Mục tiêu
Cho người dùng nghe thuyết minh ngay, không cần chờ geofence.

### Tác nhân
- User
- MapPage hoặc PlaceDetailPage
- NarrationService
- TextToSpeech API

### Luồng chính
1. User bấm `Thuyết minh`.
2. App lấy `tts_script` và `tts_locale` của địa điểm.
3. `NarrationService` gửi nội dung sang TextToSpeech.
4. Hệ thống TTS phát âm thanh trên thiết bị.

### Luồng thay thế
1. Nếu `tts_locale` không có, App dùng locale mặc định `vi-VN`.

### Luồng lỗi
1. Nếu không có `tts_script`, App báo địa điểm chưa có nội dung thuyết minh.
2. Nếu TTS engine lỗi, App hiển thị thông báo và dừng phát.

---

## 18. Theo dõi GPS thời gian thực

### Mục tiêu
Cập nhật vị trí người dùng liên tục cho bản đồ và geofence.

### Tác nhân
- Hệ điều hành
- LocationForegroundService Android hoặc polling iOS
- LocationService
- MapPage
- GeofenceEngine

### Luồng chính
1. App khởi động dịch vụ định vị nền.
2. Hệ điều hành trả về tọa độ mới.
3. `LocationService` nhận tọa độ và phát sự kiện `LocationChanged`.
4. `MapPage` cập nhật marker người dùng.
5. `GeofenceEngine` nhận dữ liệu vị trí để kiểm tra POI gần nhất.

### Luồng thay thế
1. Nếu người dùng đang kéo bản đồ, `MapPage` tạm tắt auto-follow.

### Luồng lỗi
1. Nếu người dùng từ chối quyền vị trí, App hiển thị yêu cầu cấp quyền và không thể chạy geofence.
2. Nếu GPS yếu, App vẫn giữ vị trí gần nhất đã biết.

---

## 19. Geofence tự động phát thuyết minh

### Mục tiêu
Tự động phát audio khi người dùng đi vào vùng POI phù hợp.

### Tác nhân
- LocationService
- GeofenceEngine
- PlaceService
- NarrationService
- User

### Luồng chính
1. `LocationService` phát sự kiện có vị trí mới.
2. `GeofenceEngine` nhận tọa độ hiện tại.
3. `GeofenceEngine` lọc danh sách POI có `tts_script`.
4. `GeofenceEngine` tính khoảng cách tới từng POI.
5. `GeofenceEngine` giữ lại các POI nằm trong bán kính cấu hình.
6. `GeofenceEngine` loại các POI đang trong cooldown.
7. `GeofenceEngine` sắp xếp theo `priority`, rồi theo khoảng cách gần hơn.
8. `GeofenceEngine` áp dụng debounce 2 giây để xác nhận user thực sự ở trong vùng.
9. `GeofenceEngine` trả về POI ưu tiên nhất.
10. `MapPage` hoặc controller kiểm tra `_lastSpokenPlaceId`.
11. Nếu khác địa điểm vừa phát gần nhất, App gọi `NarrationService.SpeakAsync`.
12. `NarrationService` phát TTS.
13. App cập nhật `LastPlayedAt` cho POI và lưu trạng thái cooldown.

### Luồng thay thế
1. Nếu nhiều POI cùng vùng, App chọn POI có `priority` cao hơn.
2. Nếu bằng `priority`, App chọn POI gần hơn.

### Luồng lỗi
1. Nếu GPS nhảy sai làm user ra vào vùng liên tục, debounce ngăn trigger tức thời.
2. Nếu TTS đang phát, App có thể bỏ qua trigger mới hoặc xếp hàng tùy thiết kế triển khai.

---

## 20. Tránh đọc lặp lại cùng một địa điểm

### Mục tiêu
Ngăn việc app đọc đi đọc lại khi user đứng lâu gần một POI.

### Tác nhân
- GeofenceEngine
- NarrationService

### Luồng chính
1. App vừa phát xong thuyết minh cho một `PlaceId`.
2. App lưu `LastPlayedAt` và `_lastSpokenPlaceId`.
3. Ở các lần cập nhật vị trí sau, `GeofenceEngine` kiểm tra cooldown.
4. Nếu POI chưa hết cooldown, App không phát lại.
5. Khi hết cooldown, POI được phép tham gia xét chọn lại.

---

## 21. Reverse geocoding tên đường hiện tại

### Mục tiêu
Hiển thị tên đường/khu vực người dùng đang đứng.

### Tác nhân
- LocationService
- Geocoding API hoặc nền tảng
- App

### Luồng chính
1. `LocationService` nhận vị trí mới.
2. App kiểm tra mốc thời gian cache geocode gần nhất.
3. Nếu quá 15 giây hoặc di chuyển quá 30m, App gọi reverse geocoding.
4. Dịch vụ geocode trả về tên đường hoặc địa chỉ ngắn.
5. App cập nhật thông tin vị trí hiện tại trên UI.

### Luồng thay thế
1. Nếu chưa vượt ngưỡng thời gian và khoảng cách, App dùng dữ liệu cache.

### Luồng lỗi
1. Nếu geocoding lỗi, App chỉ hiển thị tọa độ hoặc bỏ qua.

---

## 22. Xem danh sách tour có sẵn

### Mục tiêu
Cho người dùng duyệt các tour theo chủ đề.

### Tác nhân
- User
- ToursPage
- POIService hoặc TourService
- Supabase

### Luồng chính
1. User mở `ToursPage`.
2. `ToursPage` gọi service lấy danh sách tour.
3. Service lấy dữ liệu từ Supabase.
4. App hiển thị tên tour, mô tả, số điểm dừng.
5. User chọn một tour.

### Luồng lỗi
1. Nếu không có tour, App hiển thị trạng thái trống.

---

## 23. Xem chi tiết tour

### Mục tiêu
Cho người dùng xem hành trình và các điểm dừng trong tour.

### Tác nhân
- User
- ToursPage
- TourDetailPage
- TourService

### Luồng chính
1. User chọn một tour từ `ToursPage`.
2. App điều hướng đến `TourDetailPage`.
3. `TourDetailPage` gọi service lấy danh sách điểm dừng theo thứ tự.
4. App hiển thị thông tin tour và từng điểm dừng.
5. User chọn một điểm dừng bất kỳ.
6. App mở `PlaceDetailPage` tương ứng.

---

## 24. Theo tour và tự phát thuyết minh tại điểm dừng

### Mục tiêu
Kết hợp tour với geofence thực tế.

### Tác nhân
- User
- TourDetailPage
- GeofenceEngine
- NarrationService

### Luồng chính
1. User xem chi tiết một tour và bắt đầu di chuyển theo tour.
2. App không cần chế độ phát riêng, vẫn dùng `GeofenceEngine` chung.
3. Khi user đi vào bán kính của điểm dừng trong tour, `GeofenceEngine` phát hiện POI.
4. App tự phát thuyết minh của điểm dừng đó.
5. User tiếp tục di chuyển tới điểm tiếp theo.

### Luồng thay thế
1. Nếu địa điểm vừa thuộc tour vừa thuộc danh sách POI chung, App vẫn chỉ phát một lần theo logic geofence.

---

## 25. Quét mã QR để mở địa điểm

### Mục tiêu
Truy cập nhanh thông tin địa điểm bằng mã QR tại chỗ.

### Tác nhân
- User
- QRScanPage
- Camera
- ZXing
- PlaceDetailPage

### Luồng chính
1. User mở `QRScanPage`.
2. App yêu cầu quyền camera nếu cần.
3. Camera mở giao diện quét.
4. User đưa QR vào khung quét.
5. ZXing giải mã nội dung QR.
6. App parse dữ liệu nhận được thành `PlaceId`.
7. App điều hướng đến `PlaceDetailPage` tương ứng.

### Luồng thay thế
1. Nếu QR chứa URL hoặc payload khác, App parse theo format đã quy định nếu có.

### Luồng lỗi
1. Nếu user từ chối quyền camera, App hiển thị hướng dẫn cấp quyền.
2. Nếu QR không hợp lệ, App hiển thị `Mã QR không hợp lệ`.
3. Nếu `PlaceId` không tồn tại, App hiển thị không tìm thấy địa điểm.

---

## 26. Admin kích hoạt session thủ công

### Mục tiêu
Cho phép vận hành xác nhận thanh toán và mở quyền truy cập cho khách.

### Tác nhân
- Admin
- Supabase Dashboard
- SQL Function `activate_session()`
- AccessSessions

### Luồng chính
1. Admin kiểm tra giao dịch chuyển khoản ngân hàng.
2. Admin lấy `DeviceId` từ nội dung chuyển khoản.
3. Admin mở Supabase Dashboard.
4. Admin chạy hàm `SELECT activate_session('<DeviceId>')`.
5. Supabase tìm session chờ thanh toán theo `DeviceId`.
6. Supabase cập nhật `IsActive`, `ActivatedAt`, `ExpiresAt`.
7. Session sẵn sàng để app client polling thấy trạng thái mới.

### Luồng lỗi
1. Nếu `DeviceId` sai, hàm không kích hoạt được session.
2. Nếu session đã được kích hoạt rồi, hệ thống phải tránh kích hoạt trùng.

---

## 27. Khôi phục trạng thái sau khi đóng mở lại app

### Mục tiêu
Giữ nguyên phiên sử dụng và dữ liệu cục bộ sau khi app bị đóng.

### Tác nhân
- User
- App
- Preferences

### Luồng chính
1. User đóng app.
2. App đã lưu `DeviceId`, `SessionId`, `ExpiresAt` trong `Preferences`.
3. User mở lại app.
4. App đọc lại dữ liệu cục bộ.
5. Nếu phiên còn hạn, App vào thẳng màn hình chính.

### Luồng lỗi
1. Nếu dữ liệu Preferences bị mất hoặc hỏng, App yêu cầu user đăng ký gói lại.

---

## 28. Xử lý mất mạng trong khi sử dụng app

### Mục tiêu
Đảm bảo app không crash khi mất kết nối.

### Tác nhân
- User
- App
- Supabase
- OSRM

### Luồng chính
1. User đang dùng app thì mất mạng.
2. Các request mới tới Supabase hoặc OSRM bị lỗi.
3. App bắt lỗi ở service layer.
4. App hiển thị thông báo phù hợp theo từng ngữ cảnh.
5. Các màn hình tiếp tục hoạt động với dữ liệu đã cache nếu có.

### Luồng thay thế
1. Polling session tự retry theo chu kỳ.
2. Danh sách địa điểm dùng cache in-memory nếu đã tải trước đó.

### Luồng lỗi
1. Nếu không có cache và không có mạng, App chỉ hiển thị trạng thái lỗi thay vì crash.

---

## 29. Từ chối quyền vị trí

### Mục tiêu
Xử lý trường hợp người dùng không cấp GPS.

### Tác nhân
- User
- App
- Hệ điều hành

### Luồng chính
1. App yêu cầu quyền vị trí.
2. User từ chối.
3. Hệ điều hành trả về trạng thái denied.
4. App hiển thị giải thích rằng bản đồ vẫn xem được nhưng geofence và chỉ đường sẽ bị hạn chế.
5. App cung cấp nút mở cài đặt hệ thống nếu cần.

### Luồng lỗi
1. Nếu user chọn `Don't ask again`, App không yêu cầu lặp vô hạn mà chuyển sang hướng dẫn vào settings.

---

## 30. Từ chối quyền camera

### Mục tiêu
Xử lý trường hợp không thể quét QR.

### Tác nhân
- User
- QRScanPage
- Hệ điều hành

### Luồng chính
1. `QRScanPage` yêu cầu quyền camera.
2. User từ chối.
3. App hiển thị thông báo không thể dùng tính năng quét QR khi chưa cấp quyền.
4. App hướng dẫn mở Settings nếu muốn cấp lại.

---

## 31. App chuyển giữa các tab mà không tải lại dữ liệu Places

### Mục tiêu
Giữ hiệu năng tốt khi người dùng đổi màn hình liên tục.

### Tác nhân
- User
- MainPage
- MapPage
- PlaceService
- Cache in-memory

### Luồng chính
1. User mở `MainPage`, dữ liệu Places được tải vào cache.
2. User chuyển sang `MapPage`.
3. `MapPage` yêu cầu `PlaceService` cung cấp Places.
4. `PlaceService` trả dữ liệu từ cache.
5. User quay lại `MainPage`.
6. `MainPage` tiếp tục dùng dữ liệu cache mà không gọi Supabase lại.

---

## 32. Auto-follow vị trí người dùng trên bản đồ

### Mục tiêu
Giữ camera bản đồ bám theo vị trí user khi phù hợp.

### Tác nhân
- User
- MapPage
- LocationService

### Luồng chính
1. `MapPage` bật `_followUserLocation = true`.
2. `LocationService` phát vị trí mới.
3. `MapPage` cập nhật tâm bản đồ theo vị trí user.

### Luồng thay thế
1. Nếu User kéo hoặc zoom bản đồ bằng tay, `MapPage` đặt `_followUserLocation = false`.
2. Khi User bấm nút định vị lại, App bật `_followUserLocation = true` trở lại.

---

## 33. Hiển thị trạng thái mở/đóng cửa theo thời gian thực

### Mục tiêu
Cho người dùng biết địa điểm đang mở hay đóng.

### Tác nhân
- PlaceDetailPage
- MainPage
- Place model
- Clock hệ thống

### Luồng chính
1. App đọc `OpenTime` và `CloseTime` của địa điểm.
2. App so sánh với giờ hiện tại của thiết bị.
3. Nếu đang trong khoảng mở cửa, App hiển thị trạng thái `Đang mở`.
4. Nếu ngoài khoảng thời gian đó, App hiển thị `Đã đóng`.

### Luồng lỗi
1. Nếu dữ liệu giờ mở cửa thiếu hoặc sai format, App ẩn trạng thái thay vì hiển thị sai.

---

## 34. Hiển thị gallery ảnh địa điểm

### Mục tiêu
Cho người dùng xem ảnh của địa điểm trong trang chi tiết.

### Tác nhân
- PlaceDetailPage
- PlaceService
- Supabase

### Luồng chính
1. `PlaceDetailPage` gọi service lấy `PlaceImages`.
2. Service trả về danh sách ảnh theo `SortOrder`.
3. `PlaceDetailPage` hiển thị gallery cuộn ngang.
4. User vuốt để xem nhiều ảnh.

### Luồng thay thế
1. Nếu chỉ có 1 ảnh, App hiển thị một ảnh tĩnh.

### Luồng lỗi
1. Nếu tất cả ảnh lỗi tải, App hiển thị placeholder.

---

## 35. Khởi động app lần đầu với đầy đủ dependency

### Mục tiêu
Đảm bảo các service cần thiết được khởi tạo đúng thứ tự.

### Tác nhân
- User
- MauiProgram
- DI Container
- Services

### Luồng chính
1. User mở ứng dụng.
2. `MauiProgram` cấu hình DI container.
3. App đăng ký singleton services: `PlaceService`, `LocationService`, `AccessSessionService`, `NarrationService`, `GeofenceEngine`.
4. App đăng ký transient pages.
5. App khởi tạo `App`.
6. App xác định màn hình đầu tiên dựa trên trạng thái session.

### Luồng lỗi
1. Nếu service thiết yếu khởi tạo thất bại, App phải ghi log và dừng ở trạng thái lỗi an toàn.

---

## Gợi ý sử dụng tài liệu này

- Dùng phần này để vẽ sequence diagram UML.
- Mỗi mục có thể chuyển thành 1 biểu đồ riêng trong StarUML, Draw.io hoặc PlantUML.
- Nếu cần gom cho báo cáo, nên nhóm thành 5 cụm:
  - Access & Payment
  - Places & Map
  - GPS, Geofence & TTS
  - Tours & QR
  - Error handling & Permissions
