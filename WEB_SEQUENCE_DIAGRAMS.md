# 📊 TourGuideAPP Web - Sơ đồ Sequence
## 📝 Ghi chú quan trọng

### Frontend (TourismApp.Web)
- **MVC Framework** — Render HTML views
- **Session Management** — Lưu JWT token trong session/cookie
- **HttpClient** — Gọi TourGuideAPI qua ApiService
- **Areas** — Admin routes tách riêng qua **Areas/Admin**

### Backend (TourGuideAPI)
- **JWT Authentication** — Verify token từ requests
- **FluentValidation** — Validate input DTOs
- **Rate Limiting** — Giới hạn API calls
- **CORS** — Cho phép cross-origin requests từ web
- **SignalR Hub** — Notifications real-time (`/hubs/notifications`)

### Key Flows
1. **Login** → JWT generated → Stored in session cookie
2. **API Calls** → JWT từ session → Sent in Authorization header
3. **Admin Area** → Require `[Authorize(Roles = "Admin")]` attribute
4. **Real-time** → SignalR pushes notifications khi có events

---

## 📂 File Structure
```
TourGuideWeb/
├── TourGuideAPI/           (Backend REST API)
│   ├── Controllers/
│   ├── Services/
│   ├── Data/
│   └── Program.cs          (DI, JWT config, SignalR)
└── TourismApp.Web/         (Frontend MVC)
    ├── Controllers/        (Auth, Dashboard, Places, etc.)
    ├── Areas/Admin/        (Admin-only routes & views)
    ├── Services/
    │   └── ApiService.cs   (HttpClient wrapper)
    └── Program.cs          (MVC, Session, Auth config)
```
