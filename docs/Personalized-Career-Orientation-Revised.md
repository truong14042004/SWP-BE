# Personalized Career Orientation & Learning Roadmap Platform

## 1. Tổng Quan Dự Án

### 1.1 Tên Dự Án

**Personalized Career Orientation & Learning Roadmap Platform for Software Engineering Students**

Tên tiếng Việt đề xuất:

**Nền tảng định hướng nghề nghiệp và xây dựng lộ trình học cá nhân hóa cho sinh viên Công nghệ/Kỹ thuật Phần mềm**

### 1.2 Mô Tả Dự Án

Dự án là một nền tảng web hỗ trợ sinh viên ngành Software Engineering định hướng nghề nghiệp, phân tích khoảng cách kỹ năng, xây dựng lộ trình học cá nhân hóa, theo dõi tiến độ học tập, phân tích GitHub portfolio và tư vấn nghề nghiệp thông qua AI Mentor.

Hệ thống giúp sinh viên trả lời các câu hỏi quan trọng:

- Tôi phù hợp với hướng nghề nào?
- Tôi đang thiếu kỹ năng gì so với vị trí mục tiêu?
- Tôi nên học gì trước?
- Tôi cần làm project nào để tăng khả năng xin internship hoặc fresher job?
- GitHub/Portfolio của tôi đã đủ tốt chưa?
- Tôi nên cải thiện kỹ năng và project theo thứ tự nào?

Ngoài core flow dành cho sinh viên, hệ thống có thể mở rộng thành nền tảng có gói dịch vụ:

- Free Plan
- Premium Plan
- Mentor Review Plan
- AI Career Coaching Plan
- Portfolio Pro Plan
- Institution Plan

## 2. Bối Cảnh Và Vấn Đề Cần Giải Quyết

### 2.1 Bối Cảnh

Ngành Software Engineering có nhiều hướng nghề khác nhau:

- Backend Developer
- Frontend Developer
- Fullstack Developer
- Mobile Developer
- DevOps Engineer
- Data Engineer
- QA Automation Engineer
- Cloud Engineer
- AI Application Developer

Sinh viên thường học nhiều môn nền tảng nhưng chưa biết nên đi theo hướng nào. Khi gần đi thực tập hoặc ra trường, sinh viên thường gặp các vấn đề:

- Không biết mình phù hợp với vị trí nào.
- Không biết thị trường đang yêu cầu kỹ năng gì.
- Không biết học theo roadmap nào.
- Không biết portfolio hiện tại đã đủ tốt chưa.
- Không có mentor cá nhân để hỏi định hướng.
- Không biết cách biến GitHub project thành hồ sơ ứng tuyển tốt.

### 2.2 Vấn Đề Chính

#### Vấn Đề 1: Skill Gap

Sinh viên có thể đã học nhiều môn nhưng vẫn thiếu kỹ năng thực tế để ứng tuyển.

Ví dụ sinh viên biết Java cơ bản nhưng chưa biết:

- Spring Boot
- REST API
- JWT Authentication
- Docker
- Unit Testing
- Deployment

Điều này khiến sinh viên khó đạt yêu cầu của các vị trí thực tập hoặc fresher.

#### Vấn Đề 2: Choice Paralysis

Trên internet có quá nhiều roadmap, khóa học, video và tài liệu. Sinh viên không biết nên học cái nào trước.

Ví dụ muốn làm Backend nhưng không biết nên học:

- Java trước hay Spring Boot trước?
- SQL trước hay Docker trước?
- REST API trước hay System Design trước?

#### Vấn Đề 3: Portfolio Rời Rạc

Sinh viên thường có GitHub nhưng project không được trình bày tốt:

- Repo thiếu README.
- Không có mô tả project.
- Không có tech stack rõ ràng.
- Không có deployment link.
- Không có ảnh demo.
- Không thể hiện rõ sinh viên đang theo hướng nghề nào.

#### Vấn Đề 4: Thiếu Mentor Cá Nhân

Không phải sinh viên nào cũng có mentor hoặc cố vấn kỹ thuật để hỏi:

- Em nên học gì trong 3 tháng tới?
- Project này đủ apply internship chưa?
- Em hợp Frontend hay Backend hơn?
- Lộ trình hiện tại có thực tế không?

## 3. Mục Tiêu Dự Án

### 3.1 Mục Tiêu Tổng Quát

Xây dựng một nền tảng web giúp sinh viên ngành Software Engineering định hướng nghề nghiệp, phân tích kỹ năng hiện tại, tạo roadmap cá nhân hóa và cải thiện portfolio ứng tuyển.

### 3.2 Mục Tiêu Cụ Thể

Hệ thống cần đạt các mục tiêu:

1. Cho phép sinh viên đăng ký, đăng nhập bằng email/password và Google.
2. Xác thực email bằng OTP khi đăng ký tài khoản thường.
3. Cho phép sinh viên tạo hồ sơ học tập và nghề nghiệp.
4. Cho phép sinh viên nhập kỹ năng hiện tại.
5. Cho phép sinh viên chọn target career role.
6. Phân tích khoảng cách kỹ năng giữa hiện tại và vị trí mục tiêu.
7. Tạo roadmap học tập cá nhân hóa.
8. Cho phép sinh viên theo dõi tiến độ học.
9. Cho phép sinh viên chat với AI Mentor.
10. Cho phép kết nối hoặc nhập GitHub username để phân tích repository.
11. Tạo e-portfolio có thể chia sẻ.
12. Cung cấp gói trả phí cho tính năng nâng cao.
13. Cho phép Admin quản lý skill, career role, resource, user, plan và payment.
14. Cho phép Counselor/Mentor đưa feedback cho sinh viên nếu mở rộng.

## 4. Phạm Vi Dự Án

### 4.1 Phạm Vi MVP

MVP là phiên bản cần làm trước để chứng minh core business flow. MVP không phải toàn bộ hệ thống, nhưng phải đủ để demo giá trị chính.

MVP nên gồm:

- Đăng ký / đăng nhập.
- Google login.
- Email OTP verification.
- JWT authentication.
- Tạo Student Profile.
- Nhập kỹ năng hiện tại.
- Chọn Career Role mục tiêu.
- Admin quản lý Skill.
- Admin quản lý Career Role.
- Admin cấu hình Role Skill Requirement.
- Skill Gap Analysis.
- Dynamic Roadmap cơ bản.
- Roadmap Progress Tracking.
- AI Mentor Chat cơ bản.
- GitHub Repo Analysis cơ bản bằng GitHub username.
- Portfolio Builder cơ bản.
- Admin dashboard cơ bản.

### 4.2 Chức Năng Sau MVP

Các chức năng có thể phát triển sau MVP:

- Market Pulse từ dữ liệu tuyển dụng thực tế.
- Tự động scrape job portal.
- Counselor Dashboard.
- Industry Mentor Feedback.
- GitHub OAuth đầy đủ.
- PDF Skill Gap Report nâng cao.
- AI đánh giá project quality.
- Recommendation theo GPA/transcript.
- Notification system.
- Coupon/discount.
- Subscription recurring payment.
- Institution Plan cho trường/lớp.
- Portfolio custom theme.
- Portfolio custom URL.
- Mentor review có trả phí.

### 4.3 Ngoài Phạm Vi MVP

Không nên làm trong giai đoạn đầu:

- Mobile app native.
- Kubernetes deployment.
- Blockchain certificate.
- Fine-tune LLM.
- Multi-agent AI phức tạp.
- Scraping LinkedIn trực tiếp.
- Hệ thống mentor marketplace lớn.
- Video call mentoring.
- LMS đầy đủ như Moodle/Coursera.

## 5. Đối Tượng Sử Dụng

### 5.1 Guest

Guest là người chưa đăng nhập.

Guest có thể:

- Xem landing page.
- Xem giới thiệu tính năng.
- Xem bảng giá.
- Đăng ký tài khoản.
- Đăng nhập.
- Xem demo portfolio public.

### 5.2 Student

Student là người dùng chính.

Student có thể:

- Tạo hồ sơ cá nhân.
- Nhập kỹ năng hiện tại.
- Chọn career role mục tiêu.
- Xem skill gap report.
- Xem roadmap cá nhân hóa.
- Đánh dấu tiến độ học.
- Chat với AI Mentor.
- Kết nối GitHub hoặc nhập GitHub username.
- Tạo e-portfolio.
- Thanh toán nâng cấp gói Premium.
- Xuất báo cáo PDF nếu thuộc gói trả phí.

### 5.3 Academic Counselor

Academic Counselor là cố vấn học tập trong trường.

Counselor có thể:

- Xem danh sách sinh viên được phân quyền.
- Xem career goal của sinh viên.
- Xem skill gap của sinh viên.
- Xem roadmap progress.
- Gửi feedback học tập.
- Đề xuất môn học hoặc project.
- Xem báo cáo tổng quan theo lớp hoặc nhóm sinh viên.

### 5.4 Industry Mentor

Industry Mentor là mentor từ doanh nghiệp hoặc người có kinh nghiệm thực tế.

Mentor có thể:

- Xem portfolio sinh viên được chia sẻ.
- Đánh giá project.
- Gửi feedback kỹ thuật.
- Đề xuất cải thiện GitHub/README.
- Đề xuất project thực tế.
- Xác nhận kỹ năng nếu có cơ chế review.

### 5.5 Admin

Admin quản lý toàn bộ dữ liệu nền tảng.

Admin có thể:

- Quản lý user.
- Quản lý career role.
- Quản lý skill.
- Quản lý role-skill requirement.
- Quản lý learning resource.
- Quản lý subscription plan.
- Quản lý payment transaction.
- Quản lý mentor/counselor.
- Xem dashboard hệ thống.
- Quản lý prompt template AI.
- Quản lý dữ liệu job trend.

### 5.6 Payment System

Payment System là cổng thanh toán bên ngoài.

Payment System có thể:

- Tạo checkout session.
- Gửi webhook thanh toán thành công/thất bại.
- Gửi webhook refund/cancel.
- Cung cấp transaction id.

### 5.7 AI Mentor

AI Mentor là module AI được hệ thống gọi qua API.

AI Mentor có thể:

- Trả lời câu hỏi nghề nghiệp.
- Đề xuất hướng học.
- Gợi ý project.
- Review GitHub/README ở mức cơ bản.
- Tóm tắt điểm mạnh/yếu của portfolio.

## 6. Công Nghệ Sử Dụng

### 6.1 Stack Chuẩn Theo Project Hiện Tại

Tài liệu gốc đề xuất Next.js full-stack. Tuy nhiên project hiện tại đang tách riêng frontend và backend, nên stack chuẩn cần cập nhật như sau:

| Thành phần | Công nghệ |
|---|---|
| Frontend | React Vite |
| Frontend language | JavaScript hoặc TypeScript |
| Backend API | ASP.NET Core Web API (.NET 8) |
| Backend language | C# |
| Database | PostgreSQL |
| ORM | Entity Framework Core |
| Authentication | JWT, Google Login, Email OTP Verification |
| AI | Gemini API hoặc OpenAI API |
| GitHub Integration | GitHub REST API hoặc Octokit |
| Roadmap Visualization | React Flow |
| Chart | Recharts |
| PDF Export | Puppeteer, QuestPDF hoặc React PDF |
| Storage | Cloudinary, Firebase Storage hoặc Google Cloud Storage |
| Email | Gmail SMTP, SendGrid hoặc Resend |
| Payment | Stripe, PayOS, VNPay hoặc MoMo |
| Frontend Deployment | Firebase Hosting |
| Backend Deployment | Google Cloud Run |
| Version Control | GitHub |

### 6.2 Lý Do Chọn React Vite + ASP.NET Core Web API

Kiến trúc này phù hợp với codebase hiện tại vì:

- React Vite nhẹ, dễ build và deploy lên Firebase Hosting.
- ASP.NET Core Web API mạnh cho REST API, authentication, background jobs và tích hợp database.
- Entity Framework Core hỗ trợ migration PostgreSQL rõ ràng.
- JWT phù hợp cho frontend SPA gọi API độc lập.
- Google Cloud Run phù hợp deploy backend container hóa.
- Dễ mở rộng module AI, GitHub, Payment và Admin sau MVP.
- Phù hợp với hướng triển khai hiện tại của dự án.

### 6.3 Kiến Trúc Tổng Thể

```text
Client Browser
     |
     v
React Vite Frontend
     |
     |-- Pages / Components
     |-- Auth UI
     |-- Dashboard UI
     |-- Roadmap UI
     |
     v
ASP.NET Core Web API
     |
     |-- Controllers
     |-- Services
     |-- JWT Authentication
     |-- Email OTP
     |-- Business Logic
     |
     v
Entity Framework Core
     |
     v
PostgreSQL Database

External Services:
- Gemini/OpenAI API
- GitHub API
- Payment Gateway
- Firebase Hosting
- Google Cloud Run
- Email SMTP Provider
```

### 6.4 Cấu Trúc Thư Mục Đề Xuất

```text
SWP/
|-- SWP-FE/
|   |-- src/
|   |   |-- App.jsx
|   |   |-- main.jsx
|   |   |-- styles.css
|   |   |-- components/
|   |   |-- pages/
|   |   |-- services/
|   |   |-- utils/
|   |-- .env
|   |-- package.json
|   |-- vite.config.js
|
|-- SWP-BE/
    |-- Controllers/
    |   |-- AuthController.cs
    |   |-- ProfileController.cs
    |   |-- SkillsController.cs
    |   |-- CareerRolesController.cs
    |   |-- SkillGapController.cs
    |   |-- RoadmapsController.cs
    |   |-- MentorController.cs
    |   |-- GithubController.cs
    |   |-- PortfolioController.cs
    |   |-- PaymentsController.cs
    |   |-- AdminController.cs
    |-- Contracts/
    |-- Data/
    |   |-- AppDbContext.cs
    |-- Models/
    |-- Services/
    |-- Options/
    |-- Migrations/
    |-- appsettings.json
    |-- Program.cs
```

