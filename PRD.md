# Product Requirements Document (PRD)
## TourGuideAPP - Smart Tourism & Location-Based Guide Application

**Version:** 1.0  
**Date:** March 2026  
**Status:** Active Development

---

## 1. Executive Summary

**TourGuideAPP** is a comprehensive cross-platform tourism application that combines mobile and web interfaces to deliver personalized tour guides, location-based information, and interactive tourism experiences. The application leverages geofencing, QR code scanning, and real-time location tracking to enhance user engagement with points of interest (POIs) and tourist destinations.

### Key Vision
Enable travelers to discover, explore, and engage with tourist attractions through an intelligent, location-aware mobile application that provides contextual information, personalized recommendations, and community-driven content.

---

## 2. Target Users

### Primary Users
- **Tourists & Travelers**: Individual travelers seeking guided tour information, place recommendations, and interactive discovery experiences
- **Tour Operators & Guides**: Professional guides who want to curate and manage tour routes
- **Tourism Businesses**: Hotels, museums, restaurants, and attractions seeking visibility and visitor engagement

### Secondary Users
- **Tourism Administrators**: Government/DMO officials managing destination content
- **Content Creators**: Local experts contributing place information and reviews

---

## 3. Product Overview

### 3.1 Platform Architecture

#### Mobile Application (Cross-Platform MAUI)
- **Supported Platforms**: Android, iOS, macOS, Windows Desktop
- **Framework**: .NET MAUI 10.0
- **Technology Stack**:
  - Mapsui 5.0.2 for interactive mapping
  - Supabase PostgreSQL backend
  - ZXing.Net for QR code scanning
  - SkiaSharp for high-performance graphics

#### Backend Infrastructure
- **API Framework**: ASP.NET Core
- **Database**: PostgreSQL (via Supabase)
- **Authentication**: JWT-based
- **Architecture Pattern**: Service-oriented with dependency injection
- **Additional Services**: SignalR hubs for real-time updates, Entity Framework Core ORM

#### Web Application (TourismApp.Web)
- ASP.NET Core web interface for administrators and content creators

---

## 4. Core Features

### 4.1 User Authentication & Management
**Goal**: Secure user access with role-based management

**Features**:
- User registration with email validation
- Social login integration (planned)
- JWT token-based authentication
- User profile management (avatar, bio, preferences)
- Password recovery and reset functionality
- User roles: Tourist, Tour Guide, Admin, Content Creator

**Related Models**: `User`, `RefreshToken`

### 4.2 Location Services & Mapping
**Goal**: Provide real-time location awareness and interactive exploration

**Features**:
- Real-time GPS location tracking with 5-second refresh intervals
- Interactive map view showing nearby places and POIs
- Location history tracking and visit logs
- Geofence-based notifications and auto-triggered narration
- Support for offline map regions

**Related Services**: `LocationService`, `GeofenceEngine`, `TrackingService`, `GeoLocationService`

**Related Models**: `Place`, `UserTracking`

### 4.3 Places & Points of Interest (POI) Discovery
**Goal**: Enable users to discover and learn about tourist attractions

**Features**:
- Browse places by category, proximity, and ratings
- Detailed place information (name, description, address, phone, website)
- Place images with galleries
- Real-time distance calculation based on user location
- Search and filter functionality
- Popular places recommendations

**Related Services**: `PlaceService`, `POIService`

**Related Models**: `Place`, `PlaceImage`, `Category`

### 4.4 Interactive Narration & Audio Guides
**Goal**: Deliver contextual audio information automatically based on location

**Features**:
- Text-to-speech narration for POI descriptions
- Multi-language support for narration
- Auto-triggered audio when entering geofence zones
- Manual narration playback control
- Narration preferences (voice, speed, language)

**Related Service**: `NarrationService`

### 4.5 Tours & Tour Routes
**Goal**: Provide curated, sequential tourism experiences

**Features**:
- Browse available tour routes and guides
- Create custom tours (for guides)
- Tour detail view with stop-by-stop itineraries
- Estimated duration and difficulty ratings
- Rating and review tours
- Follow tour guides in real-time (for group tours)

**Related Views**: `ToursPage`, `TourDetailPage`

### 4.6 Interactive QR Code Scanning
**Goal**: Enable instant access to POI information via QR codes

