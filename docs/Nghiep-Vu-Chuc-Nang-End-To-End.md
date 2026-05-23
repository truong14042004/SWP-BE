# Tai lieu nghiep vu chuc nang end-to-end

## 1. Muc tieu he thong

CareerMap la nen tang dinh huong nghe nghiep cho sinh vien IT. He thong giup sinh vien:

- Chon target career role.
- Nhap ky nang hien co.
- Phan tich skill gap theo yeu cau cua role.
- Sinh roadmap hoc tap dang hierarchical skill tree.
- Xem tai nguyen hoc tap theo tung roadmap node.
- Hoi AI Virtual Mentor de nhan tu van ca nhan hoa.
- Dong bo GitHub, phan tich README va tao e-portfolio.
- Mua goi de su dung mentor review/payment.

He thong cung cap cong cu cho admin de van hanh du lieu nen tang: user, skill, career role, role skill requirement, learning resource, subscription plan, payment, dashboard statistics.

## 2. Actor va pham vi su dung

### 2.1 Guest

Guest la nguoi chua dang nhap.

Guest co the:

- Xem trang home/gioi thieu he thong.
- Xem goi dang ky neu FE cho phep public.
- Dang ky tai khoan bang email/password.
- Dang nhap bang email/password hoac Google OAuth.

Guest khong duoc:

- Sinh roadmap.
- Xem learning resource ca nhan hoa trong roadmap.
- Chat voi AI Mentor.
- Tao portfolio.
- Thanh toan goi khi chua dang nhap.

### 2.2 Student

Student la actor chinh cua he thong.

Student co the:

- Cap nhat profile hoc tap va target career role.
- Upload avatar.
- Nhap current skills.
- Upload evidence cho skill.
- Phan tich skill gap.
- Tao roadmap hoc tap.
- Xem/download tai lieu trong tung roadmap node.
- Danh dau node roadmap la `InProgress`, `Completed`, `Verified`, `NeedReview`.
- Chat voi AI Virtual Mentor.
- Ket noi GitHub, sync repo, phan tich README bang AI.
- Tao, cap nhat, publish e-portfolio.
- Mua goi subscription qua PayOS.
- Xem lich su goi dang ky cua minh.

### 2.3 Admin

Admin van hanh toan bo du lieu nen va cau hinh he thong.

Admin co the:

- CRUD user.
- Doi role/status user.
- Upload avatar cho user.
- CRUD skills.
- CRUD career roles.
- CRUD role skill requirements.
- CRUD learning resources bang link.
- Upload learning resource bang file vao bucket.
- CRUD subscription plans.
- Xem/quan ly payments, subscriptions, invoices.
- Xem dashboard statistics: users, payments, subscriptions, learning resources, career roles.

Nguyen tac quan trong:

- Khi admin xoa user, lich su payment khong bi xoa.
- Khi admin xoa skill/learning resource/career role, phai kiem tra rang buoc du lieu da duoc su dung.
- Learning resource nen duoc tao theo skill va do kho tu co ban den nang cao.

### 2.4 Academic Counselor

Academic Counselor ho tro sinh vien theo huong hoc thuat.

Counselor can co cac nghiep vu:

- Xem danh sach sinh vien duoc phan cong.
- Xem profile sinh vien.
- Xem skill cua sinh vien.
- Xem skill gap report.
- Xem roadmap.
- Tao counselor feedback.
- Xem lich su feedback.

Ghi chu MVP:

- Database da co `CounselorFeedback`.
- Neu chua co controller day du, day la phan can hoan thien de dung dung role.

### 2.5 Industry Mentor

Industry Mentor danh gia kha nang san sang viec lam cua sinh vien.

Mentor can co cac nghiep vu:

- Xem review queue.
- Xem portfolio cua sinh vien.
- Xem GitHub repositories cua sinh vien.
- Tao mentor feedback.
- Danh gia portfolio quality, technical skills, project quality, recommendations, job readiness level.
- Xem lich su feedback da tao.

Ghi chu MVP:

- Database da co `MentorFeedback`.
- Industry Mentor khac voi AI Virtual Mentor. AI Virtual Mentor la chatbot, Industry Mentor la user role that review portfolio.

