# Tài liệu 5 Luồng Kiểm tra Nghiệp vụ End-to-End (E2E) — CareerMap

Tài liệu này mô tả chi tiết **5 luồng kiểm tra hành trình người dùng (E2E Test Flows)** hoàn chỉnh xuyên suốt các vai trò (roles) trong hệ thống CareerMap. Tất cả các endpoint API và cấu trúc dữ liệu đã được xác thực và đối chiếu chính xác với mã nguồn thực tế của backend C#.

> [!NOTE]
> **Điều kiện chuẩn bị chung (Prerequisites):**
> - Cơ sở dữ liệu đã chạy đầy đủ các file Migrations (bao gồm các bảng liên quan đến Review Requests, Notifications, Counselor Feedback, v.v.)
> - Đã tạo sẵn tài khoản tương ứng với các role: ít nhất 1 Admin, 1 Academic Counselor, 1 Industry Mentor, và 2 Students.
> - Cấu hình `SMTP` hoạt động (nếu không cấu hình, email thông báo sẽ báo log warning nhưng luồng nghiệp vụ chính vẫn tiếp tục).
> - Gemini `AI:ApiKey` đã được cấu hình trong `appsettings.json` (phục vụ luồng #1 và #4).
> - PayOS Client (ClientId, ApiKey, ChecksumKey) đã được cấu hình (phục vụ luồng #3).

---

## Luồng 1 — Đăng ký Học viên → Đánh giá Kỹ năng → Sinh Lộ trình học (Student Onboarding → AI Mentor → Roadmap)

**Người tham gia:** Học viên mới (Student)
**Mục tiêu:** Student đăng ký → xác thực OTP → tạo hồ sơ cá nhân → thực hiện đánh giá khoảng cách kỹ năng (Skill Gap) → AI Virtual Mentor tư vấn và áp dụng lộ trình học tập.

### Sơ đồ các bước thực hiện chi tiết
```
[Student] Điền thông tin vào form đăng ký tài khoản (Email, Password, FullName)
       ↓
[FE] Gọi POST /api/auth/register
       ↓
[BE: AuthController] Tạo bản ghi tạm thời (PendingRegistration), tạo mã OTP và gửi qua email
       ↓
[Student] Nhận OTP qua email học tập → Nhập vào giao diện xác thực
       ↓
[FE] Gọi POST /api/auth/verify-email
       ↓
[BE: AuthController] Xác thực OTP thành công → Chuyển sang User chính thức (Role: Student) → Trả về JWT Access + Refresh Tokens
       ↓
[Student] Click "Cập nhật hồ sơ" → Điền Major, GPA, Target Role, GithubUsername
       ↓
[FE] Gọi POST /api/profile
       ↓
[BE: ProfileController] Lưu thông tin học tập và mục tiêu vào bảng StudentProfile
       ↓
[Student] Chuyển qua tab "Kỹ năng" → Bấm chọn "Tạo báo cáo Skill Gap"
       ↓
[FE] Gọi POST /api/skill-gap/analyze
       ↓
[BE: SkillGapsController] So sánh kỹ năng hiện tại với kỹ năng yêu cầu của Target Role → Sinh SkillGapReport & Items
       ↓
[Student] Chuyển qua tab "AI Mentor" → Khởi tạo phiên hội thoại
       ↓
[FE] Gọi GET /api/mentor/sessions + GET /api/mentor/quota (Kiểm tra quota mặc định = 20)
       ↓
[Student] Click chip gợi ý "Tạo lộ trình học tập" hoặc gõ câu hỏi yêu cầu sinh lộ trình
       ↓
[FE] Gọi POST /api/mentor/chat
       ↓
[BE: MentorController] Gom Context (Hồ sơ, kỹ năng, skill gap) gửi Gemini API → Trả về QnA kèm đề xuất suggestions.roadmap
       ↓
[Student] Xem trước lộ trình học dạng cây đề xuất → Bấm nút "✓ Áp dụng lộ trình này"
       ↓
[FE] Gọi POST /api/ai-mentor/apply-roadmap
       ↓
[BE: AiMentorActionsController] Khởi tạo bảng Roadmap (Status: Active) & phân cấp các RoadmapNodes (Status: NotStarted)
       ↓
[Student] Hệ thống tự động chuyển hướng đến màn hình sơ đồ Lộ trình học cá nhân dạng cây (GET /api/roadmap/{id})
```

### Kiểm tra trạng thái hệ thống & Cơ sở dữ liệu
- **Database:** Bảng `Roadmaps` tăng thêm 1 bản ghi mới có `Status = 'Active'`. Bảng `RoadmapNodes` tăng số lượng bản ghi tương ứng với số node học tập do AI đề xuất. Bảng `MentorSessions` lưu lịch sử câu hỏi và câu trả lời dưới dạng JSON envelope.
- **Quota:** Gọi lại `GET /api/mentor/quota` kiểm tra `Used` tăng thêm 1, `Remaining` giảm đi 1.

### Xử lý các Edge Cases & Xác minh nhanh
- ❌ **Gemini API key chưa được cấu hình:** API trả về mã lỗi `503 Service Unavailable` kèm toast thông báo lỗi cấu hình API key.
- ❌ **Học viên dùng hết quota lượt chat:** API trả về mã lỗi `402 Payment Required` kèm thông điệp yêu cầu nâng cấp gói tài khoản.
- ❌ **AI trả về dữ liệu không đúng định dạng JSON chuẩn:** Hệ thống tự động kích hoạt cơ chế fallback, hiển thị toàn bộ phản hồi dưới dạng văn bản QnA thông thường và không hiển thị card áp dụng lộ trình.

---

## Luồng 2 — Phê duyệt Node Lộ trình (Student → Mentor/Counselor Review)

**Người tham gia:** Học viên (Student) + Cố vấn học thuật (Academic Counselor) hoặc Mentor doanh nghiệp (Industry Mentor).
**Mục tiêu:** Học viên hoàn thành module học tập → gửi yêu cầu phê duyệt kèm minh chứng (Evidence) → Người đánh giá duyệt hoặc từ chối yêu cầu → Nhận thông báo đa kênh.

### Sơ đồ các bước thực hiện chi tiết — Phê duyệt Node đơn lẻ (Single Node Review)
```
[Student] Học tập & cập nhật trạng thái Node kỹ năng trên lộ trình sang "Completed" hoặc "InProgress"
       ↓
[FE] Gọi PUT /api/roadmap-node/{id}/status
       ↓
[BE: RoadmapController] Cập nhật trạng thái RoadmapNode học tập của học viên
       ↓
[Student] Chọn cập nhật trạng thái Node sang "NeedReview" để gửi yêu cầu đánh giá
       ↓
[FE] Gọi GET /api/roadmap-node/{id}/available-reviewers
       ↓
[BE: RoadmapReviewController] Trả về danh sách Cố vấn gán trực tiếp (Assigned) và các Mentors khả dụng
       ↓
[Student] Chọn file minh chứng (ZIP/PDF/Image < 25MB) từ máy tính
       ↓
[FE] Gọi POST /api/storage/roadmap-evidence (multipart/form-data)
       ↓
[BE: Storage] Lưu trữ file minh chứng lên Cloud Storage thành công → Trả về thông tin ObjectName, tên file
       ↓
[Student] Nhập ghi chú gửi người phê duyệt → Bấm nút "Gửi yêu cầu"
       ↓
[FE] Gọi POST /api/roadmap-node/{id}/review-requests
       ↓
[BE: RoadmapReviewController] Tạo mới RoadmapNodeReviewRequest ở trạng thái Pending → Gửi Notification cho Reviewer
       ↓
[Reviewer] Đăng nhập hệ thống → Nhấn biểu tượng thông báo hoặc tab Duyệt lộ trình
       ↓
[FE] Gọi GET /api/notifications?take=30 hoặc API lấy hàng đợi duyệt
       ↓
[BE: NotificationsController] Trả về danh sách thông báo và đường dẫn chuyển hướng duyệt
       ↓
[Reviewer] Xem chi tiết yêu cầu → Click tải file minh chứng của Student
       ↓
[FE] Gọi GET /api/roadmap-node/review-requests/{requestId}/evidence-url
       ↓
[BE: RoadmapReviewController] Trả về Signed URL (hiệu lực trong 15 phút) tải xuống minh chứng an toàn
       ↓
[Reviewer] Kiểm tra tài liệu → Nhập nhận xét đánh giá (ReviewerNote) và đưa ra quyết định
       ↓
 Nhánh 1: Đồng ý phê duyệt                            Nhánh 2: Từ chối yêu cầu
       ↓                                                     ↓
[FE] Gọi .../review-requests/{id}/approve             [FE] Gọi .../review-requests/{id}/reject
       ↓                                                     ↓
[BE] Cập nhật Node sang 'Verified'                    [BE] Hoàn tác Node về trạng thái 'Completed'
     Request chuyển sang 'Approved'                        Request chuyển sang 'Rejected'
     Gửi Notification & Email đến Student                  Gửi Notification & Email đến Student
```

### Sơ đồ các bước thực hiện chi tiết — Phê duyệt nhóm Node (Group Node Review - Tiết kiệm Quota)
```
[Student] Hoàn thành tất cả các Node module con thuộc một Group Node (Level 0) trên lộ trình
       ↓
[FE] Gửi các request cập nhật trạng thái Node con sang "Completed" (PUT /api/roadmap-node/{id}/status)
       ↓
[Student] Bấm nút "Yêu cầu duyệt nhóm" hiển thị trên Group Node cha → Tải lên file ZIP tổng hợp minh chứng
       ↓
[FE] Gọi POST /api/roadmap-node/{groupId}/review-requests
       ↓
[BE: RoadmapReviewController] Tạo yêu cầu review duy nhất gắn với Group Node ID. Set Group Node sang 'NeedReview'
       ↓
[Reviewer] Kiểm tra file tổng hợp minh chứng của nhóm → Bấm nút "Phê duyệt"
       ↓
[FE] Gọi POST /api/roadmap-node/review-requests/{requestId}/approve
       ↓
[BE: RoadmapReviewController] Cơ chế Cascade: Tự động cập nhật Group Node & tất cả Node con sang 'Verified'
```

### Kiểm tra trạng thái hệ thống & Cơ sở dữ liệu
- **Database:** Bảng `RoadmapNodeReviewRequests` cập nhật trạng thái tương ứng (`Approved`/`Rejected`/`Cancelled`). Bảng `Notifications` tăng bản ghi cho cả học viên và cố vấn.
- **Tiến độ lộ trình:** Sau khi Approve thành công, status của `RoadmapNode` chuyển sang `Verified` và thuộc tính `Progress` của bảng `Roadmaps` được cập nhật tính lại phần trăm dựa trên số node Verified/Completed.

### Xử lý các Edge Cases & Xác minh nhanh
- ❌ **Cố vấn chưa được gán cho học viên:** Sẽ không hiển thị trong danh sách người phê duyệt khả dụng (`available-reviewers`). Nếu gọi trực tiếp API gửi yêu cầu, backend sẽ chặn lại và trả về lỗi `400 BadRequest` ("counselor is not assigned to you").
- ❌ **Kích thước file minh chứng vượt quá 25MB:** Backend chặn trực tiếp và trả về lỗi `400 BadRequest` ("File exceeds 25 MB limit").
- ⚠️ **Gửi yêu cầu phê duyệt lần thứ hai trên cùng một node:** Yêu cầu `Pending` cũ trước đó sẽ tự động chuyển trạng thái thành `Cancelled` để tránh trùng lặp phê duyệt.

---

## Luồng 3 — Nâng cấp Gói tài khoản (Admin → Student → PayOS Payment Integration)

**Người tham gia:** Admin + Học viên (Student)
**Mục tiêu:** Admin quản trị tạo gói dịch vụ → Học viên thực hiện thanh toán nâng cấp tài khoản qua cổng PayOS → Webhook cập nhật gói dịch vụ tự động → Học viên có quota đánh giá và review mới.

### Sơ đồ các bước thực hiện chi tiết
```
[Admin] Đăng nhập trang quản trị → Bấm "Tạo gói dịch vụ mới" (Premium)
       ↓
[FE] Gọi POST /api/admin/subscription-plans
       ↓
[BE: Admin] Tạo gói mới trong database (Tên gói, Giá bán VND, Billing Cycle, FeaturesJson, IsActive = true)
       ↓
[Student] Vào giao diện mua gói dịch vụ / Nâng cấp tài khoản
       ↓
[FE] Gọi GET /api/subscription-plans
       ↓
[BE: SubscriptionsController] Trả về danh sách gói dịch vụ có trạng thái IsActive = true
       ↓
[Student] Chọn mua gói Premium → Click "Thanh toán ngay"
       ↓
[FE] Gọi POST /api/subscriptions/checkout
       ↓
[BE: SubscriptionsController] Khởi tạo Subscription (Status: Pending) & PaymentTransaction (Status: Created)
                              Gọi cổng thanh toán PayOS tạo Link thanh toán
       ↓
[Student] Tự động chuyển hướng từ app sang giao diện thanh toán cổng PayOS
       ↓
[Student] Dùng app ngân hàng quét mã QR Code hiển thị để thực hiện chuyển khoản thanh toán
       ↓
[PayOS] Nhận tiền chuyển khoản thành công từ ngân hàng liên kết
       ↓
[PayOS] Gửi tín hiệu Webhook tự động tức thì về phía Backend của CareerMap
       ↓
[FE/Server] Gọi POST /api/payos/webhook (Webhook body chứa dữ liệu giao dịch mã hóa & signature)
       ↓
[BE: PayOsController] Giải mã và xác thực chữ ký signature thành công
                      Gọi PaymentProcessingService cập nhật giao dịch sang 'Paid'
                      Kích hoạt trạng thái Subscription sang 'Active' (Cấp StartedAt & ExpiredAt)
       ↓
[Student] PayOS tự động chuyển hướng trình duyệt học viên trở lại trang kết quả của CareerMap (PaymentResultPage)
       ↓
[Student] Quay lại Dashboard kiểm tra thông tin tài khoản vừa nâng cấp
       ↓
[FE] Gọi GET /api/subscriptions/me + GET /api/mentor/quota
       ↓
[BE: Subscriptions] Trả về Subscription đang hoạt động (Active).
                     Quota chat AI chuyển thành không giới hạn (-1) và tăng số lượt nhờ duyệt lộ trình
```

### Kiểm tra trạng thái hệ thống & Cơ sở dữ liệu
- **Database:** Bản ghi trong bảng `Subscriptions` chuyển sang `Status = 'Active'` liên kết đúng UserId và PlanId. Bản ghi trong bảng `PaymentTransactions` chuyển sang `Status = 'Paid'`, lưu trữ mã giao dịch `ProviderTransactionId` tương ứng.
- **Quota:** Các API kiểm tra quota chat và review cập nhật hạn mức sử dụng theo cấu hình của gói dịch vụ vừa mua.

### Xử lý các Edge Cases & Xác minh nhanh
- ❌ **Gói dịch vụ có đơn giá bằng 0đ (Free Plan):** API checkout tự động nhận diện, tạo bản ghi `Subscription` và kích hoạt trạng thái `Active` ngay lập tức mà không cần chuyển tiếp qua cổng PayOS.
- ❌ **Webhook của PayOS bị gọi chậm hoặc lỗi kết nối:** Nếu người dùng chuyển khoản thành công nhưng webhook chưa gửi tới, trạng thái Subscription vẫn giữ là `Pending`. Cần cơ chế quét giao dịch thủ công hoặc gọi API đồng bộ trạng thái khi người dùng tải lại trang kết quả.
- ⚠️ **Webhook gửi trùng lặp (Idempotent Webhook):** Backend lưu trữ sự kiện webhook vào bảng `PaymentWebhookEvents`. Khi nhận webhook mới, backend kiểm tra `EventId` (hoặc `Reference`), nếu đã tồn tại thì bỏ qua để tránh cộng trùng hoặc xử lý trùng giao dịch.

---

## Luồng 4 — Tư vấn Chuyên sâu với AI Virtual Mentor (Student-only Intelligent Flow)

**Người tham gia:** Học viên (Student)
**Mục tiêu:** Tương tác với AI Mentor để nhận phản hồi cá nhân hóa (được tổng hợp từ toàn bộ dữ liệu hồ sơ cá nhân, các kỹ năng, báo cáo skill gap, lộ trình học tập, lịch sử đánh giá của cố vấn và dự án từ GitHub).

### Sơ đồ các bước thực hiện chi tiết
```
[Student] Truy cập giao diện "AI Mentor"
       ↓
[FE] Gọi GET /api/mentor/sessions + GET /api/mentor/quota
       ↓
[BE: MentorController] Trả về lịch sử phiên hội thoại cũ & hạn mức chat khả dụng của học viên
       ↓
[Student] Nhập câu hỏi tư vấn chuyên sâu (Ví dụ: "Đánh giá các dự án trên GitHub của tôi")
       ↓
[FE] Gọi POST /api/mentor/chat
       ↓
[BE: MentorController] Thu thập Context toàn diện của Student gửi làm prompt đầu vào cho Gemini API:
                      1. Hồ sơ học vấn (Chuyên ngành, GPA, Target Role...)
                      2. Danh sách kỹ năng hiện tại
                      3. Báo cáo Skill Gap mới nhất (Match Score)
                      4. Lộ trình học tập (Roadmap, RoadmapNodes)
                      5. 5 nhận xét gần nhất từ Cố vấn & Industry Mentor
                      6. Thông tin e-Portfolio và kho lưu trữ dự án GitHub
       ↓
[BE: Gemini Service] AI xử lý dữ liệu tổng hợp cá nhân hóa, sinh phản hồi Markdown kèm gợi ý suggestions.roadmap
       ↓
[Student] Nhận câu trả lời hiển thị dạng text/code block kèm theo card Lộ trình bổ sung đề xuất
       ↓
[Student] Click xem chi tiết lộ trình bổ sung và chọn "✓ Áp dụng lộ trình này"
       ↓
[FE] Gọi POST /api/ai-mentor/apply-roadmap
       ↓
[BE: AiMentorActionsController] Lưu lộ trình mới vào Database dưới trạng thái Active song song để bắt đầu học tập
```

### Kiểm tra trạng thái hệ thống & Cơ sở dữ liệu
- **Database:** Bảng `MentorSessions` lưu trữ bản ghi chat mới. Cột `ContextJson` chứa toàn bộ snapshot dữ liệu học viên tại thời điểm hỏi để phục vụ việc phân tích chi tiết.
- **Hạn mức:** Lượt chat của tài khoản được trừ chính xác 1 đơn vị.

### Xử lý các Edge Cases & Xác minh nhanh
- ❌ **Thời gian phản hồi AI (Timeout):** Backend bắt lỗi timeout của Gemini API, trả về mã lỗi `503 Service Unavailable` và không trừ quota chat của học viên.
- ❌ **AI trả về nội dung text thường (không phải cấu trúc JSON envelope yêu cầu):** Backend sử dụng cơ chế xử lý ngoại lệ, tự động gán nội dung text thường đó vào phần `answer`, các mục gợi ý (`suggestions`) trả về null để giao diện vẫn hiển thị được nội dung chat cho người dùng.

---

## Luồng 5 — Chỉ định Cố vấn & Tương tác Cố vấn học thuật (Counselor Assignment & Feedback Loop)

**Người tham gia:** Admin + Academic Counselor + Học viên (Student)
**Mục tiêu:** Admin phân công cố vấn học thuật cho sinh viên → Cố vấn theo dõi tiến trình và gửi nhận xét → Học viên nhận thông báo và dữ liệu feedback được cập nhật vào Context của AI Virtual Mentor.

### Sơ đồ các bước thực hiện chi tiết
```
[Admin] Đăng nhập bảng điều khiển admin → Click "Tạo phân công cố vấn học thuật"
       ↓
[FE] Gọi POST /api/admin/counselor-assignments
       ↓
[BE: AdminController] Tạo liên kết CounselorAssignment ở trạng thái Active cho cặp CounselorId & StudentId
       ↓
[Counselor] Đăng nhập → Truy cập tab "Danh sách sinh viên"
       ↓
[FE] Gọi GET /api/counselor/students
       ↓
[BE: CounselorController] Trả về danh sách học viên cố vấn được phân công quản lý
       ↓
[Counselor] Bấm chọn 1 học viên bất kỳ để đánh giá
       ↓
[FE] Gọi đồng thời các API lấy thông tin học tập của học viên:
     GET /api/counselor/students/{studentId}/profile
     GET /api/counselor/students/{studentId}/skills
     GET /api/counselor/students/{studentId}/skill-gap
     GET /api/counselor/students/{studentId}/roadmap
       ↓
[Counselor] Đọc dữ liệu tiến độ lộ trình & hồ sơ → Nhấn "Tạo Feedback"
       ↓
[Student] Nhập điểm rating (1-5), nhận xét chi tiết, khuyến nghị học tập cho sinh viên
       ↓
[FE] Gọi POST /api/counselor/feedback
       ↓
[BE: CounselorController] Xác minh quyền hạn phân công hợp lệ → Lưu bản ghi vào bảng CounselorFeedback
       ↓
[Student] Vào chat AI Mentor, hỏi: "Có nhận xét gì mới từ cố vấn của tôi không?"
       ↓
[FE] Gọi POST /api/mentor/chat
       ↓
[BE: MentorController] Tự động lấy 5 feedbacks cố vấn gần nhất trong database làm context đầu vào gửi AI
                       AI xử lý dữ liệu và trích xuất trả lời chính xác học viên những góp ý cố vấn đã gửi
```

### Kiểm tra trạng thái hệ thống & Cơ sở dữ liệu
- **Database:** Bảng `CounselorAssignments` chứa bản ghi chỉ định hoạt động với cặp khóa duy nhất (CounselorId, StudentId). Bảng `CounselorFeedbacks` lưu trữ đúng thông tin cố vấn, học viên và các trường nhận xét chuyên môn.
- **AI Context Integration:** Đầu vào context của AI Mentor hiển thị đầy đủ chuỗi text dạng `[Counselor yyyy-MM-dd] rating=X, feedback=Y, recommendations=Z`.

### Xử lý các Edge Cases & Xác minh nhanh
- ❌ **Admin thực hiện gán trùng lặp Cố vấn và Sinh viên đã có phân công trước đó:** Backend kiểm tra nếu đã tồn tại bản ghi cũ thì sẽ cập nhật trạng thái bản ghi đó thành `Active` và lưu lại thông tin cập nhật mới chứ không tạo thêm bản ghi trùng lặp (tránh xung đột database).
- ❌ **Cố vấn đã bị Admin hủy phân công (Status = 'Inactive') cố gắng viết feedback cho sinh viên:** API viết feedback kiểm tra quyền hạn thực tế, trả về mã lỗi `403 Forbidden` do mối quan hệ phân công không còn hiệu lực.
- 💡 **Khuyến nghị cải thiện trải nghiệm học tập (Gap UI):** Hiện tại hệ thống chưa thiết kế trang danh sách riêng hiển thị Counselor Feedbacks cho học viên tự đọc trực tiếp trên giao diện Dashboard. Đề xuất phát triển bổ sung một tab nhỏ có tên "Nhận xét từ cố vấn học thuật" ở giao diện trang Dashboard của Học viên để học viên xem nhanh mà không cần hỏi thông qua AI Virtual Mentor.