## 7. Luồng Nghiệp Vụ Tổng Quát

### 7.1 Luồng Chính Của Sinh Viên

```text
Sinh viên đăng ký / đăng nhập
        |
        v
Tạo Student Profile
        |
        v
Nhập kỹ năng hiện tại
        |
        v
Chọn Target Career Role
        |
        v
Hệ thống phân tích Skill Gap
        |
        v
Hệ thống tạo Dynamic Roadmap
        |
        v
Sinh viên học theo roadmap
        |
        v
Cập nhật tiến độ học
        |
        v
Chat với AI Mentor
        |
        v
Phân tích GitHub / Portfolio
        |
        v
Xuất báo cáo hoặc chia sẻ portfolio
        |
        v
Nâng cấp gói trả phí nếu cần tính năng cao cấp
```

### 7.2 Luồng Tổng Quát Theo Hệ Thống

```text
User Input
    |
    v
Student Profile
    |
    v
Skill Assessment
    |
    v
Career Role Selection
    |
    v
Skill Gap Analysis
    |
    v
Roadmap Generation
    |
    v
Progress Tracking
    |
    v
AI Mentor Recommendation
    |
    v
Portfolio Builder
    |
    v
Payment / Subscription
    |
    v
Premium Features
```

## 8. Luồng Nghiệp Vụ Chi Tiết

### 8.1 Đăng Ký Và Đăng Nhập

Mục tiêu: Cho phép người dùng tạo tài khoản và truy cập hệ thống.

Actor:

- Guest
- Student
- Admin
- Counselor
- Mentor

Luồng chính:

1. Người dùng truy cập trang đăng nhập.
2. Người dùng chọn đăng ký bằng email/password hoặc Google.
3. Nếu đăng ký bằng email/password, hệ thống yêu cầu xác nhận mật khẩu.
4. Hệ thống gửi OTP xác nhận email.
5. Người dùng nhập OTP.
6. Hệ thống kích hoạt tài khoản.
7. Người dùng đăng nhập và nhận JWT.
8. Người dùng được chuyển đến onboarding/profile.

Ngoại lệ:

| Tình huống | Cách xử lý |
|---|---|
| Email đã tồn tại | Thông báo đăng nhập thay vì đăng ký |
| Username đã tồn tại | Yêu cầu chọn username khác |
| Sai mật khẩu | Hiển thị lỗi đăng nhập |
| OTP sai hoặc hết hạn | Yêu cầu nhập lại hoặc gửi lại OTP |
| Google OAuth thất bại | Cho phép thử lại |
| Tài khoản bị khóa | Liên hệ admin |

### 8.2 Tạo Student Profile

Mục tiêu: Thu thập thông tin ban đầu để cá nhân hóa roadmap.

Dữ liệu nhập:

- Họ tên
- Trường
- Ngành học
- Năm học
- GPA nếu có
- Mục tiêu nghề nghiệp ban đầu
- Thời gian mong muốn đạt mục tiêu
- GitHub username nếu có
- Số giờ có thể học mỗi tuần

Luồng chính:

1. Sinh viên đăng nhập lần đầu.
2. Hệ thống chuyển đến trang onboarding/profile.
3. Sinh viên nhập thông tin cá nhân.
4. Hệ thống validate dữ liệu.
5. Hệ thống lưu vào `StudentProfile`.
6. Sinh viên chuyển sang bước nhập kỹ năng.

### 8.3 Nhập Kỹ Năng Hiện Tại

Mục tiêu: Ghi nhận kỹ năng hiện tại của sinh viên.

Luồng chính:

1. Sinh viên vào trang Skill Assessment.
2. Hệ thống hiển thị danh sách kỹ năng theo nhóm.
3. Sinh viên chọn kỹ năng đã biết.
4. Sinh viên chọn level cho từng kỹ năng.
5. Sinh viên có thể thêm evidence như GitHub repo hoặc chứng chỉ.
6. Hệ thống lưu vào `UserSkill`.

Nhóm kỹ năng:

- Programming Language
- Frontend
- Backend
- Database
- DevOps
- Cloud
- Mobile
- Data
- Testing
- Soft Skills

Level kỹ năng:

| Level | Ý nghĩa |
|---|---|
| Beginner | Biết cơ bản |
| Intermediate | Có thể làm project |
| Advanced | Có thể làm sản phẩm thực tế |
| Verified | Có project/chứng chỉ/mentor xác nhận |

### 8.4 Chọn Career Role

Mục tiêu: Sinh viên chọn hướng nghề muốn theo đuổi.

Luồng chính:

1. Sinh viên mở trang Career Paths.
2. Hệ thống hiển thị danh sách career roles.
3. Sinh viên xem mô tả từng role.
4. Sinh viên chọn một role chính.
5. Hệ thống lưu target role vào `StudentProfile`.
6. Hệ thống kích hoạt phân tích skill gap.

Danh sách role đề xuất:

| Career Role | Ví dụ kỹ năng chính |
|---|---|
| Backend Developer | Java, C#, Node.js, REST API, Database |
| Frontend Developer | React, TypeScript, UI/UX |
| Fullstack Developer | Frontend, Backend, Database |
| Mobile Developer | Flutter, React Native, Kotlin, Swift |
| DevOps Engineer | Docker, CI/CD, Linux, Cloud |
| Data Engineer | SQL, ETL, Data Pipeline, Cloud |
| QA Automation Engineer | Testing, Selenium, Cypress, CI/CD |
| Cloud Engineer | AWS, Azure, GCP, Networking |
| AI Application Developer | Prompting, API integration, Vector DB, Python/C# |

### 8.5 Phân Tích Skill Gap

Mục tiêu: So sánh kỹ năng hiện tại với yêu cầu của career role.

Dữ liệu đầu vào:

- `UserSkill`
- `CareerRole`
- `RoleSkillRequirement`
- `LearningResource`
- `JobTrend` nếu có

Luồng chính:

1. Sinh viên chọn target role.
2. Hệ thống lấy danh sách skill yêu cầu của role.
3. Hệ thống lấy danh sách skill hiện tại của sinh viên.
4. Hệ thống so sánh từng skill.
5. Hệ thống phân loại:
   - `Matched`
   - `Weak`
   - `Missing`
   - `NotVerified`
6. Hệ thống tính priority cho từng missing/weak skill.
7. Hệ thống tạo `SkillGapReport`.
8. Dashboard hiển thị kết quả.

Kết quả đầu ra:

- Skill Match Score.
- Danh sách kỹ năng đã có.
- Danh sách kỹ năng còn yếu.
- Danh sách kỹ năng thiếu.
- Danh sách kỹ năng cần học gấp.
- Đề xuất roadmap.