### 2.6 External Systems

External systems gom:

- Google OAuth: xac thuc dang nhap Google.
- Gemini/LLM provider: sinh cau tra loi AI Mentor va phan tich GitHub README.
- GitHub API: sync public/private repositories neu co OAuth.
- Google Cloud Storage/Bucket: luu avatar, evidence, portfolio image, learning resource file.
- PayOS: tao checkout, redirect return/cancel, goi webhook thanh toan.

## 3. Luong 1: Dang ky, xac thuc, dang nhap

### Muc tieu

Dam bao user chi duoc ghi vao bang `users` sau khi xac thuc OTP thanh cong.

### Actor

Guest, Student, Google OAuth.

### Luong email/password

1. Guest nhap username, email, full name, password, confirm password.
2. FE goi `POST /api/auth/register`.
3. BE validate du lieu:
   - Password va confirm password phai khop.
   - Username/email chua ton tai trong `users`.
   - Neu co pending registration cu va het han thi xoa.
4. BE tao OTP va luu vao `pending_registrations`.
5. BE gui OTP qua email.
6. Guest nhap OTP.
7. FE goi `POST /api/auth/verify-email`.
8. BE verify OTP:
   - OTP dung.
   - OTP chua het han.
   - Username/email van chua ton tai trong `users`.
9. BE tao user moi trong `users` voi role mac dinh `Student`.
10. BE xoa pending registration.
11. BE tra access token va refresh token.
12. FE luu session va dieu huong theo role.

### Luong login

1. User nhap email/password.
2. FE goi `POST /api/auth/login`.
3. BE kiem tra user ton tai, active, password dung.
4. BE tra access token va refresh token.
5. FE decode role hoac goi `GET /api/auth/me`.
6. FE dieu huong:
   - `Admin` -> admin dashboard.
   - `Student` -> student dashboard/home.
   - `AcademicCounselor` -> counselor dashboard.
   - `IndustryMentor` -> mentor dashboard.

### Luong refresh token

1. Access token het han.
2. FE goi `POST /api/auth/refresh` bang refresh token.
3. BE validate refresh token.
4. BE revoke token cu va issue token moi.
5. FE tiep tuc request API bang access token moi.

## 4. Luong 2: Student setup profile va target career role

### Muc tieu

Lay du thong tin dau vao de phan tich skill gap va sinh roadmap.

### Actor

Student.

### Cac buoc

1. Student dang nhap.
2. Student cap nhat profile:
   - School.
   - Major.
   - Year.
   - GPA.
   - Career goal.
   - Preferred learning hours per week.
   - GitHub username.
3. Student chon target career role, vi du:
   - Backend Developer.
   - Frontend Developer.
   - DevOps Engineer.
   - Mobile Developer.
   - Data Engineer.
4. FE goi API profile/career role tuong ung.
5. BE luu `StudentProfile.TargetRoleId`.

### Dieu kien nghiep vu

- Student phai co target role truoc khi generate roadmap.
- Target role phai active.
- Target role nen co role skill requirements de skill gap va roadmap co du lieu tot.

## 5. Luong 3: Student khai bao skill va evidence

### Muc tieu

Xac dinh current skills cua sinh vien lam dau vao cho skill gap analysis.

### Actor

Student, Admin.

### Cac buoc student

1. Student xem danh sach predefined skills.
2. Student them skill minh co:
   - SkillId.
   - Level: Beginner, Intermediate, Advanced.
   - EvidenceUrl hoac evidence file.
3. Student co the upload evidence file vao bucket.
4. BE luu `user_skills`.
5. Neu co evidence, BE luu duong dan file/object URL vao skill evidence.

### Cac buoc admin

1. Admin quan ly bang skills.
2. Admin dam bao skill co category ro rang, vi category duoc dung de group roadmap tree.
3. Admin tao role skill requirements cho tung career role.

### Dieu kien nghiep vu

- Skill chua verified van co the duoc tinh la current skill, nhung roadmap co the yeu cau evidence/verification.
- Skill verified nen co trong tinh toan match score tot hon.

## 6. Luong 4: Skill Gap Analysis

### Muc tieu

