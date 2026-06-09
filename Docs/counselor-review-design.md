# Thiết kế quy trình duyệt Skill & duyệt Roadmap cho Academic Counselor

> Tài liệu mô tả end-to-end hai luồng nghiệp vụ phía Academic Counselor: duyệt kỹ năng (skill verification) và duyệt node lộ trình (roadmap node review). Bao gồm luồng khởi tạo từ phía Student, mô hình trạng thái, hợp đồng API hiện có, các lỗ hổng phát hiện được trong code, và kế hoạch triển khai.

## 1. Bối cảnh & vấn đề

Hệ thống CareerMap cho phép sinh viên tự khai kỹ năng và tự học theo roadmap. Hai câu hỏi nghiệp vụ cốt lõi:

1. Căn cứ vào đâu để sinh roadmap khi kỹ năng sinh viên khai báo chưa được xác nhận?
2. Ai kiểm chứng roadmap/kỹ năng đủ uy tín để sinh viên dựa vào đó mà học?

Phân tích code cho thấy hạ tầng dữ liệu đã sẵn sàng nhưng quy trình còn lỗ hổng:

- `UserSkill` đã có `IsVerified`, `EvidenceUrl`, `EvidenceType`, `VerifiedByUserId`, `VerifiedAt` (`SWP-BE/Models/UserSkill.cs`), nhưng chỉ có một trường `Level` chung, không tách mức tự khai và mức đã xác thực.
- `SkillGapsController` vẫn cộng 50% trọng số cho skill chưa xác thực (`SWP-BE/Controllers/SkillGapsController.cs:121`), nên khai khống vẫn đẩy `MatchScore` ảo lên cao.
- Endpoint verify skill không kiểm tra phân công Counselor (`SWP-BE/Controllers/UserSkillsController.cs:187`), tạo lỗ hổng phân quyền.
- Roadmap node review đã hoàn chỉnh nhưng `ReviewerNote` khi reject còn optional và chưa kiểm tra assignment cho Counselor.
- AI Mentor sinh roadmap (`POST /api/ai-mentor/apply-roadmap`, `SWP-BE/Controllers/AiMentorActionsController.cs:18`) tạo roadmap thẳng `Status = "Active"` (`SWP-BE/Controllers/AiMentorActionsController.cs:102`) — sinh viên học ngay mà không qua Counselor duyệt khung. Đây là lỗ hổng "uy tín roadmap" cần xử lý ở Luồng 2.

## 2. Nguyên tắc nền

- Sinh viên được tự khai skill và `ClaimedLevel`, nhưng tự khai không có giá trị công nhận.
- Chỉ skill `Verified` (có minh chứng, đã duyệt đạt) mới tính vào năng lực và mới được trừ khỏi roadmap.
- Skill không minh chứng hoặc bị từ chối là `Unverified`, không gây tắc, vẫn nằm trong roadmap để học và chứng minh sau.
- Không ép xác thực cái không thể xác thực, không chặn người mới tạo roadmap.
- Căn cứ gốc của roadmap là `RoleSkillRequirement` của nghề mục tiêu, do Admin duyệt — AI không tự bịa skill hay tài nguyên.
- Trạng thái backend là nguồn sự thật duy nhất; FE chỉ phản ánh trạng thái.
## 3. Mô hình trạng thái

### 3.1. Trạng thái của một UserSkill

| Trạng thái | Ý nghĩa | Tính vào năng lực? |
|------------|---------|--------------------|
| `SelfDeclared` | Vừa khai, có `ClaimedLevel`, chưa nộp minh chứng | Không |
| `PendingVerification` | Đã nộp minh chứng (file/GitHub/CV), đang chờ duyệt | Không |
| `Verified` | Người duyệt xác nhận đạt, có `VerifiedLevel` + `VerifiedByUserId` + `VerifiedAt` | Có |
| `Unverified` | Không có minh chứng, hoặc minh chứng bị từ chối | Không |

Chuyển trạng thái hợp lệ:

