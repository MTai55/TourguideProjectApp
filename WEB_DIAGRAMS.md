# 📊 TourGuideAPP Web - Sơ đồ Sequence & Activity

---

## 🔷 SEQUENCE DIAGRAMS

### 1️⃣ Authentication & Login Flow

```mermaid
sequenceDiagram
    actor User
    participant Web as TourismApp.Web
    participant Auth as AuthController
    participant API as TourGuideAPI
    participant DB as PostgreSQL
    participant Session as Session/Cookie

    User->>Web: 1. Truy cập /Auth/Login
    activate Web
    Web->>Web: Hiển thị login form
    deactivate Web

    User->>Auth: 2. POST /Auth/Login (email, password)
    activate Auth
    Auth->>API: 3. POST /api/auth/login
    activate API
    API->>DB: 4. Query User từ database
    activate DB
    DB-->>API: User + hashed password
    deactivate DB
    API->>API: 5. Verify password + Generate JWT token
    API-->>Auth: 6. Return JWT token
    deactivate API
    Auth->>Session: 7. Lưu JWT vào Session
    Auth->>Session: 8. Lưu vào Cookie "auth"
    deactivate Auth
    Auth-->>Web: 9. Redirect to /Dashboard
    Web->>Web: 10. Hiển thị Dashboard
    Web-->>User: 11. Đã đăng nhập thành công
```

---

### 2️⃣ View Places with Filtering

```mermaid
sequenceDiagram
    actor User
    participant Web as TourismApp.Web
    participant PlacesCtrl as PlacesController
    participant ApiService as ApiService
    participant API as TourGuideAPI
    participant DB as PostgreSQL

    User->>Web: 1. Truy cập /Places?category=Restaurant
    activate Web
    Web->>PlacesCtrl: 2. GET /Places (category, search)
    activate PlacesCtrl
    PlacesCtrl->>ApiService: 3. GetPlaces(query params)
    activate ApiService
    ApiService->>API: 4. GET /api/places/search
    activate API
    API->>DB: 5. Query Places với filter
    activate DB
    DB-->>API: Danh sách Places + Images
    deactivate DB
    API-->>ApiService: 6. Trả về JSON
    deactivate API
    ApiService-->>PlacesCtrl: 7. Deserialize to Model
    deactivate ApiService
    PlacesCtrl->>Web: 8. Return View(places)
    deactivate PlacesCtrl
    Web->>Web: 9. Render Places list page
    Web-->>User: 10. Hiển thị danh sách địa điểm
```

---

### 3️⃣ Submit & View Reviews

```mermaid
sequenceDiagram
    actor User
    participant Web as TourismApp.Web
    participant ReviewCtrl as ReviewsController
    participant ApiService as ApiService
    participant API as TourGuideAPI
    participant DB as PostgreSQL

    User->>Web: 1. Truy cập chi tiết place + form review
    activate Web
    Web->>ReviewCtrl: 2. GET /Reviews/Create/{placeId}
    deactivate Web
    activate ReviewCtrl
    ReviewCtrl->>Web: Hiển thị form
    deactivate ReviewCtrl

    User->>ReviewCtrl: 3. POST /Reviews/Create (rating, comment, placeId)
    activate ReviewCtrl
    ReviewCtrl->>ReviewCtrl: 4. Validate form data
    ReviewCtrl->>ApiService: 5. PostReview(reviewDto)
    activate ApiService
    ApiService->>ApiService: 6. Thêm JWT from Session
    ApiService->>API: 7. POST /api/reviews (with Authorization header)
    activate API
    API->>API: 8. Verify JWT token + User role
    API->>DB: 9. Insert Review record
    activate DB
    DB-->>API: Review ID created
    deactivate DB
    API-->>ApiService: 10. Success response
    deactivate API
    ApiService-->>ReviewCtrl: 11. Trả về response
    deactivate ApiService
    ReviewCtrl->>Web: 12. Redirect to place details
    deactivate ReviewCtrl
    Web->>ReviewCtrl: 13. GET /Places/Details/{id}
    activate ReviewCtrl
    ReviewCtrl->>ApiService: 14. GetReviews(placeId)
    ApiService->>API: 15. GET /api/reviews/{placeId}
    activate API
    API->>DB: 16. Query reviews
    activate DB
    DB-->>API: Reviews list
    deactivate DB
    API-->>ApiService: Reviews
    deactivate API
    ApiService-->>ReviewCtrl: Deserialize reviews
    deactivate ReviewCtrl
    Web-->>User: 18. Hiển thị reviews + form mới
```

---

### 4️⃣ Admin Dashboard Navigation