**Features**:
- QR code scanner with camera integration
- Instant navigation to associated place details
- Check-in functionality via QR codes
- QR code generation for places (admin feature)
- Analytics on QR code scans

**Related Views**: `QRScanPage`

**Related Service**: Uses `PlaceService` for QR resolution

### 4.7 Favorites & Wishlist Management
**Goal**: Allow users to save places and tours for later

**Features**:
- Add/remove places from favorites
- Create multiple wishlists for different trip plans
- Wishlist sharing with friends
- Wishlist export (PDF, itinerary format)
- Favorite places sync across devices

**Related Services**: `FavoriteService`, `WishlistService`

**Related Models**: `Favorite`, `Wishlist`

### 4.8 Reviews & Ratings System
**Goal**: Leverage community feedback to build trust and provide social proof

**Features**:
- User photo/video reviews
- Star rating (1-5) system
- Helpful vote counts on reviews
- Display of verified visitor badges
- Detailed review moderation tools (admin)
- Review response capability for business owners

**Related Models**: `Review`

**Related Middleware**: Global exception handling and validation

### 4.9 Promotions & Special Offers
**Goal**: Drive visitor adoption through targeted promotions

**Features**:
- Seasonal promotions and discounts for places
- Limited-time offers tracking
- Promotion notifications based on location/interests
- Coupon code management
- Analytics on promotion redemption

**Related Models**: `Promotion`

### 4.10 Complaints & Support System
**Goal**: Maintain service quality through user feedback

**Features**:
- Submit complaints about places or services
- Issue severity and category classification
- Support team ticketing system
- Complaint tracking and resolution updates
- Follow-up communication channels

**Related Models**: `Complaint`

### 4.11 User Engagement & Analytics
**Goal**: Track user behavior and optimize experience

**Features**:
- Visit history tracking
- Popular place analytics
- User journey analytics
- A/B testing capability
- Engagement metrics dashboard

**Related Models**: `VisitHistory`

**Related Service**: `TrackingService`

---

## 5. Data Models & Structure

### Core Models

| Model | Purpose | Key Fields |
|-------|---------|-----------|
| **User** | User account and profile | UserId, Email, Phone, Avatar, Bio, Preferences |
| **Place** | Tourist attractions/POIs | PlaceId, Name, Description, Address, Lat/Long, Phone, Website, Images |
| **PlaceImage** | Place photo gallery | ImageId, PlaceId, ImageUrl, Caption, UploadDate |
| **POI** | Points of Interest (subset of places) | POIId, Name, Category, Coordinates, Narration |
| **Category** | Place categorization | CategoryId, Name (Cultural, Restaurant, Hotel, etc.) |
| **Review** | User reviews and ratings | ReviewId, UserId, PlaceId, Rating, Text, Photos, Date |
| **Favorite** | User favorite places | FavoriteId, UserId, PlaceId, DateAdded |
| **Wishlist** | User wishlists | WishlistId, UserId, Title, CreatedDate |
| **Promotion** | Special offers | PromotionId, PlaceId, Discount%, StartDate, EndDate |
| **Complaint** | Support tickets | ComplaintId, UserId, PlaceId, Description, Status |
| **UserTracking** | Location history | TrackingId, UserId, Latitude, Longitude, Timestamp |
| **VisitHistory** | POI visit log | VisitId, UserId, PlaceId, CheckInTime, CheckOutTime |
| **Staff** | Admin/operator accounts | StaffId, Role, Department |
| **RefreshToken** | JWT token management | TokenId, UserId, Token, ExpirationDate |

---

## 6. User Journeys & Use Cases

### Use Case 1: Discover Nearby Places
```
1. User launches app
2. App requests location permission & starts GPS
3. User sees interactive map with nearby POIs
4. User taps on place marker
5. Detailed place view displays with info, reviews, distance
6. Geofence triggers → Narration auto-plays if available
7. User can add to favorites or view more details
```

### Use Case 2: Take a Guided Tour
```
1. User browses available tours on Tours page
2. Selects a tour of interest
3. Views full itinerary with stop locations
4. Starts tour navigation
5. Receives location-based narration at each stop
6. Can explore off-route or remain on guided path
7. Completes tour and gets summary with achievements
```