So sanh ky nang hien co cua sinh vien voi yeu cau cua target career role.

### Actor

Student, Academic Counselor.

### Cac buoc

1. Student bam Analyze Skill Gap.
2. FE goi `POST /api/skill-gap/analyze`.
3. BE xac dinh career role:
   - Dung `CareerRoleId` tu request neu co.
   - Neu khong co, lay target role tu profile.
4. BE lay role skill requirements.
5. BE lay user skills cua student.
6. BE so sanh current level voi required level.
7. BE tao `SkillGapReport`.
8. BE tao cac `SkillGapReportItems`:
   - Skill.
   - CurrentLevel.
   - RequiredLevel.
   - Status.
   - Priority.
   - Recommendation.
9. BE tinh match score.
10. FE hien visual report:
   - Match score.
   - Missing skills.
   - Weak skills.
   - Priority list.
11. Counselor co the xem report de tu van.

### Dieu kien nghiep vu

- Neu career role khong co requirements thi khong the analyze.
- Report moi nhat duoc dung de sinh roadmap.
- PDF export la phan nen bo sung neu can dung dung FR3.3.

## 7. Luong 5: Dynamic Roadmap theo hierarchical skill tree

### Muc tieu

Sinh roadmap hoc tap co thu tu uu tien va co cau truc cha-con thay vi danh sach phang.

### Actor

Student, Admin.

### Cac buoc generate roadmap

1. Student da co target career role.
2. Student da co skill gap report hoac it nhat co target role.
3. Student bam Generate Roadmap.
4. FE goi `POST /api/roadmap/generate`.
5. BE xac dinh career role.
6. BE lay skill gap report moi nhat neu request khong truyen `SkillGapReportId`.
7. BE tao danh sach technical node:
   - Neu co skill gap: lay missing/weak skills tu report.
   - Neu khong co skill gap: lay role skill requirements.
   - Neu khong co du lieu: dung fallback nodes theo role.
8. BE lay active learning resources theo tung skill.
9. BE group nodes theo skill category hoac group name.
10. BE tao group node:
    - `NodeType = Group`.
    - `Level = 0`.
    - Khong gan learning resource.
11. BE tao child technical node:
    - `Level = 1`.
    - `ParentNodeId` tro ve group node.
    - `PrerequisiteNodeId` tro ve technical node truoc do de giu thu tu hoc.
    - Co mot hoac nhieu learning resources.
12. BE tra response:
    - `nodes`: flat list de tuong thich FE cu.
    - `nodeTree`: cay cha-con de FE moi render hierarchical skill tree.

### Cac buoc hoc roadmap

1. Student mo roadmap.
2. FE hien group node, vi du:
   - Foundation.
   - Backend Engineering.
   - Database.
   - Portfolio.
3. Student mo tung child node.
4. Student xem tai lieu:
   - Link external.
   - File download/signed URL tu bucket.
5. Student danh dau node:
   - `NotStarted`.
   - `InProgress`.
   - `Completed`.
   - `Verified`.
   - `NeedReview`.
6. FE goi `PUT /api/roadmap-node/{id}/status`.
7. BE kiem tra:
   - Khong cho update status group node.
   - Neu node co prerequisite thi prerequisite phai Completed/Verified truoc khi bat dau.
8. BE cap nhat status node.
9. BE tinh lai progress chi dua tren technical nodes, khong tinh group nodes.

### Dieu kien nghiep vu

- Group node chi la container, khong tinh progress.
- Technical node moi la don vi hoc tap.
- Moi technical node nen co it nhat 2 learning resources de dung FR2.3.
- Neu skill thieu resource, admin can bo sung learning resource theo skill.

## 8. Luong 6: Learning Resources

### Muc tieu

Dam bao student co tai lieu hoc tap theo tung roadmap node, co the la link hoac file.

### Actor

Admin, Student.

### Cac buoc admin tao resource link

1. Admin vao Learning Resources.
2. Chon skill.
3. Nhap title, URL, resource type, difficulty, estimated hours.
4. BE luu `learning_resources` voi `StorageObjectName = null`.
5. Resource co source type la `Link`.

### Cac buoc admin upload resource file