```mermaid
sequenceDiagram
    actor Admin
    participant Web as TourismApp.Web
    participant AdminDash as Admin/DashboardCtrl
    participant ApiService as ApiService
    participant API as TourGuideAPI
    participant DB as PostgreSQL
    participant Cache as Redis

    Admin->>Web: 1. Truy cập /Admin/Dashboard
    activate Web
    Web->>AdminDash: 2. GET /Admin/Dashboard
    activate AdminDash
    
    AdminDash->>AdminDash: 3. Check [Authorize(Role=Admin)]
    
    AdminDash->>ApiService: 4. GetAnalytics()
    activate ApiService
    ApiService->>API: 5. GET /api/analytics/summary
    activate API
    API->>Cache: 6. Check cache exists?
    activate Cache
    Cache-->>API: Cache miss
    deactivate Cache
    API->>DB: 7. Query analytics data
    activate DB
    DB-->>API: Stats (places, reviews, users)
    deactivate DB
    API->>Cache: 8. Store in cache (TTL 5min)
    activate Cache
    Cache-->>API: OK
    deactivate Cache
    API-->>ApiService: 9. Return analytics
    deactivate API
    ApiService-->>AdminDash: 10. Deserialize
    deactivate ApiService
    
    AdminDash->>ApiService: 11. GetPendingComplaints()
    activate ApiService
    ApiService->>API: 12. GET /api/complaints?status=pending
    activate API
    API->>DB: 13. Query pending complaints
    activate DB
    DB-->>API: Complaints list
    deactivate DB
    API-->>ApiService: Complaints
    deactivate API
    ApiService-->>AdminDash: Complaints
    deactivate ApiService
    
    AdminDash->>Web: 14. Return View(model)
    deactivate AdminDash
    Web->>Web: 15. Render dashboard page
    Web-->>Admin: 16. Hiển thị charts + stats
```

---

### 5️⃣ Overall Web Architecture

```mermaid
graph TB
    subgraph Client["🖥️ Client (Browser)"]
        UI["User Interface<br/>(HTML/CSS/JS)"]
    end
    
    subgraph Frontend["🌐 TourismApp.Web<br/>(ASP.NET Core MVC)"]
        MVC["MVC Controllers<br/>- Auth<br/>- Dashboard<br/>- Places<br/>- Reviews<br/>- Analytics<br/>- Admin Area"]
        Session["Session Manager<br/>(JWT in Cookie)"]
        ApiCli["HttpClient<br/>(ApiService)"]
    end
    
    subgraph Backend["🔧 TourGuideAPI<br/>(ASP.NET Core REST)"]
        Auth["Auth Service<br/>(JWT validation)"]
        Controllers["API Controllers<br/>- AuthController<br/>- PlacesController<br/>- ReviewsController<br/>- AnalyticsController<br/>- ComplaintsController<br/>- TrackingController<br/>- PromoController"]
        Validation["Validators<br/>(FluentValidation)"]
        Middleware["Middleware<br/>- Exception Handler<br/>- Rate Limiter<br/>- CORS"]
        SignalR["SignalR Hub<br/>(Notifications)"]
    end
    
    subgraph DataLayer["💾 Data Layer"]
        EF["Entity Framework<br/>Core"]
        PostgreSQL["PostgreSQL DB<br/>- Users<br/>- Places<br/>- Reviews<br/>- Complaints"]
    end
    
    UI -->|HTTP/HTTPS| MVC
    MVC -->|Store JWT| Session
    MVC -->|Call API| ApiCli
    ApiCli -->|REST Requests| Controllers
    Controllers -->|Validate| Validation
    Controllers -->|JWT Auth| Auth
    Controllers -->|Use| EF
    Middleware -->|Apply to Requests| Controllers
    Controllers -->|Real-time| SignalR
    SignalR -->|Push to Client| UI
    EF -->|Query| PostgreSQL
    PostgreSQL -->|Return Data| EF
```

---

## 🔶 ACTIVITY DIAGRAMS

### 6️⃣ Activity - User Authentication Flow

```mermaid
flowchart TD
    A([User Access App]) --> B{Token Valid?}
    B -->|Yes| C[Redirect to Dashboard]
    C --> D([Logged In])
    B -->|No| E[Show Login Form]
    E --> F[User Enter Email & Password]
    F --> G{Input Valid?}
    G -->|Yes| H[Send Credentials to API]
    H --> I[API Verify Password]
    I --> J{Password Match?}
    J -->|Yes| K[Generate JWT Token]
    K --> L[Save to Session/Cookie]
    L --> M[Redirect to Dashboard]
    M --> D
    J -->|No| N[Return Error: Invalid password]
    N --> O[Show Error Message]
    O --> P[User Retry]
    P --> F
    G -->|No| Q[Show Validation Errors]
    Q --> R[User Retry]
    R --> F
```

