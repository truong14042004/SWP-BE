# PHÂN TÍCH API CÒN THIẾU - CAREER ORIENTATION PLATFORM

## ĐÃ IMPLEMENT (12 Controllers)

### 1. AuthController ✅
- POST /api/auth/register
- POST /api/auth/verify-email
- POST /api/auth/login
- POST /api/auth/google
- GET /api/auth/me
- POST /api/auth/logout

### 2. ProfileController ✅
- GET /api/profile
- POST /api/profile
- PUT /api/profile

### 3. UserSkillsController ✅
- GET /api/user-skills
- POST /api/user-skills
- PUT /api/user-skills/{id}
- DELETE /api/user-skills/{id}

### 4. SkillsController ✅
- GET /api/skills
- POST /api/skills [Admin]
- PUT /api/skills/{id} [Admin]
- DELETE /api/skills/{id} [Admin]

### 5. CareerRolesController ✅
- GET /api/career-roles
- GET /api/career-roles/{id}
- POST /api/career-roles [Admin]
- PUT /api/career-roles/{id} [Admin]
- POST /api/career-roles/select [Student]

### 6. SkillGapsController ✅
- POST /api/skill-gap/analyze
- GET /api/skill-gap/latest
- GET /api/skill-gap/{id}

### 7. RoadmapController ✅
- POST /api/roadmap/generate
- GET /api/roadmap
- GET /api/roadmap/{id}
- PUT /api/roadmap-node/{id}/status

### 8. GithubController ✅
- POST /api/github/oauth/login
- GET /api/github/oauth/callback
- POST /api/github/sync
- GET /api/github/repositories
- POST /api/github/analyze-readme

### 9. PortfolioController ✅
- GET /api/portfolio/me
- POST /api/portfolio
- PUT /api/portfolio
- POST /api/portfolio/publish
- GET /api/portfolio/{slug}

### 10. MentorController (AI) ✅
- POST /api/mentor/chat
- GET /api/mentor/sessions
- GET /api/mentor/sessions/{id}

### 11. AdminController ✅
- GET /api/admin/skills
- POST /api/admin/skills
- PUT /api/admin/skills/{id}
- DELETE /api/admin/skills/{id}
- GET /api/admin/learning-resources
- POST /api/admin/learning-resources
- PUT /api/admin/learning-resources/{id}
- DELETE /api/admin/learning-resources/{id}
- GET /api/admin/role-skill-requirements
- POST /api/admin/role-skill-requirements
- PUT /api/admin/role-skill-requirements/{id}
- DELETE /api/admin/role-skill-requirements/{id}

### 12. StorageController ✅
- POST /api/storage/upload
- POST /api/storage/import-url
- POST /api/storage/avatar
- POST /api/storage/avatar/import-url
- POST /api/storage/user-skills/{id}/evidence
- POST /api/storage/user-skills/{id}/evidence/import-url
- POST /api/storage/portfolio-projects/{id}/image
- POST /api/storage/portfolio-projects/{id}/image/import-url
- GET /api/storage/download
- GET /api/storage/signed-url
- GET /api/storage/public/portfolio-projects/{id}/image
- GET /api/storage/public/portfolio-projects/{id}/image/download
- DELETE /api/storage

---

## ✅ ĐÃ IMPLEMENT THÊM (cap nhat 2026-05-20)

### A. Student APIs

#### 1. Learning Resources (Student View) ✅
- `GET /api/learning-resources` (`LearningResourcesController`)
- `GET /api/learning-resources/{id}`

#### 2. Portfolio Unpublish ✅
- `POST /api/portfolio/unpublish` (PortfolioController line 169)

---

### B. COUNSELOR APIs ✅

**Controller:** `CounselorController` (11 endpoints)

- `GET /api/counselor/students` — danh sách sinh viên được phân công
- `GET /api/counselor/students/{id}/profile`
- `GET /api/counselor/students/{id}/skills`
- `GET /api/counselor/students/{id}/skill-gap` + `/latest` + `/skill-gaps` + `/skill-gap/{reportId}`
- `GET /api/counselor/students/{id}/roadmap`
- `POST /api/counselor/feedback` body: `{ studentId, feedbackText, rating, recommendations, privateNotes, roadmapId?, skillGapReportId? }`
- `GET /api/counselor/feedback`
- `GET /api/counselor/students/{id}/feedback`

**Database:** `CounselorFeedback` đã có đủ field structured.

---

### C. INDUSTRY MENTOR APIs ✅

**Controller:** `IndustryMentorController` (7 endpoints)

- `GET /api/industry-mentor/review-queue`
- `GET /api/industry-mentor/students/{id}/portfolio`
- `GET /api/industry-mentor/students/{id}/github`
- `GET /api/industry-mentor/students/{id}/quota` — kiểm tra quota review
- `POST /api/industry-mentor/feedback` body: `{ studentId, portfolioId?, githubRepositoryId?, comment, rating?, portfolioQualityFeedback?, technicalSkillsAssessment?, projectQualityFeedback?, recommendations?, jobReadinessLevel? }`
- `GET /api/industry-mentor/feedback`
- `GET /api/industry-mentor/students/{id}/feedback`