### Use Case 3: QR Code Check-in
```
1. User sees QR code at attraction
2. Opens QR scanner in TourGuideAPP
3. Scans code → redirects to place detail
4. Automatic check-in recorded in VisitHistory
5. User can leave review or add to wishlist
6. Business receives foot-traffic analytics
```

### Use Case 4: Plan Future Trip
```
1. User creates wishlist titled "Summer 2026 Trip"
2. Adds places of interest while browsing
3. Shares wishlist with travel companions
4. Exports as itinerary (PDF/Calendar format)
5. On-trip: Uses wishlist as checklist
```

---

## 7. Technical Requirements

### 7.1 Mobile App Requirements
- **Minimum OS Versions**:
  - Android 21.0+
  - iOS 15.0+
  - macOS 15.0+ (Catalyst)
  - Windows 10.19041.0+
- **Minimum RAM**: 2GB
- **Screen Support**: Phones (4.5"-6.5"), Tablets, Desktop displays
- **Battery**: Optimized for 8+ hours typical use with location tracking

### 7.2 Backend Requirements
- **Runtime**: .NET 10.0 or higher
- **Database**: PostgreSQL 12+
- **API Response Time**: <500ms for 95th percentile
- **Uptime SLA**: 99.5%
- **Concurrent Users**: Support 10,000+ simultaneous connections

### 7.3 Performance Metrics
- App startup time: <2 seconds
- Map loading: <1 second for standard view
- Geofence triggering: <30 second detection latency
- API rate limiting: 1000 requests/min per user

### 7.4 Security Requirements
- JWT tokens with 1-hour expiration
- Refresh tokens for long-lived sessions
- HTTPS/TLS 1.3+ for all API communication
- Password hashing with bcrypt/PBKDF2
- GDPR compliance for user data
- Role-based access control (RBAC)

---

## 8. Integrations & APIs

### 8.1 External Services
- **Maps**: Mapsui for map rendering and tile services
- **Geolocation**: MAUI GeolocationAPI & native platform APIs
- **QR Code**: ZXing.Net for barcode generation/reading
- **Database**: Supabase PostgreSQL client
- **Text-to-Speech**: Platform native APIs (Android TTS, iOS AVFoundation)
- **Real-time**: SignalR for WebSocket connections

### 8.2 API Endpoints (Backend)
- `/api/auth/register` - User registration
- `/api/auth/login` - User login
- `/api/places` - Place CRUD operations
- `/api/pois` - POI listing and details
- `/api/tours` - Tour management
- `/api/reviews` - Review submission/listing
- `/api/favorites` - Manage favorites
- `/api/wishlists` - Manage wishlists
- `/api/complaints` - Submit complaints
- `/api/tracking/location` - Submit location data
- `/api/promotions` - Active promotions

---

## 9. Design Principles

### 9.1 User Experience
- **Location-First Design**: All features center around user location context
- **Contextual Information**: Show only relevant info based on proximity and interests
- **Always Online, Graceful Offline**: Core features work offline for cached data
- **Accessibility First**: WCAG 2.1 AA compliance
- **Mobile-First**: Responsive design for all screen sizes

### 9.2 Technical
- **Modular Architecture**: Services are independent and testable
- **Dependency Injection**: All services use DI for flexibility
- **Async/Await Pattern**: Non-blocking operations throughout
- **Error Resilience**: Graceful error handling and retry logic
- **Data Privacy by Design**: Minimal data collection, user consent-based

---

## 10. Roadmap & Phases

### Phase 1: MVP (Current - Q2 2026)
- ✅ User authentication
- ✅ Place discovery & map view
- ✅ Real-time location tracking
- ✅ QR code scanning
- ✅ Favorites & wishlist
- ✅ Basic reviews system

### Phase 2: Enhanced Tours (Q3 2026)
- [ ] Guided tour creation tools for guides
- [ ] Group tour real-time following
- [ ] Advanced narration with multi-language
- [ ] Tour completion badges/achievements
- [ ] Offline tour maps

### Phase 3: Social & Community (Q4 2026)
- [ ] Social profiles & follower system
- [ ] Community challenges
- [ ] User-generated tour routes
- [ ] Real-time chat in tour groups
- [ ] Social sharing integrations