1. Admin chon skill.
2. Upload file PDF/doc/video/tai lieu.
3. BE upload file vao bucket.
4. BE luu metadata vao `learning_resources`:
   - Storage object name.
   - Content type.
   - File size.
   - Resource type.
   - Difficulty.
5. Resource co source type la `File`.

### Cac buoc student su dung resource

1. Student mo roadmap node.
2. FE hien danh sach `learningResources`.
3. Neu resource la link, student mo URL.
4. Neu resource la file, student goi download/signed-url API.
5. BE tra file hoac signed URL.

### Chuan du lieu nen ap dung

- Moi skill quan trong nen co toi thieu 2 resources.
- Nen co it nhat:
  - 1 resource Beginner/Documentation.
  - 1 resource Practice/Project/Advanced.
- Resource phai active moi duoc gan vao roadmap.

## 9. Luong 7: AI Virtual Mentor

### Muc tieu

Cho phep student hoi dap nghe nghiep/ky thuat va nhan cau tra loi ca nhan hoa.

### Actor

Student, LLM provider.

### Cac buoc

1. Student nhap cau hoi tu nhien.
2. FE goi `POST /api/mentor/chat`.
3. BE build context:
   - Profile.
   - Target role.
   - User skills.
   - Skill gap moi nhat.
   - Roadmap moi nhat.
   - GitHub repositories da phan tich.
   - Extra context tu request.
4. BE goi LLM provider.
5. LLM tra cau tra loi bang tieng Viet.
6. BE luu session vao `mentor_sessions`.
7. FE hien cau tra loi.
8. Student co the xem lai history.

### Dieu kien nghiep vu

- AI khong duoc invent thong tin khong co trong context.
- Neu thieu Gemini/LLM config, API phai tra loi loi dich vu ro rang.
- Transcript upload/analyze chua phai core luong hien tai, neu can dung FR1.3 day du thi bo sung module transcript.

## 10. Luong 8: GitHub sync va AI analyze README

### Muc tieu

Lay du lieu GitHub cua student de phuc vu AI Mentor va e-portfolio.

### Actor

Student, GitHub API, LLM provider.

### Cac buoc sync

1. Student nhap GitHub username hoac ket noi OAuth.
2. FE goi GitHub sync API.
3. BE goi GitHub API:
   - Public repos theo username.
   - Hoac repos co quyen OAuth.
4. BE luu/cap nhat `github_repositories`.
5. FE hien danh sach repo.

### Cac buoc analyze README

1. Student chon repo.
2. FE goi analyze README API.
3. BE lay README:
   - Tu request.
   - Tu DB neu da co.
   - Tu GitHub API neu chua co.
4. BE lay repository snapshot:
   - Metadata.
   - Language stats.
   - File tree sample.
   - Selected config/source files.
   - Recent commits neu co.
5. BE goi LLM provider de phan tich.
6. BE detect technologies.
7. BE luu:
   - README content.
   - AI summary.
   - Tech stack JSON.
   - Quality score.
8. BE map repository skills neu match voi skill trong he thong.

### Ket qua su dung

- AI Mentor co context GitHub tot hon.
- Portfolio co project summary va tech stack.
- Industry Mentor co co so review portfolio.

## 11. Luong 9: E-Portfolio Management

### Muc tieu

Sinh vien tao portfolio public de gui nha tuyen dung hoac mentor review.

### Actor

Student, Guest/Employer, Industry Mentor.

### Cac buoc student

1. Student tao portfolio.
2. Nhap slug, title, bio, theme.
3. Them projects:
   - GithubRepositoryId neu lien ket repo da sync.
   - Title.
   - Description.
   - Tech stack.
   - Image.
   - Demo URL.
   - Source URL.
4. Upload project image neu co.
5. BE luu `portfolios` va `portfolio_projects`.
6. Student publish portfolio.
7. BE set `IsPublished = true`.
8. Student chia se public URL theo slug.

### Cac buoc nguoi xem public

1. Guest/Employer mo URL public.
2. FE goi public portfolio API theo slug.
3. BE chi tra portfolio neu `IsPublished = true`.
4. FE hien portfolio va project.

### Dieu kien nghiep vu