```
SelfDeclared --(nộp minh chứng)--> PendingVerification
SelfDeclared --(SV tự chấp nhận)--> Unverified
PendingVerification --(duyệt đạt)--> Verified
PendingVerification --(từ chối)--> Unverified
Unverified --(nộp lại minh chứng)--> PendingVerification
Verified --(gỡ xác thực)--> Unverified
```

### 3.2. Trạng thái yêu cầu duyệt khung & Roadmap

Theo Phương án B (đề xuất), khi AI sinh roadmap, hệ thống tạo một `RoadmapApprovalRequest` (giữ payload AI) chứ chưa tạo `Roadmap`. `RoadmapApprovalRequest.Status`: `Pending | Approved | Rejected`.

```
AI Mentor sinh khung --(SV bấm "Gửi yêu cầu duyệt")--> ApprovalRequest: Pending  (chưa có Roadmap)
ApprovalRequest: Pending --(Counselor approve)--> Approved + materialize Roadmap (Status = Active) và gán cho SV
ApprovalRequest: Pending --(Counselor reject)--> Rejected (kèm lý do, không tạo Roadmap)
ApprovalRequest: Rejected --(SV nhờ AI sinh lại)--> ApprovalRequest mới: Pending
```

`Roadmap.Status` (`SWP-BE/Models/Roadmap.cs:11`) giữ nguyên `Draft | Active | Completed` ở Phương án B — roadmap chỉ ra đời khi đã được duyệt nên vào thẳng `Active`. Nếu chọn Phương án A thì cần bổ sung `PendingReview`/`Rejected` vào `Roadmap.Status`. Roadmap sinh theo luồng deterministic (`RoadmapController`) vào thẳng `Active` (chính sách cần chốt — xem 5.3).

### 3.3. Trạng thái của RoadmapNode và ReviewRequest

`RoadmapNode.Status`: `NotStarted -> InProgress -> Completed -> NeedReview -> Verified` (hoặc quay lại `Completed` khi bị từ chối).

`RoadmapNodeReviewRequest.Status` (`SWP-BE/Models/RoadmapNodeReviewRequest.cs`): `Pending | Approved | Rejected | Cancelled`.

```
Node: Completed --(SV gửi review)--> NeedReview + Request: Pending
Request: Pending --(approve)--> Request: Approved + Node: Verified
Request: Pending --(reject)--> Request: Rejected + Node: Completed
Request: Pending --(SV hủy)--> Request: Cancelled
```

## 4. Luồng 1 — Duyệt Skill (Student -> Counselor)

### 4.1. Phía Student (FE)

1. Khai skill + `ClaimedLevel` qua `createUserSkill` (`SWP-FE/src/features/student/skillsApi.js`). Mặc định trạng thái `SelfDeclared`, hiển thị nhãn "Tự đánh giá, chưa xác thực".
2. Nộp minh chứng cho skill muốn được công nhận:
   - Upload file lên Cloud Storage: `uploadUserSkillEvidence` -> `POST /api/storage/user-skills/{userSkillId}/evidence`.
   - Hoặc nhập link (GitHub repo/CV): `importUserSkillEvidenceFromUrl` -> `POST /api/storage/user-skills/{userSkillId}/evidence/import-url`.
   - Trạng thái chuyển `PendingVerification`, tạo bản ghi vào hàng đợi duyệt skill.
3. Skill không nộp minh chứng giữ `SelfDeclared`; sinh viên có thể chủ động đánh dấu `Unverified` để chấp nhận không công nhận và đi tiếp (không bị chặn tạo roadmap).

### 4.2. Phía Counselor (BE + FE)

4. Xem danh sách skill kèm minh chứng của sinh viên được phân công: `GET /api/counselor/students/{studentId}/skills` (`SWP-BE/Controllers/CounselorController.cs:172`). Response đã trả `IsVerified`, `VerifiedByFullName`, `EvidenceUrl`, `EvidenceType`.
5. Ra quyết định tại `POST /api/user-skills/{id}/verify` (`SWP-BE/Controllers/UserSkillsController.cs:187`), role cho phép `AcademicCounselor` + `IndustryMentor`:
   - Duyệt đạt -> `IsVerified = true`, set `VerifiedLevel` (có thể bằng/cao/thấp hơn `ClaimedLevel`), `VerifiedByUserId`, `VerifiedAt`.
   - Từ chối -> chuyển `Unverified`, gửi `Notification` kèm lý do.