### 8.6 Tạo Dynamic Roadmap

Mục tiêu: Tạo lộ trình học cá nhân hóa cho sinh viên.

Luồng chính:

1. Hệ thống nhận kết quả skill gap.
2. Hệ thống lấy roadmap template theo career role.
3. Hệ thống loại bỏ hoặc giảm ưu tiên skill sinh viên đã có.
4. Hệ thống ưu tiên skill còn thiếu.
5. Hệ thống sắp xếp node theo prerequisite.
6. Hệ thống gắn learning resources.
7. Hệ thống tạo `Roadmap` và `RoadmapNode`.
8. Sinh viên xem roadmap trên giao diện.

Trạng thái Roadmap Node:

| Trạng thái | Ý nghĩa |
|---|---|
| NotStarted | Chưa học |
| InProgress | Đang học |
| Completed | Đã hoàn thành |
| Verified | Đã có minh chứng |
| NeedReview | Cần ôn lại |

### 8.7 Theo Dõi Tiến Độ Học

Mục tiêu: Sinh viên cập nhật quá trình học và hệ thống theo dõi progress.

Luồng chính:

1. Sinh viên mở roadmap.
2. Sinh viên chọn một node.
3. Sinh viên xem mô tả, tài nguyên học, task gợi ý.
4. Sinh viên học và đánh dấu trạng thái.
5. Hệ thống cập nhật `RoadmapNode`.
6. Hệ thống tính lại phần trăm hoàn thành.
7. Hệ thống đề xuất node tiếp theo.

### 8.8 AI Mentor Chat

Mục tiêu: AI tư vấn nghề nghiệp dựa trên dữ liệu cá nhân của sinh viên.

Luồng chính:

1. Sinh viên vào trang AI Mentor.
2. Sinh viên nhập câu hỏi.
3. Hệ thống lấy context:
   - Profile
   - Current skills
   - Target role
   - Skill gap
   - Roadmap progress
   - GitHub repos
4. Hệ thống gửi prompt đến AI API.
5. AI trả lời.
6. Hệ thống lưu `MentorSession`.
7. Sinh viên tiếp tục hỏi hoặc lưu lời khuyên.

Ví dụ câu hỏi:

- Em muốn trở thành Backend Developer trong 6 tháng thì nên học gì trước?
- GitHub của em có đủ apply internship chưa?
- Em nên chọn Backend hay DevOps?
- Em cần làm project nào để portfolio mạnh hơn?

### 8.9 GitHub Analysis

Mục tiêu: Phân tích repository công khai của sinh viên để đánh giá portfolio.

Luồng chính MVP:

1. Sinh viên nhập GitHub username.
2. Hệ thống gọi GitHub API.
3. Hệ thống lấy danh sách public repositories.
4. Hệ thống lấy README.md nếu có.
5. AI tóm tắt project objective, tech stack, features.
6. Hệ thống map repo với skill tương ứng.
7. Sinh viên chọn repo đưa vào portfolio.

Luồng nâng cao:

1. Sinh viên kết nối GitHub OAuth.
2. Hệ thống lấy repo public/private được cấp quyền.
3. AI đánh giá code structure, README, commit history.
4. Hệ thống đề xuất cải thiện portfolio.

### 8.10 Portfolio Builder

Mục tiêu: Giúp sinh viên tạo portfolio public từ profile và GitHub project.

Luồng chính:

1. Sinh viên mở Portfolio Builder.
2. Hệ thống lấy profile, skills và GitHub repos.
3. Sinh viên chọn project muốn hiển thị.
4. Sinh viên chỉnh bio, title, mô tả project.
5. Sinh viên chọn theme.
6. Sinh viên publish portfolio.
7. Hệ thống tạo public URL theo slug.

### 8.11 Payment Và Subscription

Mục tiêu: Cung cấp gói trả phí để mở khóa tính năng nâng cao.

Luồng checkout:

1. Student mở Pricing page.
2. Student chọn plan.
3. Backend tạo checkout session qua payment provider.
4. Frontend chuyển user đến checkout URL.
5. Payment provider xử lý thanh toán.
6. Payment provider gửi webhook về backend.
7. Backend xác thực webhook.
8. Backend tạo `PaymentTransaction`.
9. Backend cập nhật `Subscription`.
10. Student được mở khóa premium feature.

Business rule:

- Không mở khóa Premium nếu chưa nhận webhook thành công.
- Không tin trạng thái thanh toán từ frontend.
- Mỗi webhook chỉ được xử lý một lần.
- Mọi giao dịch phải được lưu.

## 9. Gói Dịch Vụ

| Tính năng | Free | Premium | Mentor Review | Institution |
|---|---|---|---|---|
| Tạo profile | Có | Có | Có | Có |
| Skill input | Có | Có | Có | Có |
| Skill gap cơ bản | Có | Có | Có | Có |
| Skill gap nâng cao | Không | Có | Có | Có |
| Roadmap cơ bản | Có | Có | Có | Có |
| Nhiều roadmap | Không | Có | Có | Có |
| AI Mentor | Giới hạn | Nhiều hơn | Nhiều hơn | Theo gói |
| GitHub analysis | Cơ bản | Nâng cao | Nâng cao | Có |
| Portfolio builder | Cơ bản | Nâng cao | Nâng cao | Có |
| PDF report | Không | Có | Có | Có |
| Mentor feedback | Không | Không | Có | Tùy gói |
| Counselor dashboard | Không | Không | Không | Có |

Premium feature nên khóa bằng subscription:

- AI Mentor không giới hạn hoặc hạn mức cao.
- Xuất PDF report.
- Phân tích GitHub nâng cao.
- Tạo nhiều roadmap.
- So sánh nhiều career role.
- Portfolio custom theme.
- Portfolio custom URL.
- Mentor review.
- Skill readiness score nâng cao.
- Market Pulse nâng cao.

## 10. Database Chuẩn Đề Xuất

### 10.1 Danh Sách Bảng Chính

```text
User
StudentProfile
Skill
UserSkill
CareerRole
RoleSkillRequirement
SkillGapReport
SkillGapReportItem
Roadmap
RoadmapNode
LearningResource
MentorSession
GithubRepository
GithubRepositorySkill
Portfolio
PortfolioProject
SubscriptionPlan
Subscription
PaymentTransaction
PaymentWebhookEvent
Invoice
Coupon
MentorFeedback
CounselorFeedback
```

### 10.2 User

Lưu thông tin tài khoản, authentication và trạng thái người dùng.

```text
id
username
email
fullName
avatarUrl
googleSubject
passwordHash
isEmailVerified
emailVerificationOtpHash
emailVerificationOtpExpiresAt
emailVerifiedAt
role
isActive
createdAt
updatedAt
```

Ghi chú:

- Không lưu password plaintext.
- Không lưu OTP plaintext.
- `role`: `Student`, `Admin`, `Counselor`, `Mentor`.

### 10.3 StudentProfile