- Slug phai unique.
- Portfolio chua publish khong public.
- Student nen co GitHub analyzed repo truoc khi publish de portfolio chat luong hon.

## 12. Luong 10: Subscription va PayOS payment

### Muc tieu

Cho phep student mua goi de co gioi han mentor review theo plan.

### Actor

Student, PayOS, Admin.

### Goi hien tai

- Free: 2 luot mentor review.
- Monthly: 5 luot mentor review.
- Khong can goi nam.

### Cac buoc checkout

1. Student mo trang mua goi.
2. FE goi `GET /api/subscription-plans`.
3. Student chon plan.
4. FE goi `POST /api/subscriptions/checkout`.
5. BE validate plan active.
6. BE tao subscription status `Pending`.
7. BE tao payment transaction status `Pending`.
8. BE goi PayOS tao payment link.
9. BE luu checkout URL.
10. FE redirect student sang PayOS checkout.

### Cac buoc sau thanh toan

1. PayOS redirect ve return URL neu thanh cong hoac cancel URL neu huy.
2. PayOS goi webhook ve BE.
3. BE verify webhook signature/checksum.
4. BE tim payment transaction theo order code/provider transaction.
5. Neu thanh toan thanh cong:
   - Payment -> `Paid`.
   - Subscription -> `Active`.
   - Set `StartedAt`, `ExpiredAt`.
6. Neu that bai:
   - Payment -> `Failed`.
   - Subscription -> failed/cancelled tuong ung.
7. FE goi API lay subscription moi nhat de cap nhat UI.

### Cac buoc admin quan ly payment

1. Admin xem danh sach payments.
2. Loc theo user/status/provider/date.
3. Xem subscriptions.
4. Cap nhat status payment khi can xu ly thu cong.
5. Xem dashboard revenue.

### Dieu kien nghiep vu

- Payment history phai duoc giu lai ke ca khi user bi xoa.
- Webhook URL phai tro ve BE, khong phai FE.
- Return URL va cancel URL tro ve FE.
- PayOS keys va URL phai nam trong env BE.

## 13. Luong 11: Admin van hanh du lieu nen

### 13.1 Quan ly user

1. Admin xem danh sach user.
2. Admin tao user neu can.
3. Admin cap nhat full name, email, role, status.
4. Admin upload avatar cho user.
5. Admin deactivate user neu user vi pham.
6. Admin xoa user neu can, nhung khong xoa payment history.

### 13.2 Quan ly skills

1. Admin tao skill.
2. Nhap name, category, description, active status.
3. Skill category can ro rang vi du:
   - Backend.
   - Frontend.
   - Database.
   - DevOps.
   - Career.
4. Skill duoc dung cho:
   - User skills.
   - Role skill requirements.
   - Learning resources.
   - Roadmap group.

### 13.3 Quan ly career roles

1. Admin tao career role.
2. Nhap name, description, level, active status.
3. Admin gan role skill requirements.
4. Role co requirements moi generate skill gap/roadmap chinh xac.

### 13.4 Quan ly role skill requirements

1. Admin chon career role.
2. Admin chon skill.
3. Admin nhap required level.
4. Admin nhap priority va weight.
5. BE dung priority de sap xep skill gap va roadmap.

### 13.5 Quan ly learning resources

1. Admin chon skill.
2. Admin tao link hoac upload file.
3. Admin gan difficulty.
4. Admin active/inactive resource.
5. Student chi thay active resource.

### 13.6 Quan ly subscription plans

1. Admin tao plan.
2. Nhap ten plan, price, currency, billing cycle.
3. Nhap mentor review limit trong features.
4. Active/inactive plan.
5. Student chi mua duoc active plan.

### 13.7 Dashboard statistics

Admin theo doi:

- So user theo role/status.
- Doanh thu va payment status.
- Subscription active/pending/cancelled.
- So learning resources theo skill.
- Career roles duoc chon nhieu.

## 14. Luong 12: Academic Counselor operation

### Muc tieu

Counselor ho tro sinh vien bang feedback hoc thuat dua tren profile, skill gap va roadmap.

### Cac buoc