6. Hỗ trợ bán tự động: với minh chứng GitHub, dùng `AiReviewSummaryService` (`SWP-BE/Services/AiReviewSummaryService.cs`) gợi ý mức độ. AI chỉ đề xuất, Counselor là người chốt — AI không tự set `Verified`.

### 4.3. Lỗ hổng cần vá (Luồng 1)

- Phân quyền: `POST /api/user-skills/{id}/verify` hiện không kiểm tra sinh viên thuộc `CounselorAssignment` của Counselor đang đăng nhập. Một Counselor bất kỳ có thể verify skill của sinh viên không thuộc mình. Cần thêm truy vấn assignment (Active) và trả `403` nếu không thuộc phân công. Mentor giữ luồng theo lĩnh vực riêng.
- Thiếu hành động gỡ verify: cần `POST /api/user-skills/{id}/unverify` (set `IsVerified=false`, `VerifiedByUserId=null`), tham chiếu logic Mentor (`SWP-BE/Controllers/IndustryMentorController.cs:261`).
- Mô hình dữ liệu: `UserSkill.Level` đang gộp chung. Cần tách `ClaimedLevel` và `VerifiedLevel` để phân biệt tự khai và đã xác thực.
## 5. Luồng 2 — Duyệt Roadmap (AI sinh khung -> Counselor duyệt)

> Đây là luồng kiểm chứng độ uy tín của roadmap: AI Mentor sinh ra khung roadmap, Counselor duyệt khung đó trước khi sinh viên được phép học. Khác với việc xác minh từng node sau khi học (mô tả ở 5.4).

### 5.1. Phía Student + AI Mentor (FE)

1. Sinh viên trò chuyện với AI Mentor; khi đủ ngữ cảnh, AI đề xuất một roadmap (intent `GenerateRoadmap`).
2. Thay cho nút "Áp dụng roadmap" hiện tại, FE hiển thị nút "Gửi yêu cầu duyệt lộ trình". Sinh viên bấm -> gửi đề xuất roadmap (payload AI) lên backend, KHÔNG tạo lộ trình chính thức ngay.
3. Backend lưu đề xuất vào một bản ghi yêu cầu duyệt khung (giữ payload AI: title, mô tả, danh sách node/skill/tài nguyên), trạng thái `Pending`. Sinh viên thấy nhãn "Đang chờ cố vấn duyệt", chưa học được.

### 5.2. Phía Counselor (BE + FE)

4. Yêu cầu duyệt khung vào hàng đợi của Counselor được phân công: `GET /api/counselor/roadmap-approval-queue` (endpoint mới).
5. Counselor mở bản xem trước khung roadmap: danh sách node, skill gắn vào, thứ tự/độ ưu tiên, tài nguyên học. Đối chiếu với `RoleSkillRequirement` của nghề mục tiêu.
6. Quyết định:
   - Approve -> backend chạy logic dựng roadmap (tái dùng `apply-roadmap`) để materialize `Roadmap` + `RoadmapNode`, gán thẳng cho sinh viên với `Status = Active`. Đây là bước "tự gán roadmap vào" sau khi duyệt; gửi `Notification`.
   - Request changes / Reject -> đánh dấu yêu cầu `Rejected` kèm lý do; sinh viên chỉnh mục tiêu, nhờ AI sinh lại và gửi yêu cầu mới. Không có roadmap rác tồn trong hệ thống.
7. Counselor có thể viết nhận xét gắn yêu cầu/roadmap qua `POST /api/counselor/feedback` (`SWP-BE/Controllers/CounselorController.cs:402`).

### 5.3. Lỗ hổng cần vá (Luồng 2)

Hiện luồng duyệt khung này CHƯA tồn tại trong code:

- `apply-roadmap` (`SWP-BE/Controllers/AiMentorActionsController.cs:18`) đang tạo roadmap thẳng `Status = "Active"` (`SWP-BE/Controllers/AiMentorActionsController.cs:102`) và node `NotStarted` (`SWP-BE/Controllers/AiMentorActionsController.cs:315`) -> sinh viên học ngay, không qua Counselor.
- Cần tách `apply-roadmap` thành hai bước: (a) nhận đề xuất + lưu yêu cầu duyệt; (b) materialize + gán roadmap khi Counselor approve. FE đổi nút "Áp dụng" thành "Gửi yêu cầu duyệt lộ trình".
- Hai phương án lưu trữ:
  - Phương án A — tạo `Roadmap` + `RoadmapNode` ngay ở `Status = PendingReview`, ẩn khỏi danh sách lộ trình đang học; approve chỉ lật `Active`. Cần thêm `PendingReview`/`Rejected` vào `Roadmap.Status` (`SWP-BE/Models/Roadmap.cs:11`) + lọc trạng thái khắp các truy vấn roadmap của sinh viên.
  - Phương án B (đề xuất) — KHÔNG tạo roadmap khi gửi; chỉ lưu bản ghi `RoadmapApprovalRequest` giữ payload AI. Approve mới materialize + gán `Active`. Sạch hơn, không có roadmap "lửng"; đổi lại Counselor review trên bản xem trước từ payload, cần endpoint preview.
- Chưa có endpoint hàng đợi duyệt khung (`roadmap-approval-queue`) và approve/reject khung roadmap cho Counselor.
- Chính sách cần quyết: roadmap sinh theo luồng deterministic (`RoadmapController`) có cần duyệt khung không, hay tự `Active` vì đã bám `RoleSkillRequirement` Admin duyệt. Đề xuất: chỉ roadmap do AI Mentor sinh mới bắt buộc qua duyệt khung.

### 5.4. Xác minh tiến độ từng node (sau khi roadmap đã Active)

Sau khi khung được duyệt và sinh viên học, từng node có thể được xác minh để công nhận tiến độ (đây là cơ chế đã có sẵn trong code):

1. Sinh viên hoàn thành lesson (`markLessonCompleted`, `SWP-FE/src/features/student/roadmapApi.js`); node đạt `Completed`.
2. Gửi review node kèm minh chứng qua modal (`SWP-FE/src/features/student/components/NodeReviewRequestModal.jsx:151`) -> tạo `RoadmapNodeReviewRequest` `Pending`, node `Completed -> NeedReview` (`SWP-BE/Controllers/RoadmapReviewController.cs:128`).
3. Counselor xem hàng đợi `GET /api/counselor/roadmap-review-queue` (`SWP-BE/Controllers/RoadmapReviewController.cs:447`).
4. Approve `POST /api/roadmap-node/review-requests/{requestId}/approve` (`SWP-BE/Controllers/RoadmapReviewController.cs:456`): node -> `Verified`; nếu node có `SkillId` thì tự verify skill qua `SyncVerifiedSkillAsync` (`SWP-BE/Controllers/RoadmapReviewController.cs:493`) — cầu nối sang Luồng 1; node `Group` cascade verify node con (`SWP-BE/Controllers/RoadmapReviewController.cs:502`).
5. Reject `POST .../reject` (`SWP-BE/Controllers/RoadmapReviewController.cs:572`): node về `Completed` để sửa và gửi lại. Cần bắt buộc `ReviewerNote` (hiện optional) và check assignment cho Counselor (`SWP-BE/Controllers/RoadmapReviewController.cs:475`).

## 6. Cổng tạo Roadmap & cách tính điểm

### 6.1. Cổng kiểm tra khi tạo roadmap

Khi sinh viên bấm tạo roadmap (`generateRoadmap` -> `POST /api/roadmap/generate`), backend kiểm tra không còn skill nào ở trạng thái lấp lửng:

- Còn skill `PendingVerification` -> chặn, báo "Bạn có N kỹ năng đang chờ xác thực, hãy đợi duyệt hoặc chuyển sang chưa xác thực trước khi tạo lộ trình."
- Mọi skill đã khai phải về một trong hai kết cục `Verified` hoặc `Unverified` mới cho tạo.
- Sinh viên khai 0 skill vẫn qua cổng bình thường (không bị chặn).