```text
id
userId
school
major
year
gpa
targetRoleId
githubUsername
careerGoal
preferredLearningHoursPerWeek
createdAt
updatedAt
```

### 10.4 Skill

```text
id
name
category
description
isActive
createdAt
updatedAt
```

### 10.5 UserSkill

```text
id
userId
skillId
level
evidenceUrl
evidenceType
isVerified
verifiedByUserId
verifiedAt
createdAt
updatedAt
```

### 10.6 CareerRole

```text
id
name
description
level
isActive
createdAt
updatedAt
```

### 10.7 RoleSkillRequirement

```text
id
careerRoleId
skillId
requiredLevel
priority
weight
createdAt
updatedAt
```

### 10.8 SkillGapReport

```text
id
userId
careerRoleId
matchScore
summary
createdAt
updatedAt
```

### 10.9 SkillGapReportItem

```text
id
skillGapReportId
skillId
currentLevel
requiredLevel
status
priority
recommendation
createdAt
```

### 10.10 Roadmap

```text
id
userId
careerRoleId
skillGapReportId
title
description
status
progress
createdAt
updatedAt
```

### 10.11 RoadmapNode

```text
id
roadmapId
skillId
learningResourceId
title
description
nodeType
status
orderIndex
estimatedHours
priority
createdAt
updatedAt
```

### 10.12 LearningResource

```text
id
skillId
title
url
resourceType
difficulty
estimatedHours
isActive
createdAt
updatedAt
```

### 10.13 MentorSession

```text
id
userId
question
answer
contextJson
model
tokensUsed
createdAt
```

### 10.14 GithubRepository

```text
id
userId
repoName
repoUrl
description
mainLanguage
readmeContent
aiSummary
techStackJson
qualityScore
lastSyncedAt
createdAt
updatedAt
```

### 10.15 GithubRepositorySkill

```text
id
githubRepositoryId
skillId
confidenceScore
evidenceText
createdAt
```

### 10.16 Portfolio

```text
id
userId
slug
title
bio
theme
isPublished
publishedAt
createdAt
updatedAt
```

### 10.17 PortfolioProject

```text
id
portfolioId
githubRepositoryId
title
description
techStackJson
demoUrl
sourceUrl
orderIndex
createdAt
updatedAt
```

### 10.18 SubscriptionPlan

```text
id
name
description
price
currency
billingCycle
featuresJson
isActive
createdAt
updatedAt
```

### 10.19 Subscription

```text
id
userId
planId
status
startedAt
expiredAt
cancelledAt
provider
providerSubscriptionId
createdAt
updatedAt
```

### 10.20 PaymentTransaction

```text
id
userId
subscriptionId
planId
amount
currency
status
provider
providerTransactionId
checkoutUrl
paidAt
createdAt
updatedAt
```

### 10.21 PaymentWebhookEvent

```text
id
provider
eventId
eventType
payloadJson
processedAt
createdAt
```

### 10.22 Coupon

```text
id
code
discountType
discountValue
maxUsage
usedCount
expiredAt
isActive
createdAt
updatedAt
```

### 10.23 MentorFeedback

```text
id
mentorId
studentId
portfolioId
githubRepositoryId
comment
rating
createdAt
updatedAt
```

### 10.24 CounselorFeedback

```text
id
counselorId
studentId
roadmapId
skillGapReportId
comment
createdAt
updatedAt
```

### 10.25 Database Attributes Chi Tiết

Phần này chuẩn hóa thuộc tính theo hướng có thể chuyển trực tiếp sang Entity Framework Core migration. Quy ước:

- `uuid`: khóa chính/khóa ngoại.
- `timestamptz`: `timestamp with time zone`.
- `jsonb`: dữ liệu JSON cần lưu linh hoạt.
- `varchar(n)`: chuỗi có giới hạn độ dài.
- `text`: nội dung dài.
- `decimal(p,s)`: số thập phân.

#### User

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| username | varchar(100) | Yes | Unique, index |
| email | varchar(256) | No | Unique, index |
| fullName | varchar(200) | No |  |
| avatarUrl | varchar(1024) | Yes |  |
| googleSubject | varchar(128) | Yes | Unique, index |
| passwordHash | varchar(512) | Yes |  |
| isEmailVerified | boolean | No | default false |
| emailVerificationOtpHash | varchar(512) | Yes |  |
| emailVerificationOtpExpiresAt | timestamptz | Yes |  |
| emailVerifiedAt | timestamptz | Yes |  |
| role | varchar(50) | No | default Student, index |
| isActive | boolean | No | default true, index |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### StudentProfile

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| userId | uuid | No | FK User.id, Unique, index |
| school | varchar(200) | Yes |  |
| major | varchar(200) | Yes |  |
| year | int | Yes | check 1-8 |
| gpa | decimal(4,2) | Yes | check >= 0 |
| targetRoleId | uuid | Yes | FK CareerRole.id, index |
| githubUsername | varchar(100) | Yes | index |
| careerGoal | varchar(1000) | Yes |  |
| preferredLearningHoursPerWeek | int | Yes | check >= 0 |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### Skill

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| name | varchar(150) | No | Unique with category |
| category | varchar(80) | No | Unique with name, index |
| description | varchar(1000) | Yes |  |
| isActive | boolean | No | default true, index |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### UserSkill

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| userId | uuid | No | FK User.id, Unique with skillId, index |
| skillId | uuid | No | FK Skill.id, Unique with userId, index |
| level | varchar(30) | No | Beginner/Intermediate/Advanced/Verified, index |
| evidenceUrl | varchar(1024) | Yes |  |
| evidenceType | varchar(50) | Yes | GitHub/Certificate/Portfolio/Other |
| isVerified | boolean | No | default false, index |
| verifiedByUserId | uuid | Yes | FK User.id |
| verifiedAt | timestamptz | Yes |  |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### CareerRole

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| name | varchar(150) | No | Unique |
| description | varchar(2000) | Yes |  |
| level | varchar(50) | Yes | Internship/Fresher/Junior |
| isActive | boolean | No | default true, index |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### RoleSkillRequirement

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| careerRoleId | uuid | No | FK CareerRole.id, Unique with skillId, index |
| skillId | uuid | No | FK Skill.id, Unique with careerRoleId, index |
| requiredLevel | varchar(30) | No | Beginner/Intermediate/Advanced/Verified |
| priority | int | No | check 1-5, index |
| weight | decimal(5,2) | No | default 1.00, check > 0 |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### SkillGapReport

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| userId | uuid | No | FK User.id, index |
| careerRoleId | uuid | No | FK CareerRole.id, index |
| matchScore | decimal(5,2) | No | check 0-100 |
| summary | varchar(4000) | Yes |  |
| createdAt | timestamptz | No | index with userId desc |
| updatedAt | timestamptz | No |  |