1. Counselor dang nhap, JWT giai phong role `Counselor`.
2. FE dieu huong vao counselor dashboard.
3. Counselor goi `GET /api/counselor/students` xem danh sach student duoc phan cong.
4. Counselor chon student. FE goi song song:
   - `GET /api/counselor/students/{id}/profile`.
   - `GET /api/counselor/students/{id}/skills`.
   - `GET /api/counselor/students/{id}/skill-gap/latest` hoac `skill-gaps` (history) hoac `skill-gap/{reportId}` (chi tiet).
   - `GET /api/counselor/students/{id}/roadmap`.
   - `GET /api/counselor/students/{id}/feedback` xem lich su feedback da gui cho student do.
5. Counselor goi `POST /api/counselor/feedback` voi payload:
   - `studentId` (bat buoc).
   - `feedbackText` (bat buoc).
   - `rating` 1-5 (optional).
   - `recommendations` (optional).
   - `privateNotes` (optional, chi counselor xem).
   - `roadmapId` hoac `skillGapReportId` (optional, link feedback toi resource cu the).
6. BE luu `CounselorFeedback`.
7. Student xem feedback (truong `privateNotes` luon bi loc khoi response cua student API).

### Trang thai hien tai

- Controller: `CounselorController` da co 11 endpoint (students list/detail, profile, skills, skill-gap latest/history/by-id, roadmap, feedback POST/list mentor/list theo student).
- Model: `CounselorFeedback` da co `FeedbackText, Rating, Recommendations, PrivateNotes, RoadmapId, SkillGapReportId`.

## 15. Luong 13: Industry Mentor operation

### Muc tieu

Industry Mentor review portfolio va danh gia job readiness.

### Cac buoc

1. Mentor dang nhap, JWT chi giai phong cho role `IndustryMentor`.
2. FE dieu huong vao mentor dashboard.
3. Mentor goi `GET /api/industry-mentor/review-queue` xem danh sach sinh vien co portfolio da publish.
4. Mentor chon student can review.
5. FE goi song song:
   - `GET /api/industry-mentor/students/{id}/portfolio` lay portfolio publish.
   - `GET /api/industry-mentor/students/{id}/github` lay danh sach repo + AI summary + tech stack.
   - `GET /api/industry-mentor/students/{id}/feedback` lay feedback cua mentor da gui truoc do.
   - `GET /api/industry-mentor/students/{id}/quota` xem con bao nhieu luot review co the gui.
6. Mentor goi `POST /api/industry-mentor/feedback` voi payload structured:
   - `studentId`, `portfolioId`, `githubRepositoryId` (optional reference).
   - `comment` (bat buoc, free text tom tat tong the).
   - `rating` 1-5 (overall rating).
   - `portfolioQualityFeedback` (chat luong portfolio).
   - `technicalSkillsAssessment` (danh gia ky nang ky thuat).
   - `projectQualityFeedback` (chat luong project).
   - `recommendations` (khuyen nghi cu the).
   - `jobReadinessLevel`: `NotReady | NeedsImprovement | Ready | Excellent`.
7. BE validate:
   - `comment` khong duoc rong.
   - `rating` phai trong 1-5 neu co.
   - `portfolioId` va `githubRepositoryId` phai thuoc ve `studentId`.
   - `jobReadinessLevel` phai nam trong enum cho phep.
   - **Quota check**: lay subscription active cua sinh vien, parse `mentorReviewLimit` tu `FeaturesJson`. Mac dinh sinh vien khong co subscription co quota Free = 2 review. Neu `Used >= Limit` tra `402 Payment Required`.
8. BE luu `MentorFeedback` voi 5 field structured.
9. Sinh vien xem feedback chi tiet tren dashboard cua minh.

### Trang thai hien tai

- Controller: `IndustryMentorController` da co full 7 endpoint (review-queue, portfolio, github, quota, feedback POST, feedback list mentor, feedback list theo student).
- Model: `MentorFeedback` co 5 field structured (PortfolioQualityFeedback, TechnicalSkillsAssessment, ProjectQualityFeedback, Recommendations, JobReadinessLevel).
- Quota: tinh tu `Subscriptions` active + `FeaturesJson.mentorReviewLimit`. Free plan default 2 luot/period.
- Khac voi AI Virtual Mentor: AI Mentor la chatbot Gemini, Industry Mentor la real user role da xac thuc.