### 6.2. Căn cứ sinh node

- Khung roadmap bám vào `RoleSkillRequirement` của nghề mục tiêu (Admin duyệt), không phải AI tự quyết.
- Thứ tự node sắp xếp theo `SkillPrerequisite` + `Priority` (topological order).
- Tài nguyên học gắn vào node là `LearningResource` do Admin nhập.
- Chỉ skill `Verified` đạt `VerifiedLevel` yêu cầu mới được trừ khỏi roadmap. Skill `Unverified`/`SelfDeclared` vẫn nằm trong roadmap dưới nhãn "chưa xác thực".

### 6.3. Cách tính điểm (thay đổi so với hiện trạng)

Hiện tại `SWP-BE/Controllers/SkillGapsController.cs` tính một `MatchScore` duy nhất và cộng 50% trọng số cho skill chưa xác thực (`SkillGapsController.cs:121`). Thiết kế mới:

- Bỏ cộng 50% cho skill chưa verified.
- Tách thành hai con số:
  - `VerifiedMatchScore`: chỉ tính từ skill `Verified` — đây là con số chính, đáng tin.
  - `SelfReportedMatchScore`: gồm cả tự khai — chỉ để tham khảo, hiển thị riêng.
- `ClaimedLevel` chưa xác thực chỉ ảnh hưởng gợi ý thứ tự học, không trừ node, không tính điểm chính.

### 6.4. Tiến triển và tái đánh giá

- Trong lúc học, sinh viên bổ sung minh chứng cho skill `Unverified` để nâng lên `Verified`; mỗi lần verify, `VerifiedMatchScore` tăng và roadmap co lại.
- Khi yêu cầu nghề đổi (`RoleSkillRequirement` cập nhật, cờ `IsOutdated`), hệ thống gợi ý regenerate. Level đã `Verified` được giữ, không bắt xác thực lại.
## 7. Thay đổi kỹ thuật theo thành phần

### Backend

- `Models/UserSkill.cs`: tách `Level` thành `ClaimedLevel` và `VerifiedLevel`; tận dụng `IsVerified`, `EvidenceUrl`, `EvidenceType`, `VerifiedByUserId`, `VerifiedAt` đã có. Cần migration EF Core.
- `Controllers/UserSkillsController.cs`: thêm check `CounselorAssignment` (Active) trong `verify`; thêm endpoint `unverify`; bắt buộc/cảnh báo minh chứng trước khi verify.
- `Controllers/SkillGapsController.cs`: bỏ trọng số 50% cho skill chưa verified; tách `VerifiedMatchScore` và `SelfReportedMatchScore`.
- `Controllers/RoadmapController.cs`: thêm cổng kiểm tra `PendingVerification` ở `Generate`; điều kiện cắt node chỉ dựa trên `VerifiedLevel`.
- Luồng duyệt khung (Phương án B): thêm model `RoadmapApprovalRequest` (payload AI + `Status` Pending/Approved/Rejected + lý do). Đổi `apply-roadmap` (`SWP-BE/Controllers/AiMentorActionsController.cs:18`): thay vì tạo roadmap ngay, lưu `RoadmapApprovalRequest` `Pending`. Khi Counselor approve, tái dùng logic dựng node để materialize `Roadmap`+`RoadmapNode` (`Status = Active`) và gán cho sinh viên. Endpoint mới: `GET /api/counselor/roadmap-approval-queue`, `GET /api/counselor/roadmap-approval-requests/{id}` (preview), `POST .../{id}/approve`, `POST .../{id}/reject`. Cần migration EF Core cho bảng mới.
- `Controllers/RoadmapReviewController.cs`: bắt buộc `ReviewerNote` khi reject; kiểm tra assignment cho Counselor.
- Hàng đợi duyệt skill: bổ sung mô hình/endpoint song song với review node (có thể tái dùng mẫu `RoadmapNodeReviewRequest`).
- `Notification`: phát thông báo cho cả hai luồng (verified/rejected, approve/reject).

### Frontend