### Phase 4: Business Tools (Q1 2027)
- [ ] Business owner dashboard
- [ ] Advanced analytics & reporting
- [ ] Promotional campaign management
- [ ] Staff/guide management portal
- [ ] Revenue sharing system

### Phase 5: AI & Personalization (Q2 2027)
- [ ] AI-powered tour recommendations
- [ ] Predictive narration (pre-loading)
- [ ] Smart route optimization
- [ ] Personalized notifications
- [ ] Sentiment analysis on reviews

---

## 11. Success Metrics

### User Adoption
- Download targets: 100K+ in first year
- Monthly active users (MAU): 30%+ of downloads
- User retention rate: >40% at 30 days

### Engagement
- Average session duration: >15 minutes
- Daily active users (DAU): >20K by end of year
- Places discovered per session: >3 POIs
- Reviews submitted per 100 users: >8

### Business
- API availability: 99.5% uptime
- Mean response time: <400ms
- Support ticket resolution: <24 hours
- User satisfaction score: >4.2/5.0 stars

---

## 12. Constraints & Assumptions

### Constraints
- Limited budget for server infrastructure (leveraging Supabase)
- Small initial team (3-4 developers)
- Must launch on iOS & Android simultaneously
- Battery consumption concerns with continuous GPS

### Assumptions
- Users have 3G+ internet connectivity minimum
- Users grant location permissions voluntarily
- Tourism operators will provide accurate, updated content
- Market demand exists for location-based tourism app

---

## 13. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|-----------|
| GPS accuracy in urban canyons | Geofence false triggers | Medium | Test in major cities, adjust fence radius |
| Supabase scaling limitations | Service degradation | Low | Plan for PostgreSQL migration if needed |
| User privacy backlash | Regulatory issues | Medium | Transparent privacy policy, GDPR/CCPA compliance |
| Competitor apps | Market share loss | High | Focus on unique narration + discovery features |
| Battery drain concerns | User churn | Medium | Implement battery saver mode, optimize tracking |

---

## 14. Competitors & Differentiation

### Direct Competitors
- **Google Maps**: Location-finding (no narration)
- **TripAdvisor**: Reviews & planning (no real-time guidance)
- **Citymapper**: Urban navigation (no tourism focus)

### Differentiating Factors
✅ **Auto-triggered contextual narration** - Unique immersive experience  
✅ **Multi-platform MAUI app** - Consistent experience across devices  
✅ **QR integration** - Physical-to-digital bridge  
✅ **Geofence-based automation** - Hands-free exploration  
✅ **Community-driven content** - Reviews + user recommendations  

---

## 15. Compliance & Legal

- **Privacy**: GDPR, CCPA compliance required
- **Accessibility**: WCAG 2.1 AA standard
- **Content**: User moderation for reviews/complaints
- **Liability**: Terms of service covering location privacy, data usage
- **Licensing**: Open-source components properly attributed

---

## 16. Post-Launch Considerations

### Monitoring & Analytics
- Real-time user metrics dashboard
- Server performance monitoring (CPU, memory, DB)
- Crash reporting & error tracking
- User session analytics
- Feature usage heatmaps

### Iterative Improvement
- A/B testing framework for UI changes
- User feedback collection mechanisms
- Monthly performance reviews
- Quarterly roadmap adjustments based on usage data

### Support & Documentation
- In-app help & FAQ section
- User community forums
- API documentation for partners
- Video tutorials for key features

---

## Appendix A: Glossary

| Term | Definition |
|------|-----------|
| **POI** | Point of Interest - specific notable location |
| **Geofence** | Virtual boundary that triggers actions when crossed |
| **QR Code** | Quick Response code for encoded information |
| **JWT** | JSON Web Token for stateless authentication |
| **MAUI** | Multi-platform App UI framework for .NET |
| **Supabase** | Open-source Firebase alternative (PostgreSQL backend) |
| **SignalR** | ASP.NET technology for real-time bidirectional communication |
| **WCAG** | Web Content Accessibility Guidelines |
| **DAU** | Daily Active Users |
| **MAU** | Monthly Active Users |

---

**Document Owner**: Product Team  
**Last Updated**: March 23, 2026  
**Next Review**: Q2 2026  
**Version History**: v1.0 - Initial PRD Creation