---

### 7️⃣ Activity - Search & Filter Places

```mermaid
flowchart TD
    A([User on Places Page]) --> B{Cache Valid?}
    B -->|Yes| C[Display All Places]
    B -->|No| D[Load from API]
    D --> E[Cache Results]
    E --> C
    C --> F[User Enter Search/Filter]
    F --> G{Filters Valid?}
    G -->|No| H[Show Error: Invalid Filters]
    H --> I([End])
    G -->|Yes| J[Apply Client-side Filter]
    J --> K{Need API Call?}
    K -->|Yes| L[Send Query to API]
    L --> M[API Filter in DB]
    M --> N[Return Results]
    N --> O[Display Filtered Places]
    K -->|No| O
    O --> P[User Can Sort]
    P --> Q{User Click Detail?}
    Q -->|Yes| R([View Details])
    Q -->|No| Q
```

---

### 8️⃣ Activity - Submit Review Process

```mermaid
flowchart TD
    A([User on Place Detail]) --> B[User Click Write Review]
    B --> C[Show Review Form]
    C --> D[User Fill Rating & Comment]
    D --> E[User Submit]
    E --> F{Validate Form}
    F -->|Invalid| G[Show Validation Errors]
    G --> H[User Can Retry]
    H --> D
    F -->|Valid| I[Create ReviewDTO]
    I --> J[Get JWT from Session]
    J --> K[Send to API with Token]
    K --> L[API Verify JWT]
    L --> M{Token Valid?}
    M -->|No| N[Return 401 Unauthorized]
    N --> O[Redirect to Login]
    O --> P([Need Login])
    M -->|Yes| Q[Insert Review to DB]
    Q --> R{Success?}
    R -->|No| S[DB Error]
    S --> T[Return Error Message]
    T --> H
    R -->|Yes| U[Return Success]
    U --> V[Refresh Review List]
    V --> W[Show Notification: Review Posted]
    W --> X([Success])
```

---

### 9️⃣ Activity - API Request Processing Pipeline

```mermaid
flowchart TD
    A([HTTP Request Received]) --> B[Extract Headers]
    B --> C{CORS OK?}
    C -->|No| D[Return 403 Forbidden]
    D --> E([Rejected])
    C -->|Yes| F[Apply Rate Limiter]
    F --> G{Rate OK?}
    G -->|No| H[Return 429 Too Many Requests]
    H --> E
    G -->|Yes| I[Extract JWT Token]
    I --> J{Token Exists?}
    J -->|No| K[Return 401 Unauthorized]
    K --> E
    J -->|Yes| L[Validate JWT Signature]
    L --> M{Signature Valid?}
    M -->|No| K
    M -->|Yes| N[Check Token Expiry]
    N --> O{Not Expired?}
    O -->|No| K
    O -->|Yes| P[Extract User Claims]
    P --> Q[Execute Controller Action]
    Q --> R[Validate Request DTO]
    R --> S{DTO Valid?}
    S -->|No| T[Return 400 Bad Request]
    T --> E
    S -->|Yes| U[Execute Business Logic]
    U --> V{Success?}
    V -->|No| W[Handle Error]
    W --> X[Return Error Response]
    X --> Y([Error])
    V -->|Yes| Z[Return 200 OK + Data]
    Z --> AA([Success])
```

---

### 🔟 Activity - Admin Dashboard Data Loading

```mermaid
flowchart TD
    A([Admin Access Dashboard]) --> B[Check Authorization]
    B --> C{Is Admin?}
    C -->|No| D[Return 403 Forbidden]
    D --> E([Access Denied])
    C -->|Yes| F[For Each Widget/Card]
    F --> G[Check Cache for Data]
    G --> H{Cache Hit?}
    H -->|Yes| I[Use Cached Data]
    H -->|No| J[Query Database]
    J --> K[Calculate Metrics]
    K --> L[Store in Cache TTL 5min]
    L --> M[Build View Model]
    I --> M
    M --> N[Load Templates]
    N --> O[Render Dashboard]
    O --> P[Return HTML to Browser]
    P --> Q([Dashboard Displayed])
```

---

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

---

✅ **Bây giờ bạn đã có:**
- 5 sơ đồ **Sequence** (tương tác từng bước)
- 5 sơ đồ **Activity** (luồng quy trình với quyết định)
- Kiến trúc tổng thể
- Ghi chú quan trọng

🎯 Sử dụng [Mermaid Live Editor](https://mermaid.live) để xem trực tiếp!