- Trang kỹ năng (`SWP-FE/src/features/student/components/StudentSkillsPage.jsx`): badge trạng thái (`SelfDeclared`/`PendingVerification`/`Verified`/`Unverified`), nút nộp minh chứng, nút tự đánh dấu `Unverified`.
- Màn Counselor: nút Xác nhận/Từ chối skill, hiển thị minh chứng, lý do reject.
- Hiển thị hai con số `VerifiedMatchScore` và `SelfReportedMatchScore` tách bạch.
- AI Mentor: đổi nút "Áp dụng roadmap" thành "Gửi yêu cầu duyệt lộ trình". Sinh viên thấy đề xuất ở trạng thái "Đang chờ cố vấn duyệt", chưa học được. Counselor có màn xem trước khung (node/skill/tài nguyên) với nút Duyệt/Từ chối kèm lý do; Duyệt xong roadmap mới xuất hiện và active cho sinh viên.
- Modal gửi review node (`SWP-FE/src/features/student/components/NodeReviewRequestModal.jsx`): hiển thị lý do reject để sinh viên sửa.

## 8. Kế hoạch triển khai (thứ tự ưu tiên)

1. Vá phân quyền verify skill (Luồng 1) — rủi ro bảo mật, ưu tiên cao nhất.
2. Tách `ClaimedLevel`/`VerifiedLevel` + thêm endpoint `unverify` + trạng thái xác thực (migration EF Core).
3. Cổng `PendingVerification` ở `RoadmapController.Generate`; cắt node theo `VerifiedLevel`.
4. Sửa `SkillGapsController`: tách hai điểm, bỏ trọng số 50%.
5. Luồng 2 (duyệt khung roadmap, Phương án B): thêm model `RoadmapApprovalRequest`, đổi `apply-roadmap` sang lưu yêu cầu `Pending`, approve mới materialize + gán roadmap `Active`; thêm endpoint approval-queue/preview/approve/reject + đổi nút FE (migration EF Core).
6. Xác minh tiến độ node (5.4): bắt buộc lý do reject + check assignment.
7. Audit log + `Notification` cho cả hai luồng.
8. FE: nút xác nhận/từ chối skill, badge trạng thái, màn duyệt khung roadmap, hiển thị lý do reject, hai con số match.

## 9. Kiểm thử

- Unit:
  - Phân quyền verify skill: Counselor trong/ngoài assignment (in -> 200, out -> 403).
  - Chuyển trạng thái node approve/reject; cascade verify node con của `Group`.
  - `SyncVerifiedSkillAsync` khi approve node có `SkillId`.
  - Cổng `PendingVerification` ở `Generate` (còn pending -> chặn).
  - Tính `VerifiedMatchScore` (bỏ trọng số 50%).
  - Gửi yêu cầu duyệt lộ trình: tạo `RoadmapApprovalRequest` `Pending`, KHÔNG tạo `Roadmap` (count roadmap của SV không đổi).
  - Counselor approve: materialize `Roadmap`+`RoadmapNode` `Active` và gán cho SV; approve lại lần nữa không tạo trùng.
  - Counselor reject: yêu cầu -> `Rejected` kèm lý do bắt buộc, không tạo `Roadmap`.
  - Phân quyền duyệt khung: chỉ Counselor được phân công (Active) mới thấy/duyệt yêu cầu của SV đó.
- Integration (full flow):
  - Student khai skill -> nộp minh chứng -> Counselor verify -> AI Mentor sinh roadmap -> SV gửi yêu cầu duyệt lộ trình -> Counselor approve -> roadmap được materialize & gán `Active` -> học & gửi review node -> approve -> skill gap tính lại bằng `VerifiedMatchScore`.

## 10. Điểm còn để mở (quyết khi triển khai)

- SLA duyệt minh chứng để tránh sinh viên chờ lâu ở `PendingVerification`.
- Mức độ dùng GitHub/AI hỗ trợ duyệt (đề xuất vs tự động).
- Roadmap deterministic (`RoadmapController.Generate`) có cần qua duyệt khung như roadmap AI không, hay tự `Active` vì đã bám `RoleSkillRequirement` Admin duyệt.