## 16. Luong 14: Market Pulse

### Muc tieu

Theo doi xu huong ky nang tren thi truong viec lam.

### Trang thai MVP

Market Pulse chua phai module core da day du. De dung requirement day du, can bo sung:

- Job sources.
- Job posts.
- Daily scraping/background job.
- Keyword frequency analysis.
- Skill trends.
- API chart cho FE.

### Luong de xuat

1. He thong chay scheduled job moi ngay.
2. Job lay tin tu nguon hop le, vi du TopCV/VietnamWorks/CSV import.
3. BE luu job posts.
4. BE normalize description.
5. BE match keyword voi bang skills.
6. BE tao skill mention records.
7. BE aggregate daily skill trends.
8. FE hien:
   - Top trending skills.
   - Skill growth/decline.
   - Chart theo ngay/tuan/thang.

## 17. Quyen truy cap tong quat

| Chuc nang | Guest | Student | Admin | Counselor | Industry Mentor |
|---|---:|---:|---:|---:|---:|
| Home | Co | Co | Co | Co | Co |
| Register/Login | Co | Co | Co | Co | Co |
| Profile | Khong | Co | Quan ly user | Xem student | Xem neu review |
| Skill input | Khong | Co | Quan ly skill | Xem | Xem neu review |
| Skill gap | Khong | Co | Xem/Thong ke | Xem/Tu van | Xem neu review |
| Roadmap | Khong | Co | Xem/Thong ke | Xem/Tu van | Xem neu review |
| Learning resources | Public tuy FE | Co | CRUD | Xem | Xem |
| AI Mentor chat | Khong | Co | Khong can | Khong can | Khong can |
| GitHub sync/analyze | Khong | Co | Khong can | Xem | Xem |
| Portfolio | Public neu publish | CRUD/Publish | Xem neu can | Xem | Review |
| Payment | Khong | Checkout | Quan ly | Khong | Khong |
| Admin dashboard | Khong | Khong | Co | Khong | Khong |

## 18. Checklist nghiem thu end-to-end

### Student journey

- Dang ky email/password khong tao user truoc OTP.
- Verify OTP tao user thanh cong.
- Login theo role dung dashboard.
- Cap nhat profile va target role.
- Them current skills.
- Upload avatar/evidence thanh cong vao bucket.
- Analyze skill gap thanh cong.
- Generate roadmap thanh cong.
- Roadmap co `nodeTree` group/children.
- Technical node co learning resources.
- Student download/xem resource file/link.
- Update node status cap nhat progress.
- Chat AI Mentor co context ca nhan.
- Sync GitHub repo.
- Analyze README bang AI.
- Tao va publish portfolio.
- Public portfolio xem duoc theo slug.
- Mua plan PayOS.
- Webhook active subscription sau thanh toan.

### Admin journey

- CRUD user.
- Doi role/status user.
- Upload avatar user.
- CRUD skills.
- CRUD career roles.
- CRUD role skill requirements.
- CRUD learning resources link/file.
- CRUD subscription plans.
- Xem payment/subscription.
- Dashboard stats khong loi LINQ/runtime.
- Xoa user khong xoa payment history.

### Counselor journey

- Login role counselor.
- Xem student assigned.
- Xem profile/skill gap/roadmap.
- Tao counselor feedback.
- Xem feedback history.

### Industry Mentor journey

- Login role industry mentor.
- Xem review queue.
- Xem portfolio/GitHub repo.
- Tao mentor feedback.
- Ghi nhan job readiness.
- Tru/kiem tra mentor review usage neu subscription co gioi han.

## 19. Cac diem can hoan thien de dung SRS day du

1. Transcript upload/analyze cho AI Mentor.
2. Dam bao moi technical roadmap node co it nhat 2 curated learning resources.
3. PDF export cho skill gap report.
4. Market Pulse scraping + trend chart.
5. CounselorController day du neu chua co.
6. IndustryMentorController day du neu chua co.
7. FE render `nodeTree` de hien hierarchical skill tree dung nghiep vu.