#### SkillGapReportItem

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| skillGapReportId | uuid | No | FK SkillGapReport.id, Unique with skillId, index |
| skillId | uuid | No | FK Skill.id, Unique with skillGapReportId, index |
| currentLevel | varchar(30) | Yes | null if Missing |
| requiredLevel | varchar(30) | No |  |
| status | varchar(30) | No | Matched/Weak/Missing/NotVerified, index |
| priority | int | No | check 1-5, index |
| recommendation | varchar(2000) | Yes |  |
| createdAt | timestamptz | No |  |

#### Roadmap

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| userId | uuid | No | FK User.id, index |
| careerRoleId | uuid | No | FK CareerRole.id, index |
| skillGapReportId | uuid | Yes | FK SkillGapReport.id |
| title | varchar(200) | No |  |
| description | varchar(2000) | Yes |  |
| status | varchar(30) | No | Draft/Active/Completed/Archived, index |
| progress | decimal(5,2) | No | default 0, check 0-100 |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### RoadmapNode

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| roadmapId | uuid | No | FK Roadmap.id, Unique with orderIndex, index |
| skillId | uuid | Yes | FK Skill.id |
| learningResourceId | uuid | Yes | FK LearningResource.id |
| title | varchar(200) | No |  |
| description | varchar(2000) | Yes |  |
| nodeType | varchar(30) | No | Skill/Project/Reading/Practice/Assessment |
| status | varchar(30) | No | NotStarted/InProgress/Completed/Verified/NeedReview, index |
| orderIndex | int | No | Unique with roadmapId |
| estimatedHours | int | Yes | check >= 0 |
| priority | int | No | check 1-5 |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### LearningResource

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| skillId | uuid | Yes | FK Skill.id, index |
| title | varchar(200) | No |  |
| url | varchar(1024) | No |  |
| resourceType | varchar(50) | No | Course/Article/Video/Book/Project/Docs |
| difficulty | varchar(50) | Yes | Beginner/Intermediate/Advanced |
| estimatedHours | int | Yes | check >= 0 |
| isActive | boolean | No | default true, index |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### MentorSession

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| userId | uuid | No | FK User.id, index with createdAt desc |
| question | text | No |  |
| answer | text | No |  |
| contextJson | jsonb | Yes |  |
| model | varchar(100) | Yes |  |
| tokensUsed | int | Yes | check >= 0 |
| createdAt | timestamptz | No | index with userId desc |

#### GithubRepository

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| userId | uuid | No | FK User.id, Unique with repoUrl, index |
| repoName | varchar(200) | No |  |
| repoUrl | varchar(1024) | No | Unique with userId |
| description | varchar(2000) | Yes |  |
| mainLanguage | varchar(100) | Yes | index |
| readmeContent | text | Yes |  |
| aiSummary | text | Yes |  |
| techStackJson | jsonb | Yes |  |
| qualityScore | decimal(5,2) | Yes | check 0-100 |
| lastSyncedAt | timestamptz | Yes |  |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### GithubRepositorySkill

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| githubRepositoryId | uuid | No | FK GithubRepository.id, Unique with skillId, index |
| skillId | uuid | No | FK Skill.id, Unique with githubRepositoryId, index |
| confidenceScore | decimal(5,2) | Yes | check 0-100 |
| evidenceText | varchar(2000) | Yes |  |
| createdAt | timestamptz | No |  |

#### Portfolio

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| userId | uuid | No | FK User.id, index |
| slug | varchar(150) | No | Unique, index |
| title | varchar(200) | No |  |
| bio | varchar(2000) | Yes |  |
| theme | varchar(80) | Yes | default Default |
| isPublished | boolean | No | default false, index |
| publishedAt | timestamptz | Yes |  |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### PortfolioProject

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| portfolioId | uuid | No | FK Portfolio.id, index with orderIndex |
| githubRepositoryId | uuid | Yes | FK GithubRepository.id |
| title | varchar(200) | No |  |
| description | varchar(3000) | Yes |  |
| techStackJson | jsonb | Yes |  |
| demoUrl | varchar(1024) | Yes |  |
| sourceUrl | varchar(1024) | Yes |  |
| orderIndex | int | No | default 0, index with portfolioId |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### SubscriptionPlan

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| name | varchar(100) | No | Unique |
| description | varchar(2000) | Yes |  |
| price | decimal(18,2) | No | default 0 |
| currency | varchar(10) | No | VND/USD |
| billingCycle | varchar(30) | No | Monthly/Yearly/OneTime |
| featuresJson | jsonb | Yes |  |
| isActive | boolean | No | default true, index |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### Subscription

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| userId | uuid | No | FK User.id, index with status |
| planId | uuid | No | FK SubscriptionPlan.id |
| status | varchar(30) | No | Pending/Active/Cancelled/Expired, index with userId |
| startedAt | timestamptz | Yes |  |
| expiredAt | timestamptz | Yes | index |
| cancelledAt | timestamptz | Yes |  |
| provider | varchar(50) | Yes | Stripe/PayOS/VNPay/MoMo |
| providerSubscriptionId | varchar(200) | Yes | index |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### PaymentTransaction

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| userId | uuid | No | FK User.id, index with createdAt desc |
| subscriptionId | uuid | Yes | FK Subscription.id |
| planId | uuid | No | FK SubscriptionPlan.id |
| amount | decimal(18,2) | No |  |
| currency | varchar(10) | No |  |
| status | varchar(30) | No | Created/Pending/Succeeded/Failed/Cancelled/Refunded, index |
| provider | varchar(50) | No | index |
| providerTransactionId | varchar(200) | Yes | Unique, index |
| checkoutUrl | varchar(2048) | Yes |  |
| paidAt | timestamptz | Yes |  |
| createdAt | timestamptz | No | index with userId desc |
| updatedAt | timestamptz | No |  |

#### PaymentWebhookEvent

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| provider | varchar(50) | No | Unique with eventId, index |
| eventId | varchar(200) | No | Unique with provider |
| eventType | varchar(100) | No | index |
| payloadJson | jsonb | No |  |
| processedAt | timestamptz | Yes |  |
| createdAt | timestamptz | No |  |

#### Invoice

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| userId | uuid | No | FK User.id, index |
| paymentTransactionId | uuid | No | FK PaymentTransaction.id, Unique |
| invoiceNumber | varchar(100) | No | Unique |
| amount | decimal(18,2) | No |  |
| currency | varchar(10) | No |  |
| issuedAt | timestamptz | No |  |
| pdfUrl | varchar(1024) | Yes |  |
| createdAt | timestamptz | No |  |

#### Coupon

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| code | varchar(80) | No | Unique, index |
| discountType | varchar(30) | No | Percent/Fixed |
| discountValue | decimal(18,2) | No |  |
| maxUsage | int | Yes |  |
| usedCount | int | No | default 0 |
| expiredAt | timestamptz | Yes | index |
| isActive | boolean | No | default true, index |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### MentorFeedback

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| mentorId | uuid | No | FK User.id, index |
| studentId | uuid | No | FK User.id, index |
| portfolioId | uuid | Yes | FK Portfolio.id |
| githubRepositoryId | uuid | Yes | FK GithubRepository.id |
| comment | text | No |  |
| rating | int | Yes | check 1-5 |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