**Database:** `MentorFeedback` đã có 5 field structured (PortfolioQualityFeedback, TechnicalSkillsAssessment, ProjectQualityFeedback, Recommendations, JobReadinessLevel).

**Quota:** parse từ `SubscriptionPlan.FeaturesJson.mentorReviewLimit`. Free plan default 2 lượt. Hết quota trả `402 Payment Required`.

**Lưu ý:** Khác với AI MentorController (chat bot)

---

### D. ADMIN APIs ✅

#### 1. User Management ✅
- `GET /api/admin/users` (`AdminUsersController`)
- `GET /api/admin/users/{id}`
- Update role + status

#### 2. Counselor Assignments ✅
- `AdminCounselorAssignmentsController` (gan counselor cho student)

#### 3. Dashboard Statistics ✅
- `GET /api/admin/stats/overview`
- `GET /api/admin/stats/users`
- `GET /api/admin/stats/payments`
- `GET /api/admin/stats/subscriptions`
- `GET /api/admin/stats/learning-resources`
- `GET /api/admin/stats/career-roles`
- `GET /api/admin/stats/daily`

---

### E. PAYMENT & SUBSCRIPTION APIs ✅

**Controllers:**
- `SubscriptionsController` (student-side)
- `PayOsController` (PayOS gateway)
- `AdminSubscriptionPlansController`
- `AdminPaymentsController`

**Đã hỗ trợ:**
- Xem các gói dịch vụ
- Tạo checkout session qua PayOS
- Webhook xử lý kết quả thanh toán
- Xem subscription hiện tại
- Hủy subscription
- Admin quản lý plans + payments

---

### F. NOTIFICATION APIs ❌ (chua implement)

**Mục đích:** Thông báo cho user

- `GET /api/notifications` — lay danh sach notification
- `PUT /api/notifications/{id}/read` — danh dau da doc
- `PUT /api/notifications/read-all` — danh dau tat ca da doc

**Database table:** Chua co model `Notification`. Can tao moi.

**Trigger goi y:**
- Counselor gui feedback → student nhan notification.
- Industry Mentor gui feedback → student nhan notification.
- Subscription chuyen Active sau thanh toan → student nhan notification.
- Skill gap analyze xong → student nhan notification.

---

### G. DASHBOARD APIs ⚠️ (data co qua endpoint khac)

#### 1. Student Dashboard
FE hien tai ghep tu cac endpoint co san:
- `/api/profile` (target role)
- `/api/skill-gap/latest` (match score)
- `/api/roadmap` (progress)
- `/api/github/repositories` (repos count)

Neu can endpoint tong hop, co the them `GET /api/dashboard/student` sau.

#### 2. Counselor Dashboard
FE goi `/api/counselor/students` + `/api/counselor/feedback` da co du data.

#### 3. Mentor Dashboard
FE goi `/api/industry-mentor/review-queue` + `/api/industry-mentor/feedback`.

---

### H. SEARCH APIs ✅

- `GET /api/search/skills` (`SearchController`)
- `GET /api/search/learning-resources`

---

### I. ANALYTICS / MARKET PULSE ❌ (chua implement)

**Muc dich:** Phan tich xu huong ky nang tren thi truong viec lam (Market Pulse module).

**Trang thai:** Chua co. Doc nghiep vu §16 mo ta day la module phai bo sung de dap ung yeu cau day du.

Can bo sung:
- Job sources, job posts.
- Daily scraping/background job.
- Keyword frequency analysis.
- Skill mention records va skill trends aggregate.
- API chart cho FE: top trending, growth/decline, theo ngay/tuan/thang.

---

## 📊 TỔNG KẾT (cap nhat 2026-05-20)

| Phần | Trạng thái |
|---|---|
| A. Student core (profile, skills, gap, roadmap, mentor AI, github, portfolio) | ✅ |
| B. Counselor (11 endpoints) | ✅ |
| C. Industry Mentor (7 endpoints, structured feedback, quota) | ✅ |
| D. Admin (users, stats, plans, payments) | ✅ |
| E. Payment & Subscription (PayOS) | ✅ |
| F. Notifications | ❌ |
| G. Dashboard endpoints chuyen dung | ⚠️ (cover qua endpoint khac) |
| H. Search | ✅ |
| I. Market Pulse / Analytics | ❌ |

### Còn thiếu

1. **Notification module** (3 endpoint + table moi).
2. **Market Pulse** (Section §16 trong doc nghiep vu).
3. (Optional) Dashboard tong hop endpoint neu FE muon.

---

## KẾT LUẬN

**Core features (MVP) đã hoàn thành ~90%**

Nhung gi con thieu deu thuoc nhom **nice-to-have**:
- **Notifications** — co the lam sau khi co user feedback.
- **Market Pulse** — module rieng, chua block flow chinh cua sinh vien.

**Recommendation:** 
- Uu tien on dinh va polish UX cho Counselor + Industry Mentor (vua hoan thien).
- Notification co the lam minimal: chi can `Notifications` table + endpoint, trigger trong cac action handler hien co.
- Market Pulse de qua phase sau.