#### CounselorFeedback

| Field | Type | Null | Key / Index / Default |
|---|---:|---:|---|
| id | uuid | No | PK |
| counselorId | uuid | No | FK User.id, index |
| studentId | uuid | No | FK User.id, index |
| roadmapId | uuid | Yes | FK Roadmap.id |
| skillGapReportId | uuid | Yes | FK SkillGapReport.id |
| comment | text | No |  |
| createdAt | timestamptz | No |  |
| updatedAt | timestamptz | No |  |

### 10.26 Index Và Constraint Tổng Hợp

| Bảng | Index / Constraint |
|---|---|
| User | Unique `email`, unique nullable `username`, unique nullable `googleSubject` |
| StudentProfile | Unique `userId`, index `targetRoleId`, index `githubUsername` |
| Skill | Unique `(name, category)`, index `category`, index `isActive` |
| UserSkill | Unique `(userId, skillId)`, index `level`, index `isVerified` |
| CareerRole | Unique `name`, index `isActive` |
| RoleSkillRequirement | Unique `(careerRoleId, skillId)`, index `priority` |
| SkillGapReport | Index `(userId, createdAt desc)`, index `careerRoleId` |
| SkillGapReportItem | Unique `(skillGapReportId, skillId)`, index `status`, index `priority` |
| Roadmap | Index `(userId, status)`, index `careerRoleId` |
| RoadmapNode | Unique `(roadmapId, orderIndex)`, index `status` |
| GithubRepository | Unique `(userId, repoUrl)`, index `mainLanguage` |
| GithubRepositorySkill | Unique `(githubRepositoryId, skillId)` |
| Portfolio | Unique `slug`, index `(userId, isPublished)` |
| Subscription | Index `(userId, status)`, index `expiredAt` |
| PaymentTransaction | Unique nullable `providerTransactionId`, index `(userId, createdAt desc)` |
| PaymentWebhookEvent | Unique `(provider, eventId)` |
| Coupon | Unique `code`, index `expiredAt`, index `isActive` |

### 10.27 Seed Data Cần Có Ban Đầu

Career roles nên seed trước:

- Backend Developer
- Frontend Developer
- Fullstack Developer
- Mobile Developer
- DevOps Engineer
- Data Engineer
- QA Automation Engineer
- Cloud Engineer
- AI Application Developer

Skill categories nên seed trước:

- Programming Language
- Frontend
- Backend
- Database
- DevOps
- Cloud
- Mobile
- Data
- Testing
- Soft Skills

Subscription plans nên seed trước nếu có payment:

- Free
- Premium
- Mentor Review
- Institution

## 11. Quan Hệ Chính

```text
User 1-1 StudentProfile
User 1-n UserSkill
CareerRole 1-n RoleSkillRequirement
Skill 1-n RoleSkillRequirement
Skill 1-n UserSkill
User 1-n SkillGapReport
SkillGapReport 1-n SkillGapReportItem
User 1-n Roadmap
Roadmap 1-n RoadmapNode
LearningResource có thể gắn với Skill hoặc RoadmapNode
User 1-n MentorSession
User 1-n GithubRepository
GithubRepository n-n Skill thông qua GithubRepositorySkill
User 1-1 hoặc 1-n Portfolio
Portfolio 1-n PortfolioProject
User 1-n Subscription
SubscriptionPlan 1-n Subscription
PaymentTransaction liên kết User, Subscription và Plan
PaymentWebhookEvent lưu webhook để tránh xử lý trùng lặp
MentorFeedback liên kết Mentor, Student, Portfolio hoặc Repository
CounselorFeedback liên kết Counselor, Student, Roadmap hoặc SkillGapReport
```

## 12. Enum Và Trạng Thái Chuẩn

```text
UserRole:
- Student
- Admin
- Counselor
- Mentor

SkillLevel:
- Beginner
- Intermediate
- Advanced
- Verified

SkillGapStatus:
- Matched
- Weak
- Missing
- NotVerified

RoadmapStatus:
- Draft
- Active
- Completed
- Archived

RoadmapNodeType:
- Skill
- Project
- Reading
- Practice
- Assessment

RoadmapNodeStatus:
- NotStarted
- InProgress
- Completed
- Verified
- NeedReview

SubscriptionStatus:
- Free
- Pending
- Active
- PastDue
- Cancelled
- Expired
- Refunded

PaymentStatus:
- Created
- Pending
- Succeeded
- Failed
- Cancelled
- Refunded

BillingCycle:
- Monthly
- Yearly
- OneTime

PaymentProvider:
- Stripe
- PayOS
- VNPay
- MoMo
```

## 13. API Endpoint Đề Xuất

### 13.1 Auth

```text
POST /api/auth/register
POST /api/auth/verify-email
POST /api/auth/login
POST /api/auth/google
POST /api/auth/logout
GET  /api/auth/me
```

### 13.2 Student Profile

```text
GET  /api/profile
POST /api/profile
PUT  /api/profile
```

### 13.3 Skill

```text
GET    /api/skills
POST   /api/skills
PUT    /api/skills/:id
DELETE /api/skills/:id

GET    /api/user-skills
POST   /api/user-skills
PUT    /api/user-skills/:id
DELETE /api/user-skills/:id
```

### 13.4 Career Role

```text
GET  /api/career-roles
GET  /api/career-roles/:id
POST /api/career-roles
PUT  /api/career-roles/:id
POST /api/career-roles/select
```

### 13.5 Skill Gap

```text
POST /api/skill-gap/analyze
GET  /api/skill-gap/latest
GET  /api/skill-gap/:id
```

### 13.6 Roadmap

```text
POST /api/roadmap/generate
GET  /api/roadmap
GET  /api/roadmap/:id
PUT  /api/roadmap-node/:id/status
```

### 13.7 AI Mentor

```text
POST /api/mentor/chat
GET  /api/mentor/sessions
GET  /api/mentor/sessions/:id
```

### 13.8 GitHub

```text
POST /api/github/sync
GET  /api/github/repositories
POST /api/github/analyze-readme
```

### 13.9 Portfolio

```text
GET  /api/portfolio/me
POST /api/portfolio
PUT  /api/portfolio
GET  /api/portfolio/:slug
POST /api/portfolio/publish
```

### 13.10 Payment

```text
GET  /api/payment/plans
POST /api/payment/checkout
POST /api/payment/webhook
GET  /api/payment/history
POST /api/payment/cancel-subscription
POST /api/payment/refund
```

### 13.11 Admin

```text
GET    /api/admin/users
PUT    /api/admin/users/:id/role
GET    /api/admin/payments
GET    /api/admin/subscriptions
POST   /api/admin/skills
PUT    /api/admin/skills/:id
DELETE /api/admin/skills/:id
POST   /api/admin/career-roles
PUT    /api/admin/career-roles/:id
DELETE /api/admin/career-roles/:id
POST   /api/admin/role-skill-requirements
PUT    /api/admin/role-skill-requirements/:id
DELETE /api/admin/role-skill-requirements/:id
```

## 14. Màn Hình Hệ Thống

### 14.1 Guest

- Landing Page
- Pricing Page
- Login Page
- Register Page
- Public Portfolio Page

### 14.2 Student

- Dashboard
- Onboarding Profile
- Skill Assessment
- Career Path Selection
- Skill Gap Report
- Roadmap
- AI Mentor Chat
- GitHub Analysis
- Portfolio Builder
- Portfolio Preview
- Pricing
- Billing
- Payment Success
- Payment Cancel

### 14.3 Counselor

- Counselor Dashboard
- Student List
- Student Detail
- Skill Gap View
- Roadmap Progress View
- Feedback Form

### 14.4 Mentor

- Mentor Dashboard
- Portfolio Review Page
- Project Feedback Page
- Student Feedback History

### 14.5 Admin

- Admin Dashboard
- User Management
- Skill Management
- Career Role Management
- Role Skill Requirement Management
- Learning Resource Management
- Subscription Plan Management
- Payment Transaction Management
- Coupon Management
- System Settings
- AI Prompt Management

## 15. Non-Functional Requirements

### 15.1 Bảo Mật

- Mật khẩu phải được hash.
- OTP phải được hash.
- API cần kiểm tra JWT.
- Phân quyền rõ theo role.
- Webhook thanh toán phải xác thực chữ ký.
- Không lưu thông tin thẻ thanh toán trong hệ thống.
- File upload cần giới hạn loại file và kích thước.
- Dữ liệu cá nhân cần được bảo vệ.
- Không log password, OTP, token hoặc app password email.

### 15.2 Hiệu Năng

- Dashboard tải dưới 3 giây trong điều kiện bình thường.
- Chat AI phản hồi trong thời gian chấp nhận được.
- Roadmap render mượt với số lượng node vừa phải.
- API phân tích skill gap nên phản hồi nhanh.
- Query database cần index các trường quan trọng.

### 15.3 Khả Năng Mở Rộng

- Có thể thêm career role mới.
- Có thể thêm skill mới.
- Có thể thêm payment provider mới.
- Có thể thêm AI provider mới.
- Có thể mở rộng sang trường đại học hoặc institution plan.

### 15.4 Tính Ổn Định

- Có logging lỗi.
- Có validate input.
- Có xử lý lỗi API bên ngoài.
- Có retry hoặc fallback cho AI API nếu cần.
- Có kiểm tra webhook trùng lặp.

## 16. Business Rules Quan Trọng

### 16.1 Skill Gap

- Một skill được xem là `Matched` nếu level hiện tại >= required level.
- Một skill được xem là `Weak` nếu user có skill nhưng level thấp hơn yêu cầu.
- Một skill được xem là `Missing` nếu user chưa có skill đó.
- Một skill được xem là `NotVerified` nếu user có khai báo nhưng chưa có evidence hoặc chưa được xác nhận.
- Skill có priority cao phải được đưa vào roadmap sớm hơn.

### 16.2 Roadmap

- Roadmap phải dựa trên target role.
- Mỗi roadmap node nên gắn với skill hoặc learning resource.
- Roadmap node có thể là skill, project, reading, practice hoặc assessment.
- Progress được tính theo số node đã hoàn thành, có thể kết hợp trọng số priority.

### 16.3 AI Mentor

- AI chỉ đưa khuyến nghị, không thay thế quyết định của sinh viên.
- AI phải dựa trên profile, skill, target role, skill gap, roadmap và GitHub nếu có.
- Nếu dữ liệu thiếu, AI cần hỏi thêm hoặc nói rõ giới hạn.
- Chat history phải được lưu.
- Free user nên bị giới hạn số lượt chat.

### 16.4 Portfolio

- Sinh viên có quyền chọn repo nào hiển thị.
- AI summary phải cho phép chỉnh sửa.
- Portfolio public chỉ hiển thị khi user publish.
- Portfolio slug phải là duy nhất.

### 16.5 Payment

- Không mở khóa Premium nếu chưa nhận webhook thanh toán thành công.
- Không tin dữ liệu thanh toán gửi từ frontend.
- Một webhook chỉ được xử lý một lần.
- Giao dịch thành công phải tạo `PaymentTransaction`.
- Subscription active mới được dùng premium feature.

## 17. Quy Trình Triển Khai

### 17.1 Giai Đoạn 1: Setup Nền Tảng

- Hoàn thiện auth.
- Hoàn thiện JWT.
- Hoàn thiện Google login.
- Hoàn thiện Email OTP.
- Kết nối PostgreSQL.
- Setup EF Core migration.
- Setup seed data cơ bản.

### 17.2 Giai Đoạn 2: Core Student Flow

- Student Profile.
- Skill Input.
- Career Role Selection.
- Skill Gap Analysis.
- Roadmap Generation.
- Roadmap Progress.

### 17.3 Giai Đoạn 3: AI Mentor

- Tích hợp Gemini/OpenAI API.
- Tạo prompt template.
- Lấy context user.
- Lưu `MentorSession`.
- Giới hạn lượt chat theo plan.

### 17.4 Giai Đoạn 4: GitHub Và Portfolio

- Nhập GitHub username.
- Lấy public repo.
- Phân tích README.
- Mapping repo với skill.
- Tạo Portfolio Builder.
- Publish portfolio URL.

### 17.5 Giai Đoạn 5: Payment

- Tạo pricing page.
- Tạo subscription plan.
- Tạo checkout API.
- Tích hợp payment gateway.
- Xử lý webhook.
- Mở khóa premium feature.
- Tạo billing page.

### 17.6 Giai Đoạn 6: Admin, Counselor, Mentor

- Quản lý skill.
- Quản lý career role.
- Quản lý role-skill requirement.
- Quản lý learning resource.
- Quản lý user.
- Quản lý payment.
- Counselor dashboard.
- Mentor review flow.

## 18. Kết Luận

Dự án phù hợp triển khai bằng kiến trúc tách Frontend và Backend:

- React Vite cho giao diện.
- ASP.NET Core Web API cho nghiệp vụ.
- Entity Framework Core cho database migration.
- PostgreSQL làm hệ quản trị dữ liệu chính.
- Firebase Hosting cho frontend.
- Google Cloud Run cho backend.

Database trong tài liệu gốc đúng hướng nhưng cần chuẩn hóa thêm quan hệ, enum, bảng chi tiết skill gap, webhook log và GitHub-skill mapping trước khi triển khai production.

Bản MVP cần ưu tiên core flow: Auth -> Profile -> Skill Input -> Career Role -> Skill Gap -> Roadmap -> Progress -> AI Mentor cơ bản -> GitHub Analysis cơ bản -> Portfolio cơ bản.
