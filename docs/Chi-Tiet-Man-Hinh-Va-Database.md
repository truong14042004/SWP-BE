# CHI TIẾT TỪNG MÀN HÌNH VÀ DATABASE - CAREER ORIENTATION PLATFORM

## MỤC LỤC
1. [Authentication Screens](#1-authentication-screens)
2. [Student Screens](#2-student-screens)
3. [Admin Screens](#3-admin-screens)
4. [Counselor Screens](#4-counselor-screens)
5. [Mentor Screens](#5-mentor-screens)
6. [Database Schema](#6-database-schema)

---

# 1. AUTHENTICATION SCREENS

## 1.1 REGISTER SCREEN

### Mô tả
Màn hình đăng ký tài khoản mới cho sinh viên

### UI Components
- Form fields:
  - Email (input, required, email validation)
  - Password (input, required, min 6 chars)
  - Confirm Password (input, required, must match)
  - Full Name (input, required)
- Buttons:
  - "Register" button
  - "Login" link (chuyển sang login screen)
  - "Sign up with Google" button

### User Actions & Flow

#### Action 1: Đăng ký bằng Email/Password
```
1. User nhập email, password, confirm password, full name
2. Click "Register" button
3. Frontend validate:
   - Email format hợp lệ
   - Password >= 6 ký tự
   - Confirm password khớp
   - Full name không rỗng
4. Frontend gọi API:
```

**API Call:**
```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "student@example.com",
  "password": "password123",
  "fullName": "Nguyen Van A"
}
```

**Backend Process:**
```csharp
// AuthController.Register()
1. Validate input
2. Check email đã tồn tại chưa (query Users table)
3. Hash password (PasswordHasher)
4. Generate OTP (6 digits)
5. Hash OTP
6. Create User entity:
   - Id = Guid.NewGuid()
   - Email = email
   - PasswordHash = hashedPassword
   - FullName = fullName
   - Role = "Student"
   - IsEmailVerified = false
   - EmailVerificationOtpHash = hashedOtp
   - EmailVerificationOtpExpiresAt = now + 10 minutes
   - IsActive = true
   - CreatedAt = now
   - UpdatedAt = now
7. Save to database (Users table)
8. Send OTP email (SmtpEmailSender)
9. Return success message
```

**Database Operations:**
```sql
-- Check email exists
SELECT COUNT(*) FROM "Users" WHERE "Email" = @email;

-- Insert new user
INSERT INTO "Users" (
  "Id", "Email", "PasswordHash", "FullName", "Role",
  "IsEmailVerified", "EmailVerificationOtpHash", "EmailVerificationOtpExpiresAt",
  "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @email, @passwordHash, @fullName, ''Student'',
  false, @otpHash, @otpExpires,
  true, @now, @now
);
```

**Response:**
```json
{
  "message": "Registration successful. Please check your email for OTP."
}
```

**Frontend Action:**
```
5. Hiển thị success message
6. Redirect to Verify Email screen
7. Pre-fill email field
```

#### Action 2: Đăng ký bằng Google
```
1. User click "Sign up with Google" button
2. Frontend gọi Google OAuth flow
3. User chọn Google account
4. Google redirect về với idToken
5. Frontend gọi API:
```

**API Call:**
```http
POST /api/auth/google
Content-Type: application/json

{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjE..."
}
```

**Backend Process:**
```csharp
// AuthController.LoginWithGoogle()
1. Verify Google idToken (GoogleJsonWebSignature.ValidateAsync)
2. Extract payload:
   - Email
   - Name
   - GoogleSubject (sub)
   - Picture (avatar)
3. Check user exists by GoogleSubject or Email
4. If exists:
   - Update user info
   - Generate JWT token
5. If not exists:
   - Create new User:
     - Id = Guid.NewGuid()
     - Email = payload.Email
     - FullName = payload.Name
     - GoogleSubject = payload.Subject
     - AvatarUrl = payload.Picture
     - Role = "Student"
     - IsEmailVerified = true (Google đã verify)
     - IsActive = true
     - CreatedAt = now
     - UpdatedAt = now
   - Save to database
   - Generate JWT token
6. Return token + user info
```

**Database Operations:**
```sql
-- Check user exists
SELECT * FROM "Users" 
WHERE "GoogleSubject" = @googleSubject OR "Email" = @email;

-- Insert new user (if not exists)
INSERT INTO "Users" (
  "Id", "Email", "FullName", "GoogleSubject", "AvatarUrl",
  "Role", "IsEmailVerified", "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @email, @fullName, @googleSubject, @avatarUrl,
  ''Student'', true, true, @now, @now
);

-- Update existing user (if exists)
UPDATE "Users" SET
  "GoogleSubject" = @googleSubject,
  "AvatarUrl" = @avatarUrl,
  "IsEmailVerified" = true,
  "UpdatedAt" = @now
WHERE "Id" = @userId;
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "student@gmail.com",
    "fullName": "Nguyen Van A",
    "avatarUrl": "https://lh3.googleusercontent.com/...",
    "role": "Student",
    "isEmailVerified": true
  }
}
```

**Frontend Action:**
```
6. Lưu token vào localStorage/sessionStorage
7. Lưu user info vào state
8. Redirect to Dashboard
```

### Database Tables Involved
- **Users**: Lưu thông tin user mới

### Error Handling
- Email đã tồn tại → 409 Conflict
- Invalid email format → 400 Bad Request
- Password quá ngắn → 400 Bad Request
- Google token invalid → 401 Unauthorized
- Email service down → 500 Internal Server Error (nhưng user vẫn được tạo)

---

## 1.2 VERIFY EMAIL SCREEN

### Mô tả
Màn hình xác thực email bằng OTP code

### UI Components
- Email display (read-only, hiển thị email đã đăng ký)
- OTP input (6 digits)
- "Verify" button
- "Resend OTP" link
- "Back to Login" link

### User Actions & Flow

#### Action 1: Nhập OTP và Verify
```
1. User nhận OTP qua email
2. User nhập 6 số OTP
3. Click "Verify" button
4. Frontend gọi API:
```

**API Call:**
```http
POST /api/auth/verify-email
Content-Type: application/json

{
  "email": "student@example.com",
  "otp": "123456"
}
```

**Backend Process:**
```csharp
// PasswordAuthService.VerifyEmailOtpAsync()
1. Find user by email
2. Check user exists
3. Check OTP chưa expire (EmailVerificationOtpExpiresAt > now)
4. Verify OTP hash (PasswordHasher.VerifyHashedPassword)
5. If valid:
   - Set IsEmailVerified = true
   - Set EmailVerifiedAt = now
   - Clear OTP fields:
     - EmailVerificationOtpHash = null
     - EmailVerificationOtpExpiresAt = null
   - UpdatedAt = now
   - Save to database
   - Generate JWT token
   - Return token + user info
6. If invalid:
   - Throw UnauthorizedAccessException
```

**Database Operations:**
```sql
-- Find user
SELECT * FROM "Users" WHERE "Email" = @email;

-- Update user after verification
UPDATE "Users" SET
  "IsEmailVerified" = true,
  "EmailVerifiedAt" = @now,
  "EmailVerificationOtpHash" = NULL,
  "EmailVerificationOtpExpiresAt" = NULL,
  "UpdatedAt" = @now
WHERE "Id" = @userId;
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "student@example.com",
    "fullName": "Nguyen Van A",
    "role": "Student",
    "isEmailVerified": true
  }
}
```

**Frontend Action:**
```
5. Lưu token vào localStorage
6. Lưu user info vào state
7. Redirect to Dashboard hoặc Profile Setup
```

#### Action 2: Resend OTP
```
1. User click "Resend OTP" link
2. Frontend gọi API:
```

**API Call:**
```http
POST /api/auth/resend-otp
Content-Type: application/json

{
  "email": "student@example.com"
}
```

**Backend Process:**
```csharp
// (Cần implement)
1. Find user by email
2. Check user chưa verified
3. Generate new OTP
4. Hash OTP
5. Update user:
   - EmailVerificationOtpHash = newHashedOtp
   - EmailVerificationOtpExpiresAt = now + 10 minutes
   - UpdatedAt = now
6. Send OTP email
7. Return success message
```

**Database Operations:**
```sql
-- Update OTP
UPDATE "Users" SET
  "EmailVerificationOtpHash" = @newOtpHash,
  "EmailVerificationOtpExpiresAt" = @newExpires,
  "UpdatedAt" = @now
WHERE "Email" = @email AND "IsEmailVerified" = false;
```

### Database Tables Involved
- **Users**: Update IsEmailVerified, clear OTP fields

### Error Handling
- OTP incorrect → 401 Unauthorized
- OTP expired → 401 Unauthorized
- User not found → 404 Not Found
- User already verified → 400 Bad Request

---

## 1.3 LOGIN SCREEN

### Mô tả
Màn hình đăng nhập cho user đã có tài khoản

### UI Components
- Email input
- Password input
- "Login" button
- "Forgot Password" link
- "Sign in with Google" button
- "Register" link

### User Actions & Flow

#### Action 1: Đăng nhập bằng Email/Password
```
1. User nhập email và password
2. Click "Login" button
3. Frontend validate input
4. Frontend gọi API:
```

**API Call:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "student@example.com",
  "password": "password123"
}
```

**Backend Process:**
```csharp
// PasswordAuthService.LoginAsync()
1. Find user by email
2. Check user exists
3. Check user IsActive = true
4. Verify password (PasswordHasher.VerifyHashedPassword)
5. Check IsEmailVerified = true
6. If all valid:
   - Generate JWT token with claims:
     - NameIdentifier = userId
     - Email = email
     - Name = fullName
     - Role = role
   - Return token + user info
7. If invalid:
   - Throw UnauthorizedAccessException
```

**Database Operations:**
```sql
-- Find user
SELECT * FROM "Users" 
WHERE "Email" = @email AND "IsActive" = true;
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "student@example.com",
    "fullName": "Nguyen Van A",
    "avatarUrl": null,
    "role": "Student",
    "isEmailVerified": true
  }
}
```

**Frontend Action:**
```
5. Lưu token vào localStorage
6. Lưu user info vào state
7. Redirect based on role:
   - Student → Dashboard
   - Admin → Admin Dashboard
   - AcademicCounselor → Counselor Dashboard
   - IndustryMentor → Mentor Dashboard
```

#### Action 2: Đăng nhập bằng Google
(Giống như Register với Google, xem section 1.1)

### Database Tables Involved
- **Users**: Query để authenticate

### Error Handling
- Email không tồn tại → 401 Unauthorized
- Password sai → 401 Unauthorized
- Email chưa verified → 401 Unauthorized ("Please verify your email first")
- Account bị vô hiệu hóa → 401 Unauthorized ("Account is disabled")

---

# 2. STUDENT SCREENS

## 2.1 STUDENT DASHBOARD

### Mô tả
Màn hình tổng quan sau khi student đăng nhập

### UI Components
- Welcome message: "Welcome back, {fullName}!"
- Quick Stats Cards:
  - Target Role card
  - Match Score card (với progress bar)
  - Roadmap Progress card
  - GitHub Repos card
- Quick Actions:
  - "Update Profile" button
  - "Analyze Skill Gap" button
  - "View Roadmap" button
  - "Chat with AI Mentor" button
- Recent Activity section
- Recommendations section

### User Actions & Flow

#### Action 1: Load Dashboard Data
```
1. User đăng nhập thành công
2. Frontend redirect to Dashboard
3. Frontend gọi multiple APIs để load data:
```

**API Calls:**
```http
# 1. Get Student Profile
GET /api/profile/me
Authorization: Bearer {token}

# 2. Get Latest Skill Gap Report
GET /api/skill-gap/latest
Authorization: Bearer {token}

# 3. Get Latest Roadmap
GET /api/roadmap?limit=1
Authorization: Bearer {token}

# 4. Get GitHub Stats
GET /api/github/repositories?limit=5
Authorization: Bearer {token}
```

**Backend Process (Profile):**
```csharp
// StudentProfileController.GetMyProfile() - CẦN IMPLEMENT
1. Get userId from JWT claims
2. Query StudentProfiles table:
   - Include TargetRole
3. If not exists:
   - Return 404
4. Return profile data
```

**Database Operations:**
```sql
-- Get student profile
SELECT 
  sp.*,
  cr."Name" as "TargetRoleName",
  cr."Description" as "TargetRoleDescription"
FROM "StudentProfiles" sp
LEFT JOIN "CareerRoles" cr ON sp."TargetRoleId" = cr."Id"
WHERE sp."UserId" = @userId;

-- Get latest skill gap report
SELECT * FROM "SkillGapReports"
WHERE "UserId" = @userId
ORDER BY "CreatedAt" DESC
LIMIT 1;

-- Get latest roadmap
SELECT * FROM "Roadmaps"
WHERE "UserId" = @userId
ORDER BY "CreatedAt" DESC
LIMIT 1;

-- Get GitHub repos count
SELECT COUNT(*) FROM "GithubRepositories"
WHERE "UserId" = @userId;
```

**Response (Profile):**
```json
{
  "id": "profile-guid",
  "userId": "user-guid",
  "school": "FPT University",
  "major": "Software Engineering",
  "year": 3,
  "gpa": 3.5,
  "targetRoleId": "role-guid",
  "targetRoleName": "Backend Developer",
  "careerGoal": "Become a senior backend developer",
  "preferredLearningHoursPerWeek": 20
}
```

**Frontend Display:**
```
4. Hiển thị stats cards:
   - Target Role: "Backend Developer" (hoặc "Not set" nếu null)
   - Match Score: "75%" với progress bar màu xanh/vàng/đỏ
   - Roadmap Progress: "12/20 steps completed (60%)"
   - GitHub Repos: "5 repositories analyzed"
5. Hiển thị quick actions buttons
6. Load recent activity (mentor sessions, roadmap updates)
```

#### Action 2: Click "Update Profile"
```
1. User click "Update Profile" button
2. Frontend navigate to Profile Screen
```

#### Action 3: Click "Analyze Skill Gap"
```
1. User click "Analyze Skill Gap" button
2. Frontend navigate to Skill Gap Analysis Screen
```

#### Action 4: Click "View Roadmap"
```
1. User click "View Roadmap" button
2. Frontend navigate to Roadmap Screen
```

#### Action 5: Click "Chat with AI Mentor"
```
1. User click "Chat with AI Mentor" button
2. Frontend navigate to AI Mentor Chat Screen
```

### Database Tables Involved
- **StudentProfiles**: Load profile data
- **SkillGapReports**: Load latest report
- **Roadmaps**: Load latest roadmap
- **GithubRepositories**: Count repos
- **MentorSessions**: Load recent sessions

---

## 2.2 STUDENT PROFILE SCREEN

### Mô tả
Màn hình quản lý thông tin cá nhân của sinh viên

### UI Components
- Avatar upload section
- Form fields:
  - School (text input)
  - Major (text input)
  - Year (number input, 1-5)
  - GPA (number input, 0.0-4.0)
  - Target Career Role (dropdown select)
  - Career Goal (textarea)
  - Preferred Learning Hours/Week (number input)
- "Save" button
- "Cancel" button

### User Actions & Flow

#### Action 1: Load Profile Data
```
1. User navigate to Profile Screen
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/profile/me
Authorization: Bearer {token}
```

**Backend Process:**
```csharp
// StudentProfileController.GetMyProfile() - CẦN IMPLEMENT
1. Get userId from JWT
2. Query StudentProfiles:
   SELECT * FROM StudentProfiles 
   WHERE UserId = @userId
   INCLUDE TargetRole
3. If not found:
   - Return 404 hoặc empty profile
4. Return profile data
```

**Response:**
```json
{
  "id": "profile-guid",
  "userId": "user-guid",
  "school": "FPT University",
  "major": "Software Engineering",
  "year": 3,
  "gpa": 3.5,
  "targetRoleId": "role-guid",
  "targetRole": {
    "id": "role-guid",
    "name": "Backend Developer",
    "description": "..."
  },
  "careerGoal": "Become a senior backend developer in 3 years",
  "preferredLearningHoursPerWeek": 20,
  "createdAt": "2026-01-15T10:00:00Z",
  "updatedAt": "2026-05-16T10:00:00Z"
}
```

**Frontend Action:**
```
3. Pre-fill form với data từ API
4. Load danh sách Career Roles cho dropdown:
```

**API Call:**
```http
GET /api/career-roles
```

**Response:**
```json
[
  {
    "id": "role-1-guid",
    "name": "Backend Developer",
    "description": "...",
    "level": "Junior",
    "isActive": true
  },
  {
    "id": "role-2-guid",
    "name": "Frontend Developer",
    "description": "...",
    "level": "Junior",
    "isActive": true
  }
]
```

#### Action 2: Update Profile
```
1. User chỉnh sửa các fields
2. Click "Save" button
3. Frontend validate:
   - Year: 1-5
   - GPA: 0.0-4.0
   - PreferredLearningHoursPerWeek: > 0
4. Frontend gọi API:
```

**API Call:**
```http
PUT /api/profile/me
Authorization: Bearer {token}
Content-Type: application/json

{
  "school": "FPT University",
  "major": "Software Engineering",
  "year": 3,
  "gpa": 3.5,
  "targetRoleId": "role-guid",
  "careerGoal": "Become a senior backend developer in 3 years",
  "preferredLearningHoursPerWeek": 20
}
```

**Backend Process:**
```csharp
// StudentProfileController.UpdateMyProfile() - CẦN IMPLEMENT
1. Get userId from JWT
2. Validate input:
   - Year: 1-5
   - GPA: 0.0-4.0
   - PreferredLearningHoursPerWeek: > 0
   - TargetRoleId exists and active
3. Find existing profile:
   SELECT * FROM StudentProfiles WHERE UserId = @userId
4. If not exists:
   - Create new profile:
     - Id = Guid.NewGuid()
     - UserId = userId
     - CreatedAt = now
5. Update fields:
   - School = request.School
   - Major = request.Major
   - Year = request.Year
   - Gpa = request.Gpa
   - TargetRoleId = request.TargetRoleId
   - CareerGoal = request.CareerGoal
   - PreferredLearningHoursPerWeek = request.PreferredLearningHoursPerWeek
   - UpdatedAt = now
6. Save to database
7. Return updated profile
```

**Database Operations:**
```sql
-- Check if profile exists
SELECT * FROM "StudentProfiles" WHERE "UserId" = @userId;

-- Insert new profile (if not exists)
INSERT INTO "StudentProfiles" (
  "Id", "UserId", "School", "Major", "Year", "Gpa",
  "TargetRoleId", "CareerGoal", "PreferredLearningHoursPerWeek",
  "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @userId, @school, @major, @year, @gpa,
  @targetRoleId, @careerGoal, @preferredHours,
  @now, @now
);

-- Update existing profile
UPDATE "StudentProfiles" SET
  "School" = @school,
  "Major" = @major,
  "Year" = @year,
  "Gpa" = @gpa,
  "TargetRoleId" = @targetRoleId,
  "CareerGoal" = @careerGoal,
  "PreferredLearningHoursPerWeek" = @preferredHours,
  "UpdatedAt" = @now
WHERE "UserId" = @userId;
```

**Response:**
```json
{
  "id": "profile-guid",
  "userId": "user-guid",
  "school": "FPT University",
  "major": "Software Engineering",
  "year": 3,
  "gpa": 3.5,
  "targetRoleId": "role-guid",
  "careerGoal": "Become a senior backend developer in 3 years",
  "preferredLearningHoursPerWeek": 20,
  "updatedAt": "2026-05-16T13:59:28Z"
}
```

**Frontend Action:**
```
5. Hiển thị success message: "Profile updated successfully"
6. Update local state
7. Optional: Navigate back to Dashboard
```

#### Action 3: Upload Avatar
```
1. User click avatar upload area
2. User chọn image file
3. Frontend validate:
   - File type: image/jpeg, image/png, image/webp
   - File size: < 5MB
4. Frontend gọi API:
```

**API Call:**
```http
POST /api/storage/avatar
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [binary data]
```

**Backend Process:**
```csharp
// StorageController.UploadAvatar()
1. Get userId from JWT
2. Validate file:
   - Content type: image/*
   - Size: < 5MB
3. Generate unique filename:
   - avatars/{userId}/{guid}.{extension}
4. Upload to Google Cloud Storage
5. Get public URL
6. Update Users table:
   UPDATE Users SET AvatarUrl = @url WHERE Id = @userId
7. Return file info
```

**Database Operations:**
```sql
-- Update user avatar
UPDATE "Users" SET
  "AvatarUrl" = @avatarUrl,
  "UpdatedAt" = @now
WHERE "Id" = @userId;
```

**Response:**
```json
{
  "fileName": "avatar.jpg",
  "url": "https://storage.googleapis.com/bucket/avatars/user-guid/file-guid.jpg",
  "contentType": "image/jpeg",
  "size": 245678
}
```

**Frontend Action:**
```
5. Update avatar preview
6. Update user state với new avatarUrl
```

### Database Tables Involved
- **StudentProfiles**: CRUD profile data
- **CareerRoles**: Load dropdown options
- **Users**: Update avatar URL

### Error Handling
- Invalid GPA range → 400 Bad Request
- Invalid year → 400 Bad Request
- Target role not found → 404 Not Found
- File too large → 400 Bad Request
- Invalid file type → 400 Bad Request

---


## 2.3 SKILLS MANAGEMENT SCREEN

### Mô tả
Màn hình quản lý kỹ năng cá nhân của sinh viên

### UI Components
- **Tab 1: My Skills**
  - Danh sách skills đã khai báo (table/cards)
  - Mỗi skill hiển thị:
    - Skill name
    - Category badge
    - Level badge (Beginner/Intermediate/Advanced/Expert)
    - Verified status (icon/badge)
    - Evidence link (nếu có)
    - Edit button
    - Delete button
  - "Add Skill" button
  - Filter by category
  - Sort by level/name

- **Tab 2: Available Skills**
  - Search box
  - Category filter
  - Danh sách tất cả skills trong hệ thống
  - "Add to My Skills" button cho mỗi skill

### User Actions & Flow

#### Action 1: Load My Skills
```
1. User navigate to Skills Management Screen
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/user-skills
Authorization: Bearer {token}
```

**Backend Process:**
```csharp
// UserSkillsController.GetMySkills() - CẦN IMPLEMENT
1. Get userId from JWT
2. Query UserSkills:
   SELECT us.*, s.Name, s.Category, s.Description
   FROM UserSkills us
   INNER JOIN Skills s ON us.SkillId = s.Id
   WHERE us.UserId = @userId
   ORDER BY s.Category, s.Name
3. Return list of user skills
```

**Database Operations:**
```sql
SELECT 
  us."Id",
  us."UserId",
  us."SkillId",
  us."Level",
  us."IsVerified",
  us."Evidence",
  us."CreatedAt",
  us."UpdatedAt",
  s."Name" as "SkillName",
  s."Category" as "SkillCategory",
  s."Description" as "SkillDescription"
FROM "UserSkills" us
INNER JOIN "Skills" s ON us."SkillId" = s."Id"
WHERE us."UserId" = @userId
ORDER BY s."Category", s."Name";
```

**Response:**
```json
[
  {
    "id": "user-skill-1-guid",
    "userId": "user-guid",
    "skillId": "skill-1-guid",
    "skillName": "Java",
    "skillCategory": "Programming Language",
    "level": "Intermediate",
    "isVerified": true,
    "evidence": "https://storage.googleapis.com/bucket/evidence/cert.pdf",
    "createdAt": "2026-01-15T10:00:00Z",
    "updatedAt": "2026-03-20T10:00:00Z"
  },
  {
    "id": "user-skill-2-guid",
    "userId": "user-guid",
    "skillId": "skill-2-guid",
    "skillName": "Spring Boot",
    "skillCategory": "Framework",
    "level": "Beginner",
    "isVerified": false,
    "evidence": null,
    "createdAt": "2026-02-10T10:00:00Z",
    "updatedAt": "2026-02-10T10:00:00Z"
  }
]
```

**Frontend Display:**
```
3. Hiển thị table/cards với skills
4. Group by category (optional)
5. Hiển thị badges cho level và verified status
```

#### Action 2: Add New Skill
```
1. User click "Add Skill" button
2. Frontend hiển thị modal/dialog:
   - Skill dropdown (load từ /api/skills)
   - Level dropdown (Beginner/Intermediate/Advanced/Expert)
   - Verified checkbox
   - Evidence file upload (optional)
3. User chọn skill, level, và upload evidence (nếu có)
4. Click "Add" button
5. Frontend gọi API:
```

**API Call:**
```http
POST /api/user-skills
Authorization: Bearer {token}
Content-Type: application/json

{
  "skillId": "skill-guid",
  "level": "Intermediate",
  "isVerified": true,
  "evidence": "https://storage.googleapis.com/bucket/evidence/cert.pdf"
}
```

**Backend Process:**
```csharp
// UserSkillsController.AddSkill() - CẦN IMPLEMENT
1. Get userId from JWT
2. Validate input:
   - SkillId exists and active
   - Level is valid enum
3. Check duplicate:
   SELECT COUNT(*) FROM UserSkills 
   WHERE UserId = @userId AND SkillId = @skillId
4. If duplicate:
   - Return 409 Conflict
5. Create UserSkill:
   - Id = Guid.NewGuid()
   - UserId = userId
   - SkillId = request.SkillId
   - Level = request.Level
   - IsVerified = request.IsVerified
   - Evidence = request.Evidence
   - CreatedAt = now
   - UpdatedAt = now
6. Save to database
7. Return created user skill
```

**Database Operations:**
```sql
-- Check skill exists
SELECT COUNT(*) FROM "Skills" 
WHERE "Id" = @skillId AND "IsActive" = true;

-- Check duplicate
SELECT COUNT(*) FROM "UserSkills" 
WHERE "UserId" = @userId AND "SkillId" = @skillId;

-- Insert new user skill
INSERT INTO "UserSkills" (
  "Id", "UserId", "SkillId", "Level", "IsVerified", "Evidence",
  "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @userId, @skillId, @level, @isVerified, @evidence,
  @now, @now
);
```

**Response:**
```json
{
  "id": "user-skill-guid",
  "userId": "user-guid",
  "skillId": "skill-guid",
  "skillName": "Docker",
  "skillCategory": "DevOps",
  "level": "Intermediate",
  "isVerified": true,
  "evidence": "https://storage.googleapis.com/bucket/evidence/cert.pdf",
  "createdAt": "2026-05-16T14:00:00Z",
  "updatedAt": "2026-05-16T14:00:00Z"
}
```

**Frontend Action:**
```
6. Close modal
7. Refresh skills list
8. Hiển thị success message: "Skill added successfully"
```

#### Action 3: Update Skill Level
```
1. User click "Edit" button trên một skill
2. Frontend hiển thị edit modal:
   - Skill name (read-only)
   - Level dropdown (pre-filled)
   - Verified checkbox (pre-filled)
   - Evidence upload
3. User thay đổi level hoặc verified status
4. Click "Save" button
5. Frontend gọi API:
```

**API Call:**
```http
PUT /api/user-skills/{userSkillId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "level": "Advanced",
  "isVerified": true,
  "evidence": "https://storage.googleapis.com/bucket/evidence/new-cert.pdf"
}
```

**Backend Process:**
```csharp
// UserSkillsController.UpdateSkill() - CẦN IMPLEMENT
1. Get userId from JWT
2. Find UserSkill:
   SELECT * FROM UserSkills 
   WHERE Id = @userSkillId AND UserId = @userId
3. If not found or not owned:
   - Return 404 Not Found
4. Validate level
5. Update fields:
   - Level = request.Level
   - IsVerified = request.IsVerified
   - Evidence = request.Evidence (if provided)
   - UpdatedAt = now
6. Save to database
7. Return updated user skill
```

**Database Operations:**
```sql
-- Update user skill
UPDATE "UserSkills" SET
  "Level" = @level,
  "IsVerified" = @isVerified,
  "Evidence" = @evidence,
  "UpdatedAt" = @now
WHERE "Id" = @userSkillId AND "UserId" = @userId;
```

**Response:**
```json
{
  "id": "user-skill-guid",
  "userId": "user-guid",
  "skillId": "skill-guid",
  "skillName": "Docker",
  "skillCategory": "DevOps",
  "level": "Advanced",
  "isVerified": true,
  "evidence": "https://storage.googleapis.com/bucket/evidence/new-cert.pdf",
  "updatedAt": "2026-05-16T14:05:00Z"
}
```

#### Action 4: Delete Skill
```
1. User click "Delete" button
2. Frontend hiển thị confirmation dialog
3. User confirm
4. Frontend gọi API:
```

**API Call:**
```http
DELETE /api/user-skills/{userSkillId}
Authorization: Bearer {token}
```

**Backend Process:**
```csharp
// UserSkillsController.DeleteSkill() - CẦN IMPLEMENT
1. Get userId from JWT
2. Find UserSkill:
   SELECT * FROM UserSkills 
   WHERE Id = @userSkillId AND UserId = @userId
3. If not found or not owned:
   - Return 404 Not Found
4. Delete from database
5. Return 204 No Content
```

**Database Operations:**
```sql
-- Delete user skill
DELETE FROM "UserSkills" 
WHERE "Id" = @userSkillId AND "UserId" = @userId;
```

**Frontend Action:**
```
5. Remove skill from list
6. Hiển thị success message: "Skill removed successfully"
```

#### Action 5: Upload Evidence
```
1. User click "Upload Evidence" button
2. User chọn file (PDF, image, etc.)
3. Frontend gọi API:
```

**API Call:**
```http
POST /api/storage/user-skills/{userSkillId}/evidence
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [binary data]
```

**Backend Process:**
```csharp
// StorageController.UploadSkillEvidence()
1. Get userId from JWT
2. Verify UserSkill belongs to user
3. Validate file (size, type)
4. Upload to Google Cloud Storage:
   - Path: evidence/{userId}/{userSkillId}/{guid}.{ext}
5. Update UserSkills table:
   UPDATE UserSkills SET Evidence = @url WHERE Id = @userSkillId
6. Return file info
```

**Database Operations:**
```sql
-- Verify ownership
SELECT COUNT(*) FROM "UserSkills" 
WHERE "Id" = @userSkillId AND "UserId" = @userId;

-- Update evidence
UPDATE "UserSkills" SET
  "Evidence" = @evidenceUrl,
  "UpdatedAt" = @now
WHERE "Id" = @userSkillId;
```

### Database Tables Involved
- **UserSkills**: CRUD user skills
- **Skills**: Load available skills
- **Users**: Get userId from JWT

### Error Handling
- Skill already added → 409 Conflict
- Skill not found → 404 Not Found
- Invalid level → 400 Bad Request
- UserSkill not owned → 404 Not Found
- File too large → 400 Bad Request

---

## 2.4 SKILL GAP ANALYSIS SCREEN

### Mô tả
Màn hình phân tích khoảng cách kỹ năng so với vai trò mục tiêu

### UI Components
- **Header Section:**
  - Target role name (large text)
  - Match score (% với progress bar màu)
  - Summary text
  - "Re-analyze" button
  - "Generate Roadmap" button
  - "Chat with AI" button

- **Filters & Tabs:**
  - Tab: All / Matched / Weak / Missing / NotVerified
  - Sort by: Priority / Name

- **Skill Gap Items List:**
  - Mỗi item card hiển thị:
    - Skill name + category
    - Current level badge
    - Required level badge
    - Status badge (màu khác nhau)
    - Priority stars (1-5)
    - Recommendation text
    - "Add to My Skills" button (nếu Missing)
    - "Improve" button (nếu Weak)
    - "Verify" button (nếu NotVerified)

### User Actions & Flow

#### Action 1: Analyze Skill Gap
```
1. User navigate to Skill Gap Analysis Screen
2. Frontend check if user has target role:
   - GET /api/profile/me
3. If no target role:
   - Hiển thị message: "Please select a target career role first"
   - Button: "Select Career Role"
4. If has target role:
   - Frontend gọi API:
```

**API Call:**
```http
POST /api/skill-gap/analyze
Authorization: Bearer {token}
Content-Type: application/json

{
  "careerRoleId": null  // null = use target role from profile
}
```

**Backend Process:**
```csharp
// SkillGapsController.AnalyzeSkillGap()
1. Get userId from JWT
2. Determine target role:
   - If request.CareerRoleId provided: use it
   - Else: get from StudentProfile.TargetRoleId
3. If no target role:
   - Return 400 Bad Request
4. Get required skills for role:
   SELECT * FROM RoleSkillRequirements 
   WHERE CareerRoleId = @roleId
   INCLUDE Skill
5. Get user's current skills:
   SELECT * FROM UserSkills WHERE UserId = @userId
6. Compare each required skill:
   For each requirement:
     - Find matching user skill
     - Parse levels to numeric (Beginner=1, Intermediate=2, Advanced=3, Expert=4)
     - Determine status:
       * userLevel >= requiredLevel && isVerified → "Matched"
       * userLevel >= requiredLevel && !isVerified → "NotVerified"
       * userLevel > 0 && userLevel < requiredLevel → "Weak"
       * userLevel == 0 → "Missing"
     - Calculate score:
       * Matched: fullWeight
       * NotVerified: fullWeight * 0.5
       * Weak: fullWeight * (userLevel / requiredLevel)
       * Missing: 0
     - Generate recommendation text
7. Calculate match score:
   matchScore = (totalScore / totalWeight) * 100
8. Generate summary text based on score:
   - >= 80%: "Excellent! You are well-prepared for this role."
   - 60-79%: "Good progress! Focus on improving weak skills."
   - 40-59%: "You need to work on several key skills."
   - < 40%: "You have significant gaps. Start with high-priority skills."
9. Delete previous reports for same user + role
10. Create SkillGapReport:
    - Id = Guid.NewGuid()
    - UserId = userId
    - CareerRoleId = roleId
    - MatchScore = matchScore
    - Summary = summary
    - CreatedAt = now
    - UpdatedAt = now
11. Create SkillGapReportItems (one per required skill)
12. Save to database
13. Return report with items
```

**Database Operations:**
```sql
-- Get target role from profile
SELECT "TargetRoleId" FROM "StudentProfiles" 
WHERE "UserId" = @userId;

-- Get required skills for role
SELECT 
  rsr.*,
  s."Name" as "SkillName",
  s."Category" as "SkillCategory"
FROM "RoleSkillRequirements" rsr
INNER JOIN "Skills" s ON rsr."SkillId" = s."Id"
WHERE rsr."CareerRoleId" = @roleId;

-- Get user skills
SELECT * FROM "UserSkills" WHERE "UserId" = @userId;

-- Delete previous reports
DELETE FROM "SkillGapReportItems" 
WHERE "SkillGapReportId" IN (
  SELECT "Id" FROM "SkillGapReports" 
  WHERE "UserId" = @userId AND "CareerRoleId" = @roleId
);

DELETE FROM "SkillGapReports" 
WHERE "UserId" = @userId AND "CareerRoleId" = @roleId;

-- Insert new report
INSERT INTO "SkillGapReports" (
  "Id", "UserId", "CareerRoleId", "MatchScore", "Summary",
  "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @userId, @roleId, @matchScore, @summary,
  @now, @now
);

-- Insert report items
INSERT INTO "SkillGapReportItems" (
  "Id", "SkillGapReportId", "SkillId", "CurrentLevel", "RequiredLevel",
  "Status", "Priority", "Recommendation", "CreatedAt"
) VALUES (
  @id, @reportId, @skillId, @currentLevel, @requiredLevel,
  @status, @priority, @recommendation, @now
);
```

**Response:**
```json
{
  "id": "report-guid",
  "userId": "user-guid",
  "careerRoleId": "role-guid",
  "careerRoleName": "Backend Developer",
  "matchScore": 65.5,
  "summary": "Good progress! Focus on improving weak skills and verifying your knowledge.",
  "createdAt": "2026-05-16T14:10:00Z",
  "items": [
    {
      "skillId": "skill-1-guid",
      "skillName": "Java",
      "skillCategory": "Programming Language",
      "currentLevel": "Intermediate",
      "requiredLevel": "Advanced",
      "status": "Weak",
      "priority": 5,
      "recommendation": "You need to improve from Intermediate to Advanced. Focus on advanced Java concepts like concurrency, JVM internals, and design patterns."
    },
    {
      "skillId": "skill-2-guid",
      "skillName": "Spring Boot",
      "skillCategory": "Framework",
      "currentLevel": "Beginner",
      "requiredLevel": "Intermediate",
      "status": "Weak",
      "priority": 5,
      "recommendation": "You need to improve from Beginner to Intermediate. Build more projects with Spring Boot."
    },
    {
      "skillId": "skill-3-guid",
      "skillName": "Docker",
      "skillCategory": "DevOps",
      "currentLevel": "None",
      "requiredLevel": "Intermediate",
      "status": "Missing",
      "priority": 4,
      "recommendation": "You need to learn this skill up to Intermediate level. Start with Docker basics and containerization concepts."
    },
    {
      "skillId": "skill-4-guid",
      "skillName": "PostgreSQL",
      "skillCategory": "Database",
      "currentLevel": "Intermediate",
      "requiredLevel": "Intermediate",
      "status": "NotVerified",
      "priority": 4,
      "recommendation": "You have the required skill level, but it needs to be verified. Provide evidence such as certificates or projects."
    },
    {
      "skillId": "skill-5-guid",
      "skillName": "Git",
      "skillCategory": "Version Control",
      "currentLevel": "Advanced",
      "requiredLevel": "Intermediate",
      "status": "Matched",
      "priority": 3,
      "recommendation": "Great job! You have mastered and verified this skill."
    }
  ]
}
```

**Frontend Display:**
```
5. Hiển thị header với match score:
   - Progress bar màu:
     * >= 80%: green
     * 60-79%: yellow
     * 40-59%: orange
     * < 40%: red
6. Hiển thị summary text
7. Hiển thị skill gap items:
   - Group by status (tabs)
   - Sort by priority (default)
   - Status badges màu:
     * Matched: green
     * NotVerified: blue
     * Weak: yellow
     * Missing: red
8. Enable action buttons
```

#### Action 2: Filter by Status
```
1. User click tab (All / Matched / Weak / Missing / NotVerified)
2. Frontend filter items locally
3. Update displayed list
```

#### Action 3: Generate Roadmap from Gap
```
1. User click "Generate Roadmap" button
2. Frontend gọi API:
```

**API Call:**
```http
POST /api/roadmap/generate
Authorization: Bearer {token}
Content-Type: application/json

{
  "careerRoleId": "role-guid",
  "skillGapReportId": "report-guid",
  "title": "Backend Developer Learning Path",
  "description": "Roadmap generated from skill gap analysis"
}
```

**Frontend Action:**
```
3. Navigate to Roadmap Screen
4. Hiển thị newly generated roadmap
```

#### Action 4: Chat with AI about Gap
```
1. User click "Chat with AI" button
2. Frontend navigate to AI Mentor Chat
3. Pre-fill context với skill gap report
4. Suggest questions:
   - "How can I improve my match score?"
   - "Which skills should I focus on first?"
   - "What projects can help me learn these skills?"
```

#### Action 5: Add Missing Skill
```
1. User click "Add to My Skills" button trên Missing skill
2. Frontend hiển thị add skill modal (pre-filled với skill)
3. User chọn level và add
4. Frontend gọi POST /api/user-skills
5. Refresh gap analysis (optional)
```

### Database Tables Involved
- **SkillGapReports**: Store analysis results
- **SkillGapReportItems**: Store individual skill gaps
- **RoleSkillRequirements**: Get required skills
- **UserSkills**: Get current skills
- **StudentProfiles**: Get target role
- **Skills**: Get skill details

### Error Handling
- No target role → 400 Bad Request
- Career role not found → 404 Not Found
- No skill requirements for role → 400 Bad Request
- User has no skills → Still generate report (all Missing)

---


## 2.5 ROADMAP SCREEN

### Mô tả
Màn hình lộ trình học tập cá nhân hóa dựa trên skill gap

### UI Components
- **Roadmap Header:**
  - Title (editable)
  - Career role badge
  - Progress bar (% completed)
  - Status badge (Active/Completed/Archived)
  - "Regenerate" button
  - "Export" button

- **Timeline View:**
  - Vertical timeline với nodes
  - Mỗi node hiển thị:
    - Order index (1, 2, 3...)
    - Title
    - Description (expandable)
    - Node type badge (Skill/Project/Reading/Practice/Assessment)
    - Status (NotStarted/InProgress/Completed)
    - Estimated hours
    - Priority stars
    - Prerequisite indicator (arrow từ node trước)
    - Action buttons:
      * "Start" (if NotStarted)
      * "Mark as Completed" (if InProgress)
      * "View Resources" (if has learning resources)

- **Sidebar:**
  - Filter by status
  - Filter by node type
  - Sort options
  - Statistics:
    * Total nodes
    * Completed nodes
    * Estimated total hours
    * Hours completed

### User Actions & Flow

#### Action 1: Load Roadmap
```
1. User navigate to Roadmap Screen
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/roadmap
Authorization: Bearer {token}
```

**Backend Process:**
```csharp
// RoadmapController.GetMine()
1. Get userId from JWT
2. Query Roadmaps:
   SELECT * FROM Roadmaps 
   WHERE UserId = @userId
   ORDER BY CreatedAt DESC
3. For each roadmap, get nodes:
   SELECT * FROM RoadmapNodes 
   WHERE RoadmapId = @roadmapId
   ORDER BY OrderIndex
4. Return list of roadmaps with nodes
```

**Database Operations:**
```sql
-- Get user roadmaps
SELECT 
  r.*,
  cr."Name" as "CareerRoleName"
FROM "Roadmaps" r
INNER JOIN "CareerRoles" cr ON r."CareerRoleId" = cr."Id"
WHERE r."UserId" = @userId
ORDER BY r."CreatedAt" DESC;

-- Get roadmap nodes
SELECT 
  rn.*,
  s."Name" as "SkillName",
  lr."Title" as "ResourceTitle",
  lr."Url" as "ResourceUrl",
  lr."StorageObjectName",
  lr."ContentType",
  lr."FileSize"
FROM "RoadmapNodes" rn
LEFT JOIN "Skills" s ON rn."SkillId" = s."Id"
LEFT JOIN "LearningResources" lr ON rn."LearningResourceId" = lr."Id"
WHERE rn."RoadmapId" = @roadmapId
ORDER BY rn."OrderIndex";
```

**Response:**
```json
[
  {
    "id": "roadmap-guid",
    "careerRoleId": "role-guid",
    "careerRoleName": "Backend Developer",
    "skillGapReportId": "report-guid",
    "title": "Backend Developer Learning Path",
    "description": "Personalized roadmap based on skill gap analysis",
    "status": "Active",
    "progress": 35.5,
    "createdAt": "2026-05-16T10:00:00Z",
    "updatedAt": "2026-05-16T14:00:00Z",
    "nodes": [
      {
        "id": "node-1-guid",
        "skillId": "skill-1-guid",
        "skillName": "Java Advanced",
        "learningResourceId": "resource-1-guid",
        "prerequisiteNodeId": null,
        "title": "Master Java Advanced Concepts",
        "description": "Learn concurrency, JVM internals, and design patterns",
        "nodeType": "Skill",
        "status": "Completed",
        "orderIndex": 1,
        "estimatedHours": 40,
        "priority": 5,
        "learningResource": {
          "id": "resource-1-guid",
          "skillId": "skill-1-guid",
          "skillName": "Java Advanced",
          "title": "Java Concurrency in Practice",
          "url": "/api/storage/learning-resources/resource-1-guid/download",
          "sourceType": "File",
          "contentType": "application/pdf",
          "fileSize": 2485120,
          "resourceType": "Book",
          "difficulty": "Intermediate",
          "estimatedHours": 8
        }
      },
      {
        "id": "node-2-guid",
        "skillId": "skill-2-guid",
        "skillName": "Spring Boot",
        "learningResourceId": "resource-2-guid",
        "prerequisiteNodeId": "node-1-guid",
        "title": "Build REST APIs with Spring Boot",
        "description": "Create production-ready REST APIs",
        "nodeType": "Skill",
        "status": "InProgress",
        "orderIndex": 2,
        "estimatedHours": 30,
        "priority": 5,
        "learningResource": {
          "id": "resource-2-guid",
          "skillId": "skill-2-guid",
          "skillName": "Spring Boot",
          "title": "Spring Boot Documentation",
          "url": "https://spring.io/projects/spring-boot",
          "sourceType": "Link",
          "contentType": null,
          "fileSize": null,
          "resourceType": "Documentation",
          "difficulty": "Intermediate",
          "estimatedHours": 6
        }
      },
      {
        "id": "node-3-guid",
        "skillId": null,
        "skillName": null,
        "learningResourceId": null,
        "prerequisiteNodeId": "node-2-guid",
        "title": "Build E-commerce Backend Project",
        "description": "Apply Spring Boot knowledge in a real project",
        "nodeType": "Project",
        "status": "NotStarted",
        "orderIndex": 3,
        "estimatedHours": 60,
        "priority": 4,
        "learningResource": null
      }
    ]
  }
]
```

**Frontend Display:**
```
3. Hiển thị roadmap header với progress
4. Render timeline với nodes
5. Nếu node có learningResource:
   - sourceType = Link: mở learningResource.url trong tab mới
   - sourceType = File: gọi learningResource.url để tải/xem file
6. Hiển thị prerequisite arrows
7. Calculate statistics
```

#### Action 2: Update Node Status
```
1. User click "Start" button trên NotStarted node
2. Frontend gọi API:
```

**API Call:**
```http
PUT /api/roadmap-node/{nodeId}/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "InProgress"
}
```

**Backend Process:**
```csharp
// RoadmapController.UpdateNodeStatus()
1. Get userId from JWT
2. Find RoadmapNode:
   SELECT rn.*, r.UserId 
   FROM RoadmapNodes rn
   INNER JOIN Roadmaps r ON rn.RoadmapId = r.Id
   WHERE rn.Id = @nodeId
3. Verify ownership (r.UserId == userId)
4. Validate status transition:
   - NotStarted -> InProgress: OK
   - InProgress -> Completed: OK
   - Completed -> InProgress: OK (allow undo)
   - Other transitions: validate
5. Update node:
   - Status = request.Status
   - UpdatedAt = now
6. Recalculate roadmap progress:
   completedCount = COUNT nodes WHERE Status = 'Completed'
   totalCount = COUNT all nodes
   progress = (completedCount / totalCount) * 100
7. Update Roadmap:
   - Progress = calculated progress
   - Status = 'Completed' if progress == 100, else 'Active'
   - UpdatedAt = now
8. Save to database
9. Return updated node
```

**Database Operations:**
```sql
-- Find node and verify ownership
SELECT rn.*, r."UserId"
FROM "RoadmapNodes" rn
INNER JOIN "Roadmaps" r ON rn."RoadmapId" = r."Id"
WHERE rn."Id" = @nodeId;

-- Update node status
UPDATE "RoadmapNodes" SET
  "Status" = @status,
  "UpdatedAt" = @now
WHERE "Id" = @nodeId;

-- Calculate progress
SELECT 
  COUNT(*) as "TotalNodes",
  SUM(CASE WHEN "Status" = 'Completed' THEN 1 ELSE 0 END) as "CompletedNodes"
FROM "RoadmapNodes"
WHERE "RoadmapId" = @roadmapId;

-- Update roadmap progress
UPDATE "Roadmaps" SET
  "Progress" = @progress,
  "Status" = @status,
  "UpdatedAt" = @now
WHERE "Id" = @roadmapId;
```

**Response:**
```json
{
  "id": "node-2-guid",
  "skillId": "skill-2-guid",
  "skillName": "Spring Boot",
  "learningResourceId": "resource-2-guid",
  "prerequisiteNodeId": "node-1-guid",
  "title": "Build REST APIs with Spring Boot",
  "description": "Create production-ready REST APIs",
  "nodeType": "Skill",
  "status": "Completed",
  "orderIndex": 2,
  "estimatedHours": 30,
  "priority": 5
}
```

**Frontend Action:**
```
3. Update node status in UI
4. Update progress bar
5. Show success animation
6. If completed, show confetti/celebration
```

#### Action 3: Generate New Roadmap
```
1. User click "Regenerate" button
2. Frontend show confirmation dialog
3. User confirm
4. Frontend gọi API:
```

**API Call:**
```http
POST /api/roadmap/generate
Authorization: Bearer {token}
Content-Type: application/json

{
  "careerRoleId": "role-guid",
  "skillGapReportId": "report-guid",
  "title": "Updated Backend Developer Path",
  "description": null
}
```

(Xem chi tiết ở section Roadmap Generation)

### Database Tables Involved
- **Roadmaps**: Store roadmap metadata
- **RoadmapNodes**: Store learning steps
- **Skills**: Link nodes to skills
- **LearningResources**: Link nodes to resources
- **SkillGapReports**: Source for roadmap generation

---

## 2.6 GITHUB INTEGRATION SCREEN

### Mô tả
Màn hình kết nối GitHub và phân tích repositories

### UI Components
- **Tab 1: Connect GitHub**
  - Connection status card
  - "Connect GitHub" button (if not connected)
  - Connected info (if connected):
    - GitHub username
    - Avatar
    - Connected date
    - "Disconnect" button

- **Tab 2: My Repositories**
  - "Sync Repositories" button
  - Last synced time
  - Repository cards grid:
    - Repo name
    - Description
    - Main language badge
    - Quality score (0-100) với progress bar
    - AI summary (expandable)
    - Mapped skills (tags)
    - "Analyze" button
    - "Add to Portfolio" button
    - "View on GitHub" link

### User Actions & Flow

#### Action 1: Connect GitHub OAuth
```
1. User click "Connect GitHub" button
2. Frontend gọi API:
```

**API Call:**
```http
POST /api/github/oauth/login
Authorization: Bearer {token}
Content-Type: application/json

{
  "returnUrl": "https://frontend.com/github",
  "scope": "repo read:user user:email"
}
```

**Backend Process:**
```csharp
// GithubController.CreateOAuthLogin()
1. Get userId from JWT
2. Generate random state (32 chars)
3. Create GithubOAuthState:
   - State = state
   - UserId = userId
   - ReturnUrl = request.ReturnUrl
   - ExpiresAt = now + 10 minutes
   - CreatedAt = now
4. Save to database
5. Clean up expired states
6. Build GitHub authorization URL:
   https://github.com/login/oauth/authorize?
     client_id={clientId}&
     redirect_uri={callbackUrl}&
     scope={scope}&
     state={state}
7. Return authorization URL
```

**Database Operations:**
```sql
-- Insert OAuth state
INSERT INTO "GithubOAuthStates" (
  "State", "UserId", "ReturnUrl", "ExpiresAt", "CreatedAt"
) VALUES (
  @state, @userId, @returnUrl, @expiresAt, @now
);

-- Delete expired states
DELETE FROM "GithubOAuthStates" 
WHERE "UserId" = @userId AND "ExpiresAt" < @now;
```

**Response:**
```json
{
  "authorizationUrl": "https://github.com/login/oauth/authorize?client_id=xxx&redirect_uri=xxx&scope=repo%20read:user&state=abc123",
  "state": "abc123",
  "returnUrl": "https://frontend.com/github"
}
```

**Frontend Action:**
```
3. Redirect user to authorizationUrl
4. User authorizes on GitHub
5. GitHub redirects to callback URL
```

#### Action 2: OAuth Callback (Backend handles)
```
GitHub redirects to: /api/github/oauth/callback?code=xxx&state=yyy
```

**Backend Process:**
```csharp
// GithubController.OAuthCallback()
1. Validate code and state parameters
2. Find GithubOAuthState by state
3. Check not expired
4. Exchange code for access token:
   POST https://github.com/login/oauth/access_token
   Body: { client_id, client_secret, code }
5. Get GitHub user profile:
   GET https://api.github.com/user
   Header: Authorization: Bearer {accessToken}
6. Find or create GithubConnection:
   - If exists: update
   - If not: create new
7. Save connection:
   - GithubUserId = githubUser.Id
   - GithubUsername = githubUser.Login
   - AccessToken = accessToken
   - TokenType = "bearer"
   - Scope = scope
   - ConnectedAt = now (if new)
   - UpdatedAt = now
8. Update StudentProfile.GithubUsername
9. Delete OAuth state
10. Redirect to returnUrl
```

**Database Operations:**
```sql
-- Find OAuth state
SELECT * FROM "GithubOAuthStates" WHERE "State" = @state;

-- Find existing connection
SELECT * FROM "GithubConnections" WHERE "UserId" = @userId;

-- Insert new connection
INSERT INTO "GithubConnections" (
  "Id", "UserId", "GithubUserId", "GithubUsername",
  "AccessToken", "TokenType", "Scope",
  "ConnectedAt", "UpdatedAt"
) VALUES (
  @id, @userId, @githubUserId, @githubUsername,
  @accessToken, @tokenType, @scope,
  @now, @now
);

-- Update existing connection
UPDATE "GithubConnections" SET
  "GithubUserId" = @githubUserId,
  "GithubUsername" = @githubUsername,
  "AccessToken" = @accessToken,
  "TokenType" = @tokenType,
  "Scope" = @scope,
  "UpdatedAt" = @now
WHERE "UserId" = @userId;

-- Update profile
UPDATE "StudentProfiles" SET
  "GithubUsername" = @githubUsername,
  "UpdatedAt" = @now
WHERE "UserId" = @userId;

-- Delete OAuth state
DELETE FROM "GithubOAuthStates" WHERE "State" = @state;
```

#### Action 3: Sync Repositories
```
1. User click "Sync Repositories" button
2. Frontend gọi API:
```

**API Call:**
```http
POST /api/github/sync
Authorization: Bearer {token}
```

**Backend Process:**
```csharp
// GithubController.Sync()
1. Get userId from JWT
2. Find GithubConnection
3. If not connected: return 400
4. Call GitHub API:
   GET https://api.github.com/user/repos?per_page=100
   Header: Authorization: Bearer {accessToken}
5. Filter public repos
6. For each repo:
   - Check if exists in database
   - If exists: update
   - If not: create new
   - Save:
     - RepoName = repo.Name
     - RepoUrl = repo.HtmlUrl
     - Description = repo.Description
     - MainLanguage = repo.Language
     - LastSyncedAt = now
7. Return list of synced repos
```

**Database Operations:**
```sql
-- Find connection
SELECT * FROM "github_connections" WHERE "UserId" = @userId;

-- Upsert repository
INSERT INTO "github_repositories" (
  "Id", "UserId", "RepoName", "RepoUrl",
  "Description", "MainLanguage", "QualityScore",
  "LastSyncedAt", "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @userId, @repoName, @repoUrl,
  @description, @mainLanguage, @qualityScore,
  @now, @now, @now
)
ON CONFLICT ("UserId", "RepoUrl") DO UPDATE SET
  "RepoName" = @repoName,
  "Description" = @description,
  "LastSyncedAt" = @now,
  "UpdatedAt" = @now;
```

**Response:**
```json
[
  {
    "id": "repo-1-guid",
    "repoName": "ecommerce-backend",
    "repoUrl": "https://github.com/user/ecommerce-backend",
    "description": "E-commerce REST API built with Spring Boot",
    "mainLanguage": "Java",
    "qualityScore": 75,
    "aiSummary": null,
    "techStackJson": null,
    "lastSyncedAt": "2026-05-16T14:15:00Z"
  }
]
```

#### Action 4: Analyze Repository
```
1. User click "Analyze" button trên một repo
2. Frontend gọi API:
```

**API Call:**
```http
POST /api/github/analyze-readme
Authorization: Bearer {token}
Content-Type: application/json

{
  "repositoryId": "repo-1-guid"
}
```

**Backend Process:**
```csharp
// GithubController.AnalyzeReadme()
1. Get userId from JWT
2. Find GithubRepository (verify ownership)
3. Get access token from GithubConnection
4. Fetch README from GitHub API:
   GET https://api.github.com/repos/{owner}/{repo}/readme
5. Decode README content (base64)
6. Extract tech stack from README:
   - Parse markdown
   - Look for technology keywords
   - Build tech stack JSON
7. Call AI API (Gemini) to summarize:
   Prompt: "Summarize this GitHub project in 2-3 sentences. 
            Focus on: what it does, technologies used, key features.
            README: {readmeContent}"
8. Calculate quality score:
   - Has README: +20
   - Has description: +10
   - README quality: +20
   - Tech stack clarity: +20
   - Has license: +10
   - Has topics: +10
   - Recent activity: +15
9. Update GithubRepository:
   - AiSummary = aiResponse
   - TechStackJson = techStackJson
   - QualityScore = calculatedScore
   - UpdatedAt = now
10. Auto-map to skills based on tech stack
11. Save to database
12. Return updated repo
```

**Database Operations:**
```sql
-- Update repository analysis
UPDATE "GithubRepositories" SET
  "AiSummary" = @aiSummary,
  "TechStackJson" = @techStackJson,
  "QualityScore" = @qualityScore,
  "UpdatedAt" = @now
WHERE "Id" = @repositoryId AND "UserId" = @userId;

-- Insert skill mappings
INSERT INTO "GithubRepositorySkills" (
  "Id", "GithubRepositoryId", "SkillId", "CreatedAt"
) VALUES (
  @id, @repositoryId, @skillId, @now
);
```

**Response:**
```json
{
  "id": "repo-1-guid",
  "repoName": "ecommerce-backend",
  "description": "E-commerce REST API built with Spring Boot",
  "mainLanguage": "Java",
  "stars": 15,
  "forks": 3,
  "qualityScore": 75,
  "aiSummary": "A full-featured e-commerce backend API built with Spring Boot and PostgreSQL. Implements JWT authentication, product management, order processing, and payment integration. Includes comprehensive unit tests and Docker deployment configuration.",
  "techStackJson": "{\"languages\":[\"Java\"],\"frameworks\":[\"Spring Boot\"],\"databases\":[\"PostgreSQL\"],\"tools\":[\"Docker\",\"Maven\"]}",
  "mappedSkills": [
    {"id": "skill-1-guid", "name": "Java"},
    {"id": "skill-2-guid", "name": "Spring Boot"},
    {"id": "skill-3-guid", "name": "PostgreSQL"},
    {"id": "skill-4-guid", "name": "Docker"}
  ]
}
```

### Database Tables Involved
- **GithubConnections**: Store OAuth connection
- **GithubOAuthStates**: Temporary OAuth state
- **GithubRepositories**: Store repo data
- **GithubRepositorySkills**: Map repos to skills
- **StudentProfiles**: Update GitHub username

---


## 2.7 PORTFOLIO BUILDER SCREEN

### Mô tả
Màn hình tạo và quản lý portfolio công khai

### UI Components
- **Tab 1: Edit Portfolio**
  - Form fields:
    - Title (text input)
    - Slug (text input, auto-generated from title)
    - Bio (textarea, markdown support)
    - Theme (dropdown: Default/Modern/Minimal/Dark)
  - Projects section:
    - Drag & drop to reorder
    - Project cards:
      * Title
      * Description
      * Tech stack tags
      * Image preview
      * Demo URL
      * Source URL
      * Edit/Remove buttons
    - "Add Project" button
    - "Import from GitHub" button
  - Action buttons:
    - "Save Draft" button
    - "Preview" button
    - "Publish" button (if unpublished)
    - "Unpublish" button (if published)

- **Tab 2: Preview**
  - Live preview of portfolio
  - Theme applied
  - "Back to Edit" button

- **Tab 3: Share**
  - Published status indicator
  - Public URL display
  - "Copy Link" button
  - QR code
  - Social share buttons (LinkedIn, Twitter, Facebook)
  - View count (future feature)

### User Actions & Flow

#### Action 1: Load Portfolio
```
1. User navigate to Portfolio Builder
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/portfolio/me
Authorization: Bearer {token}
```

**Backend Process:**
```csharp
// PortfolioController.GetMine()
1. Get userId from JWT
2. Query Portfolios:
   SELECT * FROM Portfolios 
   WHERE UserId = @userId
   ORDER BY CreatedAt DESC
   LIMIT 1
3. If not found: return 404
4. Get portfolio projects:
   SELECT * FROM PortfolioProjects
   WHERE PortfolioId = @portfolioId
   ORDER BY OrderIndex
5. Return portfolio with projects
```

**Database Operations:**
```sql
-- Get latest portfolio
SELECT * FROM "Portfolios" 
WHERE "UserId" = @userId
ORDER BY "CreatedAt" DESC
LIMIT 1;

-- Get portfolio projects
SELECT 
  pp.*,
  gr."RepoName",
  gr."RepoUrl",
  gr."MainLanguage"
FROM "PortfolioProjects" pp
LEFT JOIN "GithubRepositories" gr ON pp."GithubRepositoryId" = gr."Id"
WHERE pp."PortfolioId" = @portfolioId
ORDER BY pp."OrderIndex";
```

**Response:**
```json
{
  "id": "portfolio-guid",
  "slug": "nguyen-van-a",
  "title": "Nguyen Van A - Backend Developer",
  "bio": "Passionate backend developer with 2 years of experience in Java and Spring Boot. Love building scalable APIs and solving complex problems.",
  "theme": "Modern",
  "isPublished": true,
  "publishedAt": "2026-05-10T10:00:00Z",
  "createdAt": "2026-05-01T10:00:00Z",
  "updatedAt": "2026-05-10T10:00:00Z",
  "projects": [
    {
      "id": "project-1-guid",
      "githubRepositoryId": "repo-1-guid",
      "title": "E-commerce Backend API",
      "description": "Full-featured REST API for e-commerce platform",
      "techStackJson": "{\"languages\":[\"Java\"],\"frameworks\":[\"Spring Boot\"]}",
      "imageUrl": "https://storage.googleapis.com/bucket/portfolio/project1.png",
      "demoUrl": "https://api.example.com",
      "sourceUrl": "https://github.com/user/ecommerce-backend",
      "orderIndex": 0
    },
    {
      "id": "project-2-guid",
      "githubRepositoryId": null,
      "title": "Task Management System",
      "description": "Microservices-based task management",
      "techStackJson": "{\"languages\":[\"Java\"],\"frameworks\":[\"Spring Boot\",\"Docker\"]}",
      "imageUrl": null,
      "demoUrl": null,
      "sourceUrl": "https://github.com/user/task-manager",
      "orderIndex": 1
    }
  ]
}
```

**Frontend Display:**
```
3. Pre-fill form với portfolio data
4. Render project cards
5. Enable drag & drop for reordering
```

#### Action 2: Create/Update Portfolio
```
1. User chỉnh sửa title, bio, theme
2. User add/edit/remove projects
3. User reorder projects (drag & drop)
4. Click "Save Draft" button
5. Frontend gọi API:
```

**API Call (Create):**
```http
POST /api/portfolio
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Nguyen Van A - Backend Developer",
  "slug": "nguyen-van-a",
  "bio": "Passionate backend developer...",
  "theme": "Modern",
  "projects": [
    {
      "githubRepositoryId": "repo-1-guid",
      "title": "E-commerce Backend API",
      "description": "Full-featured REST API",
      "techStackJson": "{...}",
      "imageUrl": "https://...",
      "demoUrl": "https://...",
      "sourceUrl": "https://...",
      "orderIndex": 0
    }
  ]
}
```

**API Call (Update):**
```http
PUT /api/portfolio
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Updated Title",
  "slug": "new-slug",
  "bio": "Updated bio",
  "theme": "Dark",
  "projects": [...]
}
```

**Backend Process (Create):**
```csharp
// PortfolioController.Create()
1. Get userId from JWT
2. Validate input:
   - Title required
   - Slug valid format (lowercase, alphanumeric, hyphens)
3. Generate unique slug:
   - If slug provided: use it
   - Else: generate from title
   - Check uniqueness, add suffix if needed
4. Create Portfolio:
   - Id = Guid.NewGuid()
   - UserId = userId
   - Slug = uniqueSlug
   - Title = request.Title
   - Bio = request.Bio
   - Theme = request.Theme ?? "Default"
   - IsPublished = false
   - CreatedAt = now
   - UpdatedAt = now
5. Sync projects:
   - Validate GitHub repos belong to user
   - Create PortfolioProject for each
6. Save to database
7. Return created portfolio
```

**Backend Process (Update):**
```csharp
// PortfolioController.Update()
1. Get userId from JWT
2. Find existing portfolio (latest)
3. If not found: return 404
4. Update fields:
   - Title (if provided)
   - Slug (if changed, ensure unique)
   - Bio
   - Theme
   - UpdatedAt = now
5. Sync projects:
   - Delete all existing PortfolioProjects
   - Create new ones from request
6. Save to database
7. Return updated portfolio
```

**Database Operations:**
```sql
-- Check slug uniqueness
SELECT COUNT(*) FROM "Portfolios" 
WHERE "Slug" = @slug AND "Id" != @currentPortfolioId;

-- Insert portfolio
INSERT INTO "Portfolios" (
  "Id", "UserId", "Slug", "Title", "Bio", "Theme",
  "IsPublished", "PublishedAt", "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @userId, @slug, @title, @bio, @theme,
  false, NULL, @now, @now
);

-- Delete existing projects
DELETE FROM "PortfolioProjects" WHERE "PortfolioId" = @portfolioId;

-- Insert projects
INSERT INTO "PortfolioProjects" (
  "Id", "PortfolioId", "GithubRepositoryId",
  "Title", "Description", "TechStackJson",
  "ImageUrl", "DemoUrl", "SourceUrl", "OrderIndex",
  "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @portfolioId, @githubRepoId,
  @title, @description, @techStackJson,
  @imageUrl, @demoUrl, @sourceUrl, @orderIndex,
  @now, @now
);
```

**Response:**
```json
{
  "id": "portfolio-guid",
  "slug": "nguyen-van-a",
  "title": "Nguyen Van A - Backend Developer",
  "bio": "...",
  "theme": "Modern",
  "isPublished": false,
  "publishedAt": null,
  "updatedAt": "2026-05-16T14:20:00Z",
  "projects": [...]
}
```

#### Action 3: Publish Portfolio
```
1. User click "Publish" button
2. Frontend show confirmation
3. User confirm
4. Frontend gọi API:
```

**API Call:**
```http
POST /api/portfolio/publish
Authorization: Bearer {token}
```

**Backend Process:**
```csharp
// PortfolioController.Publish()
1. Get userId from JWT
2. Find latest portfolio
3. If not found: return 404
4. Update:
   - IsPublished = true
   - PublishedAt = now (if first time)
   - UpdatedAt = now
5. Save to database
6. Return updated portfolio
```

**Database Operations:**
```sql
-- Publish portfolio
UPDATE "Portfolios" SET
  "IsPublished" = true,
  "PublishedAt" = COALESCE("PublishedAt", @now),
  "UpdatedAt" = @now
WHERE "UserId" = @userId
  AND "Id" = (SELECT "Id" FROM "Portfolios" 
              WHERE "UserId" = @userId 
              ORDER BY "CreatedAt" DESC LIMIT 1);
```

**Response:**
```json
{
  "id": "portfolio-guid",
  "slug": "nguyen-van-a",
  "isPublished": true,
  "publishedAt": "2026-05-16T14:25:00Z",
  "publicUrl": "https://frontend.com/portfolio/nguyen-van-a"
}
```

**Frontend Action:**
```
5. Update UI to show published status
6. Enable share tab
7. Show success message with public URL
```

#### Action 4: Import Project from GitHub
```
1. User click "Import from GitHub" button
2. Frontend show modal với danh sách GitHub repos
3. User chọn repos
4. Frontend auto-fill project data từ repo:
   - Title = repo.RepoName
   - Description = repo.AiSummary ?? repo.Description
   - TechStackJson = repo.TechStackJson
   - SourceUrl = repo.RepoUrl
5. User có thể edit trước khi add
6. Add to projects list
```

#### Action 5: Upload Project Image
```
1. User click "Upload Image" trên project card
2. User chọn image file
3. Frontend gọi API:
```

**API Call:**
```http
POST /api/storage/portfolio-projects/{projectId}/image
Authorization: Bearer {token}
Content-Type: multipart/form-data

file: [binary data]
```

**Backend Process:**
```csharp
// StorageController.UploadPortfolioProjectImage()
1. Get userId from JWT
2. Verify project belongs to user's portfolio
3. Validate image (type, size)
4. Upload to Google Cloud Storage:
   - Path: portfolio/{userId}/{projectId}/{guid}.{ext}
5. Update PortfolioProjects:
   UPDATE PortfolioProjects SET ImageUrl = @url
6. Return file info
```

**Database Operations:**
```sql
-- Verify ownership
SELECT COUNT(*) 
FROM "PortfolioProjects" pp
INNER JOIN "Portfolios" p ON pp."PortfolioId" = p."Id"
WHERE pp."Id" = @projectId AND p."UserId" = @userId;

-- Update image URL
UPDATE "PortfolioProjects" SET
  "ImageUrl" = @imageUrl,
  "UpdatedAt" = @now
WHERE "Id" = @projectId;
```

#### Action 6: View Public Portfolio
```
1. Anyone (không cần login) truy cập:
   https://frontend.com/portfolio/{slug}
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/portfolio/{slug}
# No Authorization header needed
```

**Backend Process:**
```csharp
// PortfolioController.GetPublic()
1. Find portfolio by slug
2. Check IsPublished = true
3. If not published: return 404
4. Get projects
5. Return portfolio (public view)
```

**Database Operations:**
```sql
-- Get published portfolio
SELECT * FROM "Portfolios" 
WHERE "Slug" = @slug AND "IsPublished" = true;
```

### Database Tables Involved
- **Portfolios**: Store portfolio metadata
- **PortfolioProjects**: Store project details
- **GithubRepositories**: Link to GitHub repos

---

## 2.8 AI MENTOR CHAT SCREEN

### Mô tả
Màn hình chat với AI Mentor để tư vấn nghiệp nghiệp

### UI Components
- **Chat Interface:**
  - Message list (scrollable, auto-scroll to bottom)
  - Mỗi message:
    - Avatar (user/AI)
    - Message text (markdown rendering)
    - Timestamp
    - Copy button
  - Input box (textarea, auto-expand)
  - "Send" button
  - "New Chat" button

- **Sidebar:**
  - Chat history (sessions list)
  - Click to load previous chat
  - Context summary card:
    - Profile summary
    - Latest match score
    - Roadmap progress
    - GitHub repos count

- **Suggested Questions:**
  - "Em nên học gì tiếp theo?"
  - "Portfolio của em đã đủ tốt chưa?"
  - "Em phù hợp với vai trò nào?"
  - "Làm sao để cải thiện match score?"

### User Actions & Flow

#### Action 1: Send Message to AI
```
1. User nhập câu hỏi vào input box
2. Click "Send" button hoặc press Enter
3. Frontend hiển thị user message ngay lập tức
4. Frontend hiển thị loading indicator
5. Frontend gọi API:
```

**API Call:**
```http
POST /api/mentor/chat
Authorization: Bearer {token}
Content-Type: application/json

{
  "question": "Em đang học Spring Boot, em nên làm project gì để thực hành?",
  "contextJson": null
}
```

**Backend Process:**
```csharp
// MentorController.Chat()
1. Get userId from JWT
2. Validate question not empty
3. Build context from user data:
   a. Get StudentProfile:
      - School, Major, Year, GPA
      - Target role
      - Career goal
      - Preferred learning hours
   b. Get UserSkills:
      - List all skills with levels
   c. Get latest SkillGapReport:
      - Match score
      - Summary
      - Gap items (missing/weak skills)
   d. Get latest Roadmap:
      - Title, status, progress
      - Nodes (title, status, priority)
   e. Get top 5 GitHub repos:
      - Repo name, language, quality score
      - AI summary
   f. Combine into context string
4. Build AI prompt:
   System: "You are an AI career mentor for software engineering students.
            Give practical, concise, structured guidance in Vietnamese.
            Base the answer on the provided student profile, skills, 
            target role, skill gap, roadmap, GitHub and portfolio context.
            Do not invent facts not present in context."
   User: "Student context: {context}\n\nStudent question: {question}"
5. Call Gemini API:
   POST https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent
   Body: { contents: [{ parts: [{ text: prompt }] }] }
6. Parse AI response
7. Create MentorSession:
   - Id = Guid.NewGuid()
   - UserId = userId
   - Question = question
   - Answer = aiResponse.Text
   - ContextJson = context
   - Model = "gemini-pro"
   - TokensUsed = aiResponse.TokenCount
   - CreatedAt = now
8. Save to database
9. Return session
```

**Database Operations:**
```sql
-- Get profile
SELECT 
  sp.*,
  cr."Name" as "TargetRoleName"
FROM "StudentProfiles" sp
LEFT JOIN "CareerRoles" cr ON sp."TargetRoleId" = cr."Id"
WHERE sp."UserId" = @userId;

-- Get skills
SELECT 
  us."Level",
  us."IsVerified",
  s."Name",
  s."Category"
FROM "UserSkills" us
INNER JOIN "Skills" s ON us."SkillId" = s."Id"
WHERE us."UserId" = @userId
ORDER BY s."Category", s."Name";

-- Get latest skill gap
SELECT 
  sgr.*,
  cr."Name" as "CareerRoleName"
FROM "SkillGapReports" sgr
INNER JOIN "CareerRoles" cr ON sgr."CareerRoleId" = cr."Id"
WHERE sgr."UserId" = @userId
ORDER BY sgr."CreatedAt" DESC
LIMIT 1;

-- Get gap items
SELECT 
  sgri.*,
  s."Name" as "SkillName"
FROM "SkillGapReportItems" sgri
INNER JOIN "Skills" s ON sgri."SkillId" = s."Id"
WHERE sgri."SkillGapReportId" = @reportId
ORDER BY sgri."Priority";

-- Get latest roadmap
SELECT * FROM "Roadmaps"
WHERE "UserId" = @userId
ORDER BY "CreatedAt" DESC
LIMIT 1;

-- Get roadmap nodes
SELECT * FROM "RoadmapNodes"
WHERE "RoadmapId" = @roadmapId
ORDER BY "OrderIndex";

-- Get top GitHub repos
SELECT * FROM "GithubRepositories"
WHERE "UserId" = @userId
ORDER BY "QualityScore" DESC
LIMIT 5;

-- Insert mentor session
INSERT INTO "MentorSessions" (
  "Id", "UserId", "Question", "Answer", "ContextJson",
  "Model", "TokensUsed", "CreatedAt"
) VALUES (
  @id, @userId, @question, @answer, @contextJson,
  @model, @tokensUsed, @now
);
```

**Response:**
```json
{
  "id": "session-guid",
  "question": "Em đang học Spring Boot, em nên làm project gì để thực hành?",
  "answer": "Dựa vào profile của em (năm 3, GPA 3.5, target Backend Developer), em nên làm các project sau:\n\n1. **REST API cho quản lý sản phẩm** (20-30 giờ)\n   - CRUD operations\n   - JWT authentication\n   - PostgreSQL database\n   - Unit testing với JUnit\n\n2. **Microservices với Spring Cloud** (40-50 giờ)\n   - Service discovery (Eureka)\n   - API Gateway\n   - Config server\n   - Docker deployment\n\n3. **Real-time chat application** (30-40 giờ)\n   - WebSocket\n   - Redis pub/sub\n   - Message persistence\n\nEm nên bắt đầu với project 1 vì nó cover được các kỹ năng cơ bản mà em đang thiếu (theo skill gap report). Sau đó tiếp tục với project 2 để nâng cao.",
  "contextJson": "{...}",
  "model": "gemini-pro",
  "tokensUsed": 1250,
  "createdAt": "2026-05-16T14:30:00Z"
}
```

**Frontend Action:**
```
6. Hiển thị AI response trong chat
7. Render markdown (bold, lists, code blocks)
8. Auto-scroll to bottom
9. Enable input for next question
```

#### Action 2: Load Chat History
```
1. User click "New Chat" hoặc navigate to chat screen
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/mentor/sessions
Authorization: Bearer {token}
```

**Backend Process:**
```csharp
// MentorController.GetSessions()
1. Get userId from JWT
2. Query MentorSessions:
   SELECT * FROM MentorSessions
   WHERE UserId = @userId
   ORDER BY CreatedAt DESC
3. Return list of sessions
```

**Response:**
```json
[
  {
    "id": "session-1-guid",
    "question": "Em nên học gì tiếp theo?",
    "answer": "...",
    "createdAt": "2026-05-16T14:30:00Z"
  },
  {
    "id": "session-2-guid",
    "question": "Portfolio của em đã đủ tốt chưa?",
    "answer": "...",
    "createdAt": "2026-05-15T10:00:00Z"
  }
]
```

### Database Tables Involved
- **MentorSessions**: Store chat history
- **StudentProfiles**: Build context
- **UserSkills**: Build context
- **SkillGapReports**: Build context
- **Roadmaps**: Build context
- **GithubRepositories**: Build context

---


# 3. ADMIN SCREENS

## 3.1 ADMIN DASHBOARD

### Mô tả
Màn hình tổng quan cho Admin quản trị hệ thống

### UI Components
- **Statistics Cards:**
  - Total Users
  - Total Students
  - Total Skills
  - Total Career Roles
  - Active Subscriptions (future)
  - Total Revenue (future)

- **Charts:**
  - Users growth over time (line chart)
  - Popular career roles (bar chart)
  - Popular skills (bar chart)
  - Skill gap distribution (pie chart)

- **Recent Activity:**
  - New user registrations
  - New skill gap analyses
  - New roadmaps created
  - New portfolios published

- **Quick Actions:**
  - "Manage Skills" button
  - "Manage Career Roles" button
  - "Manage Users" button
  - "View Reports" button

### User Actions & Flow

#### Action 1: Load Dashboard Stats
```
1. Admin login và navigate to dashboard
2. Frontend gọi multiple APIs:
```

**API Calls:**
```http
# Get user stats
GET /api/admin/stats/users
Authorization: Bearer {adminToken}

# Get skill stats
GET /api/admin/stats/skills
Authorization: Bearer {adminToken}

# Get career role stats
GET /api/admin/stats/career-roles
Authorization: Bearer {adminToken}
```

**Backend Process (CẦN IMPLEMENT):**
```csharp
// AdminController.GetUserStats()
1. Verify admin role
2. Query statistics:
   - Total users: COUNT(*) FROM Users
   - Students: COUNT(*) WHERE Role = 'Student'
   - Active users: COUNT(*) WHERE IsActive = true
   - New users this month: COUNT(*) WHERE CreatedAt >= startOfMonth
3. Return stats
```

**Database Operations:**
```sql
-- User statistics
SELECT 
  COUNT(*) as "TotalUsers",
  SUM(CASE WHEN "Role" = 'Student' THEN 1 ELSE 0 END) as "TotalStudents",
  SUM(CASE WHEN "IsActive" = true THEN 1 ELSE 0 END) as "ActiveUsers",
  SUM(CASE WHEN "CreatedAt" >= @startOfMonth THEN 1 ELSE 0 END) as "NewUsersThisMonth"
FROM "Users";

-- Skill statistics
SELECT 
  COUNT(*) as "TotalSkills",
  SUM(CASE WHEN "IsActive" = true THEN 1 ELSE 0 END) as "ActiveSkills"
FROM "Skills";

-- Career role statistics
SELECT 
  COUNT(*) as "TotalRoles",
  SUM(CASE WHEN "IsActive" = true THEN 1 ELSE 0 END) as "ActiveRoles"
FROM "CareerRoles";

-- Popular career roles
SELECT 
  cr."Name",
  COUNT(sp."Id") as "StudentCount"
FROM "CareerRoles" cr
LEFT JOIN "StudentProfiles" sp ON cr."Id" = sp."TargetRoleId"
GROUP BY cr."Id", cr."Name"
ORDER BY "StudentCount" DESC
LIMIT 10;

-- Popular skills
SELECT 
  s."Name",
  s."Category",
  COUNT(us."Id") as "UserCount"
FROM "Skills" s
LEFT JOIN "UserSkills" us ON s."Id" = us."SkillId"
GROUP BY s."Id", s."Name", s."Category"
ORDER BY "UserCount" DESC
LIMIT 10;
```

---

## 3.2 SKILLS MANAGEMENT SCREEN

### Mô tả
Màn hình quản lý danh sách kỹ năng trong hệ thống

### UI Components
- **Header:**
  - "Add Skill" button
  - Search box
  - Filter by category dropdown
  - Filter by status (Active/Inactive)

- **Skills Table:**
  - Columns:
    * Name
    * Category
    * Description
    * IsActive (toggle)
    * Usage Count (số users đang dùng)
    * CreatedAt
    * Actions (Edit/Delete)
  - Pagination
  - Sort by column

### User Actions & Flow

#### Action 1: Load Skills List
```
1. Admin navigate to Skills Management
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/admin/skills
Authorization: Bearer {adminToken}
```

**Backend Process:**
```csharp
// AdminController.GetSkills() - ĐÃ IMPLEMENT
1. Verify admin role
2. Query Skills:
   SELECT * FROM Skills
   ORDER BY Category, Name
3. Return list
```

**Response:**
```json
[
  {
    "id": "skill-1-guid",
    "name": "Java",
    "category": "Programming Language",
    "description": "Object-oriented programming language",
    "isActive": true,
    "createdAt": "2026-01-01T00:00:00Z",
    "updatedAt": "2026-01-01T00:00:00Z"
  }
]
```

#### Action 2: Add New Skill
```
1. Admin click "Add Skill" button
2. Frontend hiển thị modal form:
   - Name (required)
   - Category (required)
   - Description (optional)
   - IsActive (checkbox, default true)
3. Admin điền form và submit
4. Frontend gọi API:
```

**API Call:**
```http
POST /api/admin/skills
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "name": "Kubernetes",
  "category": "DevOps",
  "description": "Container orchestration platform",
  "isActive": true
}
```

**Backend Process:**
```csharp
// AdminController.CreateSkill() - ĐÃ IMPLEMENT
1. Verify admin role
2. Validate input:
   - Name required
   - Category required
3. Check duplicate:
   SELECT COUNT(*) FROM Skills 
   WHERE Name = @name AND Category = @category
4. If duplicate: return 409 Conflict
5. Create Skill:
   - Id = Guid.NewGuid()
   - Name = request.Name.Trim()
   - Category = request.Category.Trim()
   - Description = request.Description?.Trim()
   - IsActive = request.IsActive ?? true
   - CreatedAt = now
   - UpdatedAt = now
6. Save to database
7. Return created skill
```

**Database Operations:**
```sql
-- Check duplicate
SELECT COUNT(*) FROM "Skills" 
WHERE "Name" = @name AND "Category" = @category;

-- Insert skill
INSERT INTO "Skills" (
  "Id", "Name", "Category", "Description", "IsActive",
  "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @name, @category, @description, @isActive,
  @now, @now
);
```

#### Action 3: Edit Skill
```
1. Admin click "Edit" button
2. Frontend hiển thị edit modal (pre-filled)
3. Admin chỉnh sửa và submit
4. Frontend gọi API:
```

**API Call:**
```http
PUT /api/admin/skills/{skillId}
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "name": "Kubernetes",
  "category": "DevOps",
  "description": "Updated description",
  "isActive": true
}
```

**Backend Process:**
```csharp
// AdminController.UpdateSkill() - ĐÃ IMPLEMENT
1. Verify admin role
2. Find skill by ID
3. If not found: return 404
4. Validate input
5. Check duplicate (exclude current skill)
6. Update fields:
   - Name
   - Category
   - Description
   - IsActive
   - UpdatedAt = now
7. Save to database
8. Return updated skill
```

#### Action 4: Delete Skill
```
1. Admin click "Delete" button
2. Frontend hiển thị confirmation dialog
3. Admin confirm
4. Frontend gọi API:
```

**API Call:**
```http
DELETE /api/admin/skills/{skillId}
Authorization: Bearer {adminToken}
```

**Backend Process:**
```csharp
// AdminController.DeleteSkill() - ĐÃ IMPLEMENT
1. Verify admin role
2. Find skill
3. Check if skill is being used:
   - UserSkills
   - RoleSkillRequirements
   - GithubRepositorySkills
   - SkillGapReportItems
   - RoadmapNodes
4. If used:
   - Set IsActive = false (soft delete)
   - Return message: "Skill is in use, marked as inactive"
5. If not used:
   - Hard delete from database
   - Return 204 No Content
```

**Database Operations:**
```sql
-- Check usage
SELECT 
  (SELECT COUNT(*) FROM "UserSkills" WHERE "SkillId" = @skillId) +
  (SELECT COUNT(*) FROM "RoleSkillRequirements" WHERE "SkillId" = @skillId) +
  (SELECT COUNT(*) FROM "GithubRepositorySkills" WHERE "SkillId" = @skillId) +
  (SELECT COUNT(*) FROM "SkillGapReportItems" WHERE "SkillId" = @skillId) +
  (SELECT COUNT(*) FROM "RoadmapNodes" WHERE "SkillId" = @skillId)
as "UsageCount";

-- Soft delete
UPDATE "Skills" SET
  "IsActive" = false,
  "UpdatedAt" = @now
WHERE "Id" = @skillId;

-- Hard delete
DELETE FROM "Skills" WHERE "Id" = @skillId;
```

### Database Tables Involved
- **Skills**: CRUD operations
- **UserSkills**: Check usage
- **RoleSkillRequirements**: Check usage

---

## 3.3 CAREER ROLES MANAGEMENT SCREEN

### Mô tả
Màn hình quản lý các vai trò nghiệp nghiệp

### UI Components
- **Header:**
  - "Add Career Role" button
  - Search box
  - Filter by level (Intern/Junior/Mid/Senior)
  - Filter by status (Active/Inactive)

- **Roles Table:**
  - Columns:
    * Name
    * Description
    * Level
    * IsActive
    * Student Count (số students chọn role này)
    * Skill Requirements Count
    * CreatedAt
    * Actions (Edit/Delete/View Requirements)

### User Actions & Flow

#### Action 1: Add Career Role
```
1. Admin click "Add Career Role" button
2. Frontend hiển thị form modal
3. Admin điền:
   - Name (required)
   - Description (optional)
   - Level (dropdown: Intern/Junior/Mid/Senior)
   - IsActive (checkbox)
4. Submit
5. Frontend gọi API:
```

**API Call:**
```http
POST /api/career-roles
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "name": "DevOps Engineer",
  "description": "Manages infrastructure and deployment pipelines",
  "level": "Junior"
}
```

**Backend Process:**
```csharp
// CareerRolesController.CreateCareerRole() - ĐÃ IMPLEMENT
1. Verify admin role
2. Validate name required
3. Check duplicate name
4. Create CareerRole:
   - Id = Guid.NewGuid()
   - Name = request.Name
   - Description = request.Description
   - Level = request.Level
   - IsActive = true
   - CreatedAt = now
   - UpdatedAt = now
5. Save to database
6. Return created role
```

**Database Operations:**
```sql
-- Check duplicate
SELECT COUNT(*) FROM "CareerRoles" WHERE "Name" = @name;

-- Insert role
INSERT INTO "CareerRoles" (
  "Id", "Name", "Description", "Level", "IsActive",
  "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @name, @description, @level, true,
  @now, @now
);
```

#### Action 2: View Skill Requirements
```
1. Admin click "View Requirements" button
2. Frontend navigate to Role-Skill Requirements screen
3. Pre-filter by selected role
```

### Database Tables Involved
- **CareerRoles**: CRUD operations
- **StudentProfiles**: Count students
- **RoleSkillRequirements**: Count requirements

---

## 3.4 ROLE-SKILL REQUIREMENTS SCREEN

### Mô tả
Màn hình quản lý yêu cầu kỹ năng cho từng vai trò

### UI Components
- **Header:**
  - Career Role selector (dropdown)
  - "Add Requirement" button

- **Requirements Table:**
  - Columns:
    * Skill Name
    * Skill Category
    * Required Level (Beginner/Intermediate/Advanced/Expert)
    * Priority (1-5 stars)
    * Weight (decimal)
    * Actions (Edit/Delete)
  - Sort by priority

### User Actions & Flow

#### Action 1: Load Requirements for Role
```
1. Admin chọn career role từ dropdown
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/admin/role-skill-requirements?careerRoleId={roleId}
Authorization: Bearer {adminToken}
```

**Backend Process:**
```csharp
// AdminController.GetRoleSkillRequirements() - ĐÃ IMPLEMENT
1. Verify admin role
2. Query RoleSkillRequirements:
   SELECT rsr.*, s.Name, s.Category, cr.Name as RoleName
   FROM RoleSkillRequirements rsr
   INNER JOIN Skills s ON rsr.SkillId = s.Id
   INNER JOIN CareerRoles cr ON rsr.CareerRoleId = cr.Id
   WHERE rsr.CareerRoleId = @roleId (if provided)
   ORDER BY rsr.Priority DESC, s.Name
3. Return list
```

**Database Operations:**
```sql
SELECT 
  rsr.*,
  s."Name" as "SkillName",
  s."Category" as "SkillCategory",
  cr."Name" as "CareerRoleName"
FROM "RoleSkillRequirements" rsr
INNER JOIN "Skills" s ON rsr."SkillId" = s."Id"
INNER JOIN "CareerRoles" cr ON rsr."CareerRoleId" = cr."Id"
WHERE rsr."CareerRoleId" = @roleId
ORDER BY rsr."Priority" DESC, s."Name";
```

**Response:**
```json
[
  {
    "id": "req-1-guid",
    "careerRoleId": "role-guid",
    "careerRoleName": "Backend Developer",
    "skillId": "skill-1-guid",
    "skillName": "Java",
    "skillCategory": "Programming Language",
    "requiredLevel": "Advanced",
    "priority": 5,
    "weight": 10.0,
    "createdAt": "2026-01-01T00:00:00Z"
  }
]
```

#### Action 2: Add Requirement
```
1. Admin click "Add Requirement" button
2. Frontend hiển thị form modal:
   - Career Role (dropdown, pre-selected)
   - Skill (dropdown)
   - Required Level (dropdown)
   - Priority (1-5)
   - Weight (number input)
3. Admin điền và submit
4. Frontend gọi API:
```

**API Call:**
```http
POST /api/admin/role-skill-requirements
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "careerRoleId": "role-guid",
  "skillId": "skill-guid",
  "requiredLevel": "Intermediate",
  "priority": 4,
  "weight": 8.0
}
```

**Backend Process:**
```csharp
// AdminController.CreateRoleSkillRequirement() - ĐÃ IMPLEMENT
1. Verify admin role
2. Validate input:
   - CareerRoleId exists and active
   - SkillId exists and active
   - RequiredLevel valid
   - Priority 1-5
   - Weight > 0
3. Check duplicate:
   SELECT COUNT(*) FROM RoleSkillRequirements
   WHERE CareerRoleId = @roleId AND SkillId = @skillId
4. If duplicate: return 409 Conflict
5. Create RoleSkillRequirement:
   - Id = Guid.NewGuid()
   - CareerRoleId = request.CareerRoleId
   - SkillId = request.SkillId
   - RequiredLevel = request.RequiredLevel
   - Priority = request.Priority
   - Weight = request.Weight
   - CreatedAt = now
   - UpdatedAt = now
6. Save to database
7. Return created requirement
```

**Database Operations:**
```sql
-- Validate career role
SELECT COUNT(*) FROM "CareerRoles" 
WHERE "Id" = @roleId AND "IsActive" = true;

-- Validate skill
SELECT COUNT(*) FROM "Skills" 
WHERE "Id" = @skillId AND "IsActive" = true;

-- Check duplicate
SELECT COUNT(*) FROM "RoleSkillRequirements" 
WHERE "CareerRoleId" = @roleId AND "SkillId" = @skillId;

-- Insert requirement
INSERT INTO "RoleSkillRequirements" (
  "Id", "CareerRoleId", "SkillId", "RequiredLevel",
  "Priority", "Weight", "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @roleId, @skillId, @requiredLevel,
  @priority, @weight, @now, @now
);
```

#### Action 3: Update Requirement
```
1. Admin click "Edit" button
2. Frontend hiển thị edit modal (pre-filled)
3. Admin chỉnh sửa required level, priority, hoặc weight
4. Submit
5. Frontend gọi API:
```

**API Call:**
```http
PUT /api/admin/role-skill-requirements/{requirementId}
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "careerRoleId": "role-guid",
  "skillId": "skill-guid",
  "requiredLevel": "Advanced",
  "priority": 5,
  "weight": 12.0
}
```

**Backend Process:**
```csharp
// AdminController.UpdateRoleSkillRequirement() - ĐÃ IMPLEMENT
1. Verify admin role
2. Find requirement by ID
3. If not found: return 404
4. Validate input
5. Update fields:
   - RequiredLevel
   - Priority
   - Weight
   - UpdatedAt = now
6. Save to database
7. Return updated requirement
```

#### Action 4: Delete Requirement
```
1. Admin click "Delete" button
2. Frontend confirmation
3. Admin confirm
4. Frontend gọi API:
```

**API Call:**
```http
DELETE /api/admin/role-skill-requirements/{requirementId}
Authorization: Bearer {adminToken}
```

**Backend Process:**
```csharp
// AdminController.DeleteRoleSkillRequirement() - ĐÃ IMPLEMENT
1. Verify admin role
2. Find requirement
3. If not found: return 404
4. Delete from database
5. Return 204 No Content
```

**Database Operations:**
```sql
DELETE FROM "RoleSkillRequirements" WHERE "Id" = @requirementId;
```

### Database Tables Involved
- **RoleSkillRequirements**: CRUD operations
- **CareerRoles**: Validate and display
- **Skills**: Validate and display

---

## 3.5 LEARNING RESOURCES MANAGEMENT SCREEN

### Mô tả
Màn hình quản lý tài nguyên học tập

### UI Components
- **Header:**
  - "Add Resource" button
  - Search box
  - Filter by skill
  - Filter by resource type
  - Filter by difficulty

- **Resources Table:**
  - Columns:
    * Title
    * Skill Name
    * Resource Type (Video/Article/Course/Book/Tutorial)
    * Difficulty (Beginner/Intermediate/Advanced)
    * Estimated Hours
    * URL (link)
    * Source Type (Link/File)
    * Content Type (nếu là file)
    * File Size (nếu là file)
    * IsActive
    * Actions (Edit/Delete)

### User Actions & Flow

#### Action 1: Add Learning Resource
```
1. Admin click "Add Resource" button
2. Frontend hiển thị form modal:
   - Skill (dropdown, optional)
   - Title (required)
   - URL (required)
   - Resource Type (dropdown)
   - Difficulty (dropdown)
   - Estimated Hours (number)
   - IsActive (checkbox)
3. Admin điền và submit
4. Frontend gọi API:
```

**API Call:**
```http
POST /api/admin/learning-resources
Authorization: Bearer {adminToken}
Content-Type: application/json

{
  "skillId": "skill-guid",
  "title": "Spring Boot Masterclass",
  "url": "https://udemy.com/spring-boot-masterclass",
  "resourceType": "Course",
  "difficulty": "Intermediate",
  "estimatedHours": 30,
  "isActive": true
}
```

**Backend Process:**
```csharp
// AdminController.CreateLearningResource() - ĐÃ IMPLEMENT
1. Verify admin role
2. Validate input:
   - Title required
   - URL required and valid
   - ResourceType required
   - EstimatedHours >= 0
   - SkillId exists if provided
3. Create LearningResource:
   - Id = Guid.NewGuid()
   - SkillId = request.SkillId
   - Title = request.Title
   - Url = request.Url
   - StorageObjectName = null
   - ContentType = null
   - FileSize = null
   - ResourceType = request.ResourceType
   - Difficulty = request.Difficulty
   - EstimatedHours = request.EstimatedHours
   - IsActive = request.IsActive ?? true
   - CreatedAt = now
   - UpdatedAt = now
4. Save to database
5. Return created resource
```

**Database Operations:**
```sql
-- Validate skill (if provided)
SELECT COUNT(*) FROM "Skills" 
WHERE "Id" = @skillId AND "IsActive" = true;

-- Insert resource
INSERT INTO "LearningResources" (
  "Id", "SkillId", "Title", "Url",
  "StorageObjectName", "ContentType", "FileSize",
  "ResourceType", "Difficulty", "EstimatedHours", "IsActive",
  "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @skillId, @title, @url,
  null, null, null,
  @resourceType, @difficulty, @estimatedHours, @isActive,
  @now, @now
);
```

#### Action 2: Upload Learning Resource File
```
1. Admin click "Upload Resource File" hoặc chọn tab File trong modal Add Resource
2. Frontend hiển thị form upload:
   - Skill (dropdown, optional)
   - Title (required)
   - Resource Type (dropdown)
   - Difficulty (dropdown)
   - Estimated Hours (number)
   - File (required: PDF, DOC/DOCX, PPT/PPTX, XLS/XLSX, TXT/MD, JPG/PNG/WEBP)
   - IsActive (checkbox)
3. Admin chọn file và submit
4. Frontend gọi API:
```

**API Call:**
```http
POST /api/admin/learning-resources/upload
Authorization: Bearer {adminToken}
Content-Type: multipart/form-data

skillId=skill-guid
title=Java Concurrency in Practice
resourceType=Book
difficulty=Intermediate
estimatedHours=8
isActive=true
file=@java-concurrency.pdf
```

**Backend Process:**
```csharp
// AdminController.UploadLearningResource() - ĐÃ IMPLEMENT
1. Verify admin role
2. Validate input:
   - Title required
   - ResourceType required
   - EstimatedHours >= 0
   - File required
   - File size <= StorageOptions.MaxUploadBytes
   - ContentType thuộc danh sách được phép
   - SkillId exists if provided
3. Upload file to Google Cloud Storage:
   - ObjectName = learning-resources/{resourceId}/{timestamp}-{guid}-{safe-file-name}
4. Create LearningResource:
   - Id = resourceId
   - SkillId = request.SkillId
   - Title = request.Title
   - Url = /api/storage/learning-resources/{resourceId}/download
   - StorageObjectName = uploaded.ObjectName
   - ContentType = uploaded.ContentType
   - FileSize = uploaded.Size
   - ResourceType = request.ResourceType
   - Difficulty = request.Difficulty
   - EstimatedHours = request.EstimatedHours
   - IsActive = request.IsActive ?? true
   - CreatedAt = now
   - UpdatedAt = now
5. Save to database
6. Return created resource with SourceType = File
```

**Database Operations:**
```sql
-- Insert uploaded file resource
INSERT INTO "LearningResources" (
  "Id", "SkillId", "Title", "Url",
  "StorageObjectName", "ContentType", "FileSize",
  "ResourceType", "Difficulty", "EstimatedHours", "IsActive",
  "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @skillId, @title, @downloadUrl,
  @storageObjectName, @contentType, @fileSize,
  @resourceType, @difficulty, @estimatedHours, @isActive,
  @now, @now
);
```

**Student Access:**
```http
GET /api/storage/learning-resources/{resourceId}/download
Authorization: Bearer {studentToken}
```

```http
GET /api/storage/learning-resources/{resourceId}/signed-url
Authorization: Bearer {studentToken}
```

### Database Tables Involved
- **LearningResources**: CRUD operations
- **Skills**: Link resources to skills
- **Google Cloud Storage**: Lưu file tài liệu khi resource là file

---


# 4. COUNSELOR SCREENS

## 4.1 COUNSELOR DASHBOARD

### Mô tả
Màn hình tổng quan cho Academic Counselor

### UI Components
- **Statistics Cards:**
  - Assigned Students Count
  - Feedback Given Count
  - Students Needing Help Count
  - Average Student Progress

- **Students List:**
  - Student cards/table:
    * Avatar
    * Name, Email
    * School, Major, Year
    * Target Role
    * Match Score (progress bar)
    * Roadmap Progress
    * Last Activity
    * "View Profile" button
    * "Give Feedback" button

- **Filters:**
  - Filter by school
  - Filter by year
  - Filter by target role
  - Sort by match score/progress

### User Actions & Flow

#### Action 1: Load Assigned Students
```
1. Counselor login và navigate to dashboard
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/counselor/students
Authorization: Bearer {counselorToken}
```

**Backend Process (CẦN IMPLEMENT):**
```csharp
// CounselorController.GetAssignedStudents()
1. Verify counselor role
2. Get counselorId from JWT
3. Query assigned students:
   - Option 1: Từ bảng StudentCounselorAssignment (nếu có)
   - Option 2: Tất cả students (nếu counselor quản lý tất cả)
4. For each student:
   - Get StudentProfile
   - Get latest SkillGapReport (match score)
   - Get latest Roadmap (progress)
   - Get last activity timestamp
5. Return list of students with summary data
```

**Database Operations:**
```sql
-- Get students with profiles
SELECT 
  u."Id" as "UserId",
  u."Email",
  u."FullName",
  u."AvatarUrl",
  sp."School",
  sp."Major",
  sp."Year",
  cr."Name" as "TargetRoleName"
FROM "Users" u
INNER JOIN "StudentProfiles" sp ON u."Id" = sp."UserId"
LEFT JOIN "CareerRoles" cr ON sp."TargetRoleId" = cr."Id"
WHERE u."Role" = 'Student' AND u."IsActive" = true
ORDER BY u."FullName";

-- Get latest match scores
SELECT DISTINCT ON ("UserId")
  "UserId",
  "MatchScore",
  "CreatedAt"
FROM "SkillGapReports"
ORDER BY "UserId", "CreatedAt" DESC;

-- Get latest roadmap progress
SELECT DISTINCT ON ("UserId")
  "UserId",
  "Progress",
  "UpdatedAt"
FROM "Roadmaps"
ORDER BY "UserId", "CreatedAt" DESC;
```

**Response:**
```json
[
  {
    "userId": "student-1-guid",
    "email": "student1@example.com",
    "fullName": "Nguyen Van A",
    "avatarUrl": "https://...",
    "school": "FPT University",
    "major": "Software Engineering",
    "year": 3,
    "targetRoleName": "Backend Developer",
    "matchScore": 65.5,
    "roadmapProgress": 35.0,
    "lastActivity": "2026-05-16T10:00:00Z"
  }
]
```

#### Action 2: View Student Profile
```
1. Counselor click "View Profile" button
2. Frontend navigate to Student Profile View screen
3. Load full student data
```

---

## 4.2 STUDENT PROFILE VIEW (COUNSELOR)

### Mô tả
Màn hình xem chi tiết hồ sơ sinh viên (read-only cho counselor)

### UI Components
- **Tabs:**
  - **Profile Tab:** Thông tin cá nhân
  - **Skills Tab:** Danh sách kỹ năng
  - **Skill Gap Tab:** Báo cáo skill gap
  - **Roadmap Tab:** Lộ trình học tập
  - **GitHub Tab:** Repositories
  - **Feedback Tab:** Xem/thêm feedback

### User Actions & Flow

#### Action 1: Load Student Data
```
1. Counselor navigate to student profile
2. Frontend gọi multiple APIs:
```

**API Calls:**
```http
# Get student profile
GET /api/counselor/students/{studentId}/profile
Authorization: Bearer {counselorToken}

# Get student skills
GET /api/counselor/students/{studentId}/skills
Authorization: Bearer {counselorToken}

# Get latest skill gap
GET /api/counselor/students/{studentId}/skill-gap
Authorization: Bearer {counselorToken}

# Get roadmap
GET /api/counselor/students/{studentId}/roadmap
Authorization: Bearer {counselorToken}
```

**Backend Process (CẦN IMPLEMENT):**
```csharp
// CounselorController.GetStudentProfile()
1. Verify counselor role
2. Verify counselor has access to this student
3. Query StudentProfile with related data
4. Return profile data
```

#### Action 2: Give Feedback
```
1. Counselor navigate to Feedback tab
2. Counselor click "Add Feedback" button
3. Frontend hiển thị feedback form:
   - Feedback text (textarea, required)
   - Rating (1-5 stars, optional)
   - Recommendations (textarea, optional)
   - Private notes (textarea, optional)
4. Counselor điền và submit
5. Frontend gọi API:
```

**API Call:**
```http
POST /api/counselor/feedback
Authorization: Bearer {counselorToken}
Content-Type: application/json

{
  "studentId": "student-guid",
  "feedbackText": "Em đang tiến bộ tốt. Nên tập trung vào các kỹ năng DevOps để hoàn thiện profile.",
  "rating": 4,
  "recommendations": "1. Học Docker và Kubernetes\n2. Làm project với CI/CD\n3. Cải thiện GitHub README",
  "privateNotes": "Student is motivated but needs guidance on DevOps"
}
```

**Backend Process (CẦN IMPLEMENT):**
```csharp
// CounselorController.CreateFeedback()
1. Verify counselor role
2. Get counselorId from JWT
3. Validate input:
   - StudentId exists
   - Comment not empty
   - RoadmapId / SkillGapReportId valid if provided
4. Create CounselorFeedback:
   - Id = Guid.NewGuid()
   - CounselorId = counselorId
   - StudentId = request.StudentId
   - RoadmapId = request.RoadmapId
   - SkillGapReportId = request.SkillGapReportId
   - Comment = request.Comment
   - CreatedAt = now
   - UpdatedAt = now
5. Save to database
6. Optional: Send notification to student
7. Return created feedback
```

**Database Operations:**
```sql
-- Verify student exists
SELECT COUNT(*) FROM "users" 
WHERE "Id" = @studentId AND "Role" = 'Student';

-- Insert feedback
INSERT INTO "counselor_feedbacks" (
  "Id", "CounselorId", "StudentId", "RoadmapId",
  "SkillGapReportId", "Comment", "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @counselorId, @studentId, @roadmapId,
  @skillGapReportId, @comment, @now, @now
);
```

**Response:**
```json
{
  "id": "feedback-guid",
  "counselorId": "counselor-guid",
  "counselorName": "Dr. Nguyen Thi B",
  "studentId": "student-guid",
  "roadmapId": "roadmap-guid",
  "skillGapReportId": "report-guid",
  "comment": "Em đang tiến bộ tốt...",
  "createdAt": "2026-05-16T14:30:00Z"
}
```

#### Action 3: View Feedback History
```
1. Counselor xem Feedback tab
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/counselor/students/{studentId}/feedback
Authorization: Bearer {counselorToken}
```

**Backend Process (CẦN IMPLEMENT):**
```csharp
// CounselorController.GetStudentFeedback()
1. Verify counselor role
2. Query CounselorFeedback:
   SELECT * FROM CounselorFeedback
   WHERE StudentId = @studentId
   ORDER BY CreatedAt DESC
3. Include counselor info
4. Return list of feedback
```

**Database Operations:**
```sql
SELECT 
  cf.*,
  u."FullName" as "CounselorName"
FROM "CounselorFeedback" cf
INNER JOIN "Users" u ON cf."CounselorId" = u."Id"
WHERE cf."StudentId" = @studentId
ORDER BY cf."CreatedAt" DESC;
```

### Database Tables Involved
- **CounselorFeedback**: Store feedback
- **Users**: Get counselor and student info
- **StudentProfiles**: View student data
- **SkillGapReports**: View gap analysis
- **Roadmaps**: View learning path

---

# 5. INDUSTRY MENTOR SCREENS

## 5.1 MENTOR DASHBOARD

### Mô tả
Màn hình tổng quan cho Industry Mentor

### UI Components
- **Statistics Cards:**
  - Portfolios Reviewed Count
  - Feedback Given Count
  - Students in Review Queue
  - Average Portfolio Score

- **Review Queue:**
  - Student cards:
    * Avatar
    * Name
    * Target Role
    * Portfolio link
    * GitHub link
    * Submitted date
    * "Review" button

### User Actions & Flow

#### Action 1: Load Review Queue
```
1. Mentor login và navigate to dashboard
2. Frontend gọi API:
```

**API Call:**
```http
GET /api/mentor/review-queue
Authorization: Bearer {mentorToken}
```

**Backend Process (CẦN IMPLEMENT):**
```csharp
// MentorController.GetReviewQueue()
1. Verify mentor role
2. Query students with published portfolios:
   - Option 1: Students assigned to this mentor
   - Option 2: All students needing review
3. Filter students who haven't been reviewed recently
4. Return list with portfolio links
```

**Database Operations:**
```sql
-- Get students with published portfolios
SELECT 
  u."Id" as "UserId",
  u."FullName",
  u."AvatarUrl",
  sp."TargetRoleId",
  cr."Name" as "TargetRoleName",
  p."Slug" as "PortfolioSlug",
  p."PublishedAt",
  gc."GithubUsername"
FROM "Users" u
INNER JOIN "StudentProfiles" sp ON u."Id" = sp."UserId"
LEFT JOIN "CareerRoles" cr ON sp."TargetRoleId" = cr."Id"
INNER JOIN "Portfolios" p ON u."Id" = p."UserId"
LEFT JOIN "GithubConnections" gc ON u."Id" = gc."UserId"
WHERE u."Role" = 'Student' 
  AND p."IsPublished" = true
  AND u."Id" NOT IN (
    SELECT "StudentId" FROM "MentorFeedback" 
    WHERE "CreatedAt" > @recentThreshold
  )
ORDER BY p."PublishedAt" DESC;
```

**Response:**
```json
[
  {
    "userId": "student-guid",
    "fullName": "Nguyen Van A",
    "avatarUrl": "https://...",
    "targetRoleName": "Backend Developer",
    "portfolioSlug": "nguyen-van-a",
    "portfolioUrl": "https://frontend.com/portfolio/nguyen-van-a",
    "githubUsername": "nguyenvana",
    "githubUrl": "https://github.com/nguyenvana",
    "publishedAt": "2026-05-10T10:00:00Z"
  }
]
```

---

## 5.2 PORTFOLIO REVIEW SCREEN

### Mô tả
Màn hình review portfolio của sinh viên

### UI Components
- **Left Panel: Portfolio View**
  - Embedded portfolio (iframe hoặc render)
  - Student info summary
  - Target role
  - Skills list
  - Projects list

- **Right Panel: Review Form**
  - Overall Rating (1-5 stars)
  - Portfolio Quality Feedback (textarea)
  - Technical Skills Assessment (textarea)
  - Project Quality Feedback (textarea)
  - Recommendations for Improvement (textarea)
  - Job Readiness Assessment (dropdown):
    * Not Ready
    * Need Improvement
    * Ready for Internship
    * Ready for Junior Position
  - "Submit Review" button

- **GitHub Analysis Tab:**
  - List of repositories
  - Code quality indicators
  - Commit history
  - Best practices assessment

### User Actions & Flow

#### Action 1: Load Portfolio for Review
```
1. Mentor click "Review" button
2. Frontend navigate to review screen
3. Frontend gọi APIs:
```

**API Calls:**
```http
# Get portfolio
GET /api/portfolio/{slug}
# No auth needed (public)

# Get student profile for context
GET /api/mentor/students/{studentId}/profile
Authorization: Bearer {mentorToken}

# Get GitHub repos
GET /api/mentor/students/{studentId}/github
Authorization: Bearer {mentorToken}
```

#### Action 2: Submit Review
```
1. Mentor điền review form
2. Mentor click "Submit Review" button
3. Frontend gọi API:
```

**API Call:**
```http
POST /api/mentor/feedback
Authorization: Bearer {mentorToken}
Content-Type: application/json

{
  "studentId": "student-guid",
  "overallRating": 4,
  "portfolioQualityFeedback": "Portfolio is well-structured and professional. Good use of images and descriptions.",
  "technicalSkillsAssessment": "Strong backend skills demonstrated through projects. Good understanding of Spring Boot and PostgreSQL.",
  "projectQualityFeedback": "E-commerce project shows good architecture. Recommend adding more unit tests and documentation.",
  "recommendations": "1. Add CI/CD pipeline to projects\n2. Improve README with setup instructions\n3. Add more comments in code\n4. Consider adding a microservices project",
  "jobReadinessLevel": "ReadyForInternship"
}
```

**Backend Process (CẦN IMPLEMENT):**
```csharp
// MentorController.CreateFeedback()
1. Verify mentor role
2. Get mentorId from JWT
3. Validate input:
   - StudentId exists
   - Comment not empty
   - Rating 1-5 if provided
4. Create MentorFeedback:
   - Id = Guid.NewGuid()
   - MentorId = mentorId
   - StudentId = request.StudentId
   - PortfolioId = request.PortfolioId
   - GithubRepositoryId = request.GithubRepositoryId
   - Comment = request.Comment
   - Rating = request.Rating
   - CreatedAt = now
   - UpdatedAt = now
5. Save to database
6. Optional: Send notification to student
7. Return created feedback
```

**Database Operations:**
```sql
-- Insert mentor feedback
INSERT INTO "mentor_feedbacks" (
  "Id", "MentorId", "StudentId", "PortfolioId",
  "GithubRepositoryId", "Comment", "Rating",
  "CreatedAt", "UpdatedAt"
) VALUES (
  @id, @mentorId, @studentId, @portfolioId,
  @githubRepositoryId, @comment, @rating,
  @now, @now
);
```

**Response:**
```json
{
  "id": "feedback-guid",
  "mentorId": "mentor-guid",
  "mentorName": "John Doe",
  "studentId": "student-guid",
  "portfolioId": "portfolio-guid",
  "githubRepositoryId": "repo-guid",
  "comment": "Portfolio is well-structured...",
  "rating": 4,
  "createdAt": "2026-05-16T14:35:00Z"
}
```

**Frontend Action:**
```
4. Hiển thị success message
5. Remove student from review queue
6. Navigate back to dashboard
```

### Database Tables Involved
- **MentorFeedback**: Store mentor reviews
- **Portfolios**: View student portfolios
- **GithubRepositories**: Analyze code
- **Users**: Get mentor and student info

---


# 6. DATABASE SCHEMA CHI TIẾT

## 6.1 TỔNG QUAN DATABASE

### Database Engine
- **PostgreSQL** (version 14+)
- **ORM:** Entity Framework Core
- **Migration:** EF Core Migrations

### Naming Conventions
- Table names: snake_case theo `ToTable(...)` trong `AppDbContext` (e.g., `users`, `student_profiles`, `learning_resources`)
- Column names: PascalCase theo property C# / EF Core mặc định (e.g., `Id`, `FullName`, `CreatedAt`)
- Primary keys: "Id" (Guid)
- Foreign keys: "{EntityName}Id" (e.g., "UserId", "SkillId")
- Timestamps: "CreatedAt", "UpdatedAt"

---

## 6.2 CORE TABLES

### Table: `users`
**Mô tả:** Lưu thông tin người dùng của hệ thống

```sql
CREATE TABLE "users" (
    "Id" UUID PRIMARY KEY,
    "Username" VARCHAR(100) NULL,
    "Email" VARCHAR(256) NOT NULL UNIQUE,
    "FullName" VARCHAR(200) NOT NULL,
    "AvatarUrl" VARCHAR(1024) NULL,
    "GoogleSubject" VARCHAR(128) NULL UNIQUE,
    "PasswordHash" VARCHAR(512) NULL,
    "IsEmailVerified" BOOLEAN NOT NULL DEFAULT false,
    "EmailVerificationOtpHash" VARCHAR(512) NULL,
    "EmailVerificationOtpExpiresAt" TIMESTAMPTZ NULL,
    "EmailVerifiedAt" TIMESTAMPTZ NULL,
    "Role" VARCHAR(50) NOT NULL DEFAULT 'Student',
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX "IX_users_Username" ON "users" ("Username");
CREATE UNIQUE INDEX "IX_users_Email" ON "users" ("Email");
CREATE UNIQUE INDEX "IX_users_GoogleSubject" ON "users" ("GoogleSubject");
CREATE INDEX "IX_users_Role" ON "users" ("Role");
CREATE INDEX "IX_users_IsActive" ON "users" ("IsActive");
```

**Columns:**
- `Id`: Primary key (Guid)
- `Username`: Tên đăng nhập (optional, có thể null)
- `Email`: Email (unique, required)
- `FullName`: Họ tên đầy đủ
- `AvatarUrl`: URL ảnh đại diện
- `GoogleSubject`: Google user ID (cho OAuth)
- `PasswordHash`: Mật khẩu đã hash (null nếu dùng Google)
- `IsEmailVerified`: Email đã xác thực chưa
- `EmailVerificationOtpHash`: OTP hash cho xác thực email
- `EmailVerificationOtpExpiresAt`: Thời gian hết hạn OTP
- `EmailVerifiedAt`: Thời điểm xác thực email
- `Role`: Vai trò (Student/Admin/AcademicCounselor/IndustryMentor)
- `IsActive`: Tài khoản còn hoạt động không
- `CreatedAt`: Ngày tạo
- `UpdatedAt`: Ngày cập nhật

**Relationships:**
- 1 User -> 1 StudentProfile
- 1 User -> N UserSkills
- 1 User -> N SkillGapReports
- 1 User -> N Roadmaps
- 1 User -> N MentorSessions
- 1 User -> 1 GithubConnection
- 1 User -> N GithubRepositories
- 1 User -> N Portfolios

---

### Table: `student_profiles`
**Mô tả:** Hồ sơ sinh viên

```sql
CREATE TABLE "student_profiles" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL UNIQUE,
    "School" VARCHAR(200) NULL,
    "Major" VARCHAR(200) NULL,
    "Year" INTEGER NULL CHECK ("Year" IS NULL OR ("Year" >= 1 AND "Year" <= 8)),
    "Gpa" DECIMAL(4,2) NULL CHECK ("Gpa" IS NULL OR "Gpa" >= 0),
    "TargetRoleId" UUID NULL,
    "GithubUsername" VARCHAR(100) NULL,
    "CareerGoal" VARCHAR(1000) NULL,
    "PreferredLearningHoursPerWeek" INTEGER NULL CHECK ("PreferredLearningHoursPerWeek" IS NULL OR "PreferredLearningHoursPerWeek" >= 0),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_StudentProfiles_Users" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_StudentProfiles_CareerRoles" 
        FOREIGN KEY ("TargetRoleId") REFERENCES "career_roles"("Id") ON DELETE SET NULL
);

CREATE UNIQUE INDEX "IX_student_profiles_UserId" ON "student_profiles" ("UserId");
CREATE INDEX "IX_student_profiles_TargetRoleId" ON "student_profiles" ("TargetRoleId");
CREATE INDEX "IX_student_profiles_GithubUsername" ON "student_profiles" ("GithubUsername");
```

**Columns:**
- `Id`: Primary key
- `UserId`: Foreign key to Users (unique, 1-1 relationship)
- `School`: Tên trường
- `Major`: Chuyên ngành
- `Year`: Năm học (1-5)
- `Gpa`: Điểm trung bình (0.0-4.0)
- `TargetRoleId`: Vai trò mục tiêu (FK to CareerRoles)
- `GithubUsername`: Tên GitHub
- `CareerGoal`: Mục tiêu nghề nghiệp
- `PreferredLearningHoursPerWeek`: Số giờ học/tuần

---

### Table: `skills`
**Mô tả:** Danh sách kỹ năng trong hệ thống

```sql
CREATE TABLE "skills" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(150) NOT NULL,
    "Category" VARCHAR(80) NOT NULL,
    "Description" VARCHAR(1000) NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "UQ_Skills_Name_Category" UNIQUE ("Name", "Category")
);

CREATE UNIQUE INDEX "IX_skills_Name_Category" ON "skills" ("Name", "Category");
CREATE INDEX "IX_skills_Category" ON "skills" ("Category");
CREATE INDEX "IX_skills_IsActive" ON "skills" ("IsActive");
```

**Columns:**
- `Id`: Primary key
- `Name`: Tên kỹ năng (e.g., "Java", "Spring Boot")
- `Category`: Danh mục (e.g., "Programming Language", "Framework")
- `Description`: Mô tả kỹ năng
- `IsActive`: Kỹ năng còn sử dụng không

**Unique Constraint:** (Name, Category) - Một kỹ năng không được trùng tên trong cùng category

---

### Table: `user_skills`
**Mô tả:** Kỹ năng của người dùng

```sql
CREATE TABLE "user_skills" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "SkillId" UUID NOT NULL,
    "Level" VARCHAR(30) NOT NULL,
    "EvidenceUrl" VARCHAR(1024) NULL,
    "EvidenceType" VARCHAR(50) NULL,
    "IsVerified" BOOLEAN NOT NULL DEFAULT false,
    "VerifiedByUserId" UUID NULL,
    "VerifiedAt" TIMESTAMPTZ NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_UserSkills_Users" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserSkills_Skills" 
        FOREIGN KEY ("SkillId") REFERENCES "skills"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_user_skills_users_VerifiedByUserId" 
        FOREIGN KEY ("VerifiedByUserId") REFERENCES "users"("Id") ON DELETE SET NULL,
    CONSTRAINT "UQ_UserSkills_User_Skill" UNIQUE ("UserId", "SkillId")
);

CREATE UNIQUE INDEX "IX_user_skills_UserId_SkillId" ON "user_skills" ("UserId", "SkillId");
CREATE INDEX "IX_user_skills_SkillId" ON "user_skills" ("SkillId");
CREATE INDEX "IX_user_skills_Level" ON "user_skills" ("Level");
CREATE INDEX "IX_user_skills_IsVerified" ON "user_skills" ("IsVerified");
```

**Columns:**
- `Id`: Primary key
- `UserId`: Foreign key to Users
- `SkillId`: Foreign key to Skills
- `Level`: Mức độ (Beginner/Intermediate/Advanced/Expert)
- `IsVerified`: Đã xác thực chưa (có chứng chỉ/project)
- `EvidenceUrl`: URL/object name minh chứng
- `EvidenceType`: MIME type hoặc loại minh chứng
- `VerifiedByUserId`: người xác thực kỹ năng (optional)
- `VerifiedAt`: thời điểm xác thực (optional)

**Unique Constraint:** (UserId, SkillId) - Một user không thể có 2 lần cùng 1 skill

---

### Table: `career_roles`
**Mô tả:** Các vai trò nghề nghiệp

```sql
CREATE TABLE "career_roles" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(150) NOT NULL UNIQUE,
    "Description" VARCHAR(2000) NULL,
    "Level" VARCHAR(50) NULL, -- Intern, Junior, Mid, Senior
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX "IX_career_roles_Name" ON "career_roles" ("Name");
CREATE INDEX "IX_career_roles_IsActive" ON "career_roles" ("IsActive");
```

**Columns:**
- `Id`: Primary key
- `Name`: Tên vai trò (e.g., "Backend Developer", "DevOps Engineer")
- `Description`: Mô tả vai trò
- `Level`: Cấp độ (Intern/Junior/Mid/Senior)
- `IsActive`: Vai trò còn sử dụng không

---

### Table: `role_skill_requirements`
**Mô tả:** Yêu cầu kỹ năng cho từng vai trò

```sql
CREATE TABLE "role_skill_requirements" (
    "Id" UUID PRIMARY KEY,
    "CareerRoleId" UUID NOT NULL,
    "SkillId" UUID NOT NULL,
    "RequiredLevel" VARCHAR(30) NOT NULL,
    "Priority" INTEGER NOT NULL CHECK ("Priority" >= 1 AND "Priority" <= 5),
    "Weight" DECIMAL(5,2) NOT NULL DEFAULT 1 CHECK ("Weight" > 0),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_RoleSkillRequirements_CareerRoles" 
        FOREIGN KEY ("CareerRoleId") REFERENCES "career_roles"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RoleSkillRequirements_Skills" 
        FOREIGN KEY ("SkillId") REFERENCES "skills"("Id") ON DELETE RESTRICT,
    CONSTRAINT "UQ_RoleSkillRequirements_Role_Skill" UNIQUE ("CareerRoleId", "SkillId")
);

CREATE UNIQUE INDEX "IX_role_skill_requirements_CareerRoleId_SkillId" ON "role_skill_requirements" ("CareerRoleId", "SkillId");
CREATE INDEX "IX_role_skill_requirements_SkillId" ON "role_skill_requirements" ("SkillId");
CREATE INDEX "IX_role_skill_requirements_Priority" ON "role_skill_requirements" ("Priority");
```

**Columns:**
- `Id`: Primary key
- `CareerRoleId`: Foreign key to CareerRoles
- `SkillId`: Foreign key to Skills
- `RequiredLevel`: Mức độ yêu cầu
- `Priority`: Ước ưu tiên (1-5, 5 là cao nhất)
- `Weight`: Trọng số cho tính match score

**Unique Constraint:** (CareerRoleId, SkillId) - Một role không thể yêu cầu 2 lần cùng 1 skill

---

## 6.3 SKILL GAP ANALYSIS TABLES

### Table: `skill_gap_reports`
**Mô tả:** Báo cáo phân tích khoảng cách kỹ năng

```sql
CREATE TABLE "skill_gap_reports" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "CareerRoleId" UUID NOT NULL,
    "MatchScore" DECIMAL(5,2) NOT NULL CHECK ("MatchScore" >= 0 AND "MatchScore" <= 100),
    "Summary" VARCHAR(4000) NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_SkillGapReports_Users" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SkillGapReports_CareerRoles" 
        FOREIGN KEY ("CareerRoleId") REFERENCES "career_roles"("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_skill_gap_reports_UserId_CreatedAt" ON "skill_gap_reports" ("UserId", "CreatedAt" DESC);
CREATE INDEX "IX_skill_gap_reports_CareerRoleId" ON "skill_gap_reports" ("CareerRoleId");
```

**Columns:**
- `Id`: Primary key
- `UserId`: Foreign key to Users
- `CareerRoleId`: Vai trò được phân tích
- `MatchScore`: Điểm phù hợp (0-100%)
- `Summary`: Tóm tắt kết quả

---

### Table: `skill_gap_report_items`
**Mô tả:** Chi tiết từng kỹ năng trong báo cáo

```sql
CREATE TABLE "skill_gap_report_items" (
    "Id" UUID PRIMARY KEY,
    "SkillGapReportId" UUID NOT NULL,
    "SkillId" UUID NOT NULL,
    "CurrentLevel" VARCHAR(30) NULL,
    "RequiredLevel" VARCHAR(30) NOT NULL,
    "Status" VARCHAR(30) NOT NULL,
    "Priority" INTEGER NOT NULL CHECK ("Priority" >= 1 AND "Priority" <= 5),
    "Recommendation" VARCHAR(2000) NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_SkillGapReportItems_SkillGapReports" 
        FOREIGN KEY ("SkillGapReportId") REFERENCES "skill_gap_reports"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SkillGapReportItems_Skills" 
        FOREIGN KEY ("SkillId") REFERENCES "skills"("Id") ON DELETE RESTRICT
);

CREATE UNIQUE INDEX "IX_skill_gap_report_items_SkillGapReportId_SkillId" ON "skill_gap_report_items" ("SkillGapReportId", "SkillId");
CREATE INDEX "IX_skill_gap_report_items_SkillId" ON "skill_gap_report_items" ("SkillId");
CREATE INDEX "IX_skill_gap_report_items_Status" ON "skill_gap_report_items" ("Status");
CREATE INDEX "IX_skill_gap_report_items_Priority" ON "skill_gap_report_items" ("Priority");
```

**Columns:**
- `Id`: Primary key
- `SkillGapReportId`: Foreign key to SkillGapReports
- `SkillId`: Foreign key to Skills
- `CurrentLevel`: Mức độ hiện tại (null nếu Missing)
- `RequiredLevel`: Mức độ yêu cầu
- `Status`: Trạng thái (Matched/Weak/Missing/NotVerified)
- `Priority`: Ước ưu tiên (từ RoleSkillRequirement)
- `Recommendation`: Khuyến nghị cải thiện

---

## 6.4 ROADMAP TABLES

### Table: `roadmaps`
**Mô tả:** Lộ trình học tập

```sql
CREATE TABLE "roadmaps" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "CareerRoleId" UUID NOT NULL,
    "SkillGapReportId" UUID NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(2000) NULL,
    "Status" VARCHAR(30) NOT NULL DEFAULT 'Draft',
    "Progress" DECIMAL(5,2) NOT NULL DEFAULT 0 CHECK ("Progress" >= 0 AND "Progress" <= 100),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_Roadmaps_Users" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Roadmaps_CareerRoles" 
        FOREIGN KEY ("CareerRoleId") REFERENCES "career_roles"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Roadmaps_SkillGapReports" 
        FOREIGN KEY ("SkillGapReportId") REFERENCES "skill_gap_reports"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_roadmaps_UserId_Status" ON "roadmaps" ("UserId", "Status");
CREATE INDEX "IX_roadmaps_CareerRoleId" ON "roadmaps" ("CareerRoleId");
```

**Columns:**
- `Id`: Primary key
- `UserId`: Foreign key to Users
- `CareerRoleId`: Vai trò mục tiêu
- `SkillGapReportId`: Báo cáo skill gap (nếu roadmap tạo từ gap)
- `Title`: Tiêu đề roadmap
- `Description`: Mô tả
- `Status`: Trạng thái (Active/Completed/Archived)
- `Progress`: Tiến độ (0-100%)

---

### Table: `roadmap_nodes`
**Mô tả:** Các bước trong lộ trình

```sql
CREATE TABLE "roadmap_nodes" (
    "Id" UUID PRIMARY KEY,
    "RoadmapId" UUID NOT NULL,
    "SkillId" UUID NULL,
    "LearningResourceId" UUID NULL,
    "PrerequisiteNodeId" UUID NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(2000) NULL,
    "NodeType" VARCHAR(30) NOT NULL,
    "Status" VARCHAR(30) NOT NULL DEFAULT 'NotStarted',
    "OrderIndex" INTEGER NOT NULL,
    "EstimatedHours" INTEGER NULL CHECK ("EstimatedHours" IS NULL OR "EstimatedHours" >= 0),
    "Priority" INTEGER NOT NULL CHECK ("Priority" >= 1 AND "Priority" <= 5),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_RoadmapNodes_Roadmaps" 
        FOREIGN KEY ("RoadmapId") REFERENCES "roadmaps"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_RoadmapNodes_Skills" 
        FOREIGN KEY ("SkillId") REFERENCES "skills"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_RoadmapNodes_LearningResources" 
        FOREIGN KEY ("LearningResourceId") REFERENCES "learning_resources"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_RoadmapNodes_PrerequisiteNode" 
        FOREIGN KEY ("PrerequisiteNodeId") REFERENCES "roadmap_nodes"("Id") ON DELETE SET NULL
);

CREATE UNIQUE INDEX "IX_roadmap_nodes_RoadmapId_OrderIndex" ON "roadmap_nodes" ("RoadmapId", "OrderIndex");
CREATE INDEX "IX_roadmap_nodes_SkillId" ON "roadmap_nodes" ("SkillId");
CREATE INDEX "IX_roadmap_nodes_LearningResourceId" ON "roadmap_nodes" ("LearningResourceId");
CREATE INDEX "IX_roadmap_nodes_PrerequisiteNodeId" ON "roadmap_nodes" ("PrerequisiteNodeId");
CREATE INDEX "IX_roadmap_nodes_Status" ON "roadmap_nodes" ("Status");
```

**Columns:**
- `Id`: Primary key
- `RoadmapId`: Foreign key to Roadmaps
- `SkillId`: Kỹ năng liên quan (optional)
- `LearningResourceId`: Tài nguyên học tập (optional)
- `PrerequisiteNodeId`: Node tiên quyết (self-reference)
- `Title`: Tiêu đề bước học
- `Description`: Mô tả chi tiết
- `NodeType`: Loại node (Skill/Project/Reading/Practice/Assessment)
- `Status`: Trạng thái (NotStarted/InProgress/Completed)
- `OrderIndex`: Thứ tự trong roadmap
- `EstimatedHours`: Ước tính số giờ
- `Priority`: Ước ưu tiên

---

## 6.5 LEARNING RESOURCES TABLE

### Table: `learning_resources`
**Mô tả:** Tài nguyên học tập

```sql
CREATE TABLE "learning_resources" (
    "Id" UUID PRIMARY KEY,
    "SkillId" UUID NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Url" VARCHAR(1024) NOT NULL,
    "StorageObjectName" VARCHAR(1024) NULL,
    "ContentType" VARCHAR(100) NULL,
    "FileSize" BIGINT NULL,
    "ResourceType" VARCHAR(50) NOT NULL, -- Video, Article, Course, Book, Tutorial
    "Difficulty" VARCHAR(50) NULL, -- Beginner, Intermediate, Advanced
    "EstimatedHours" INTEGER NULL CHECK ("EstimatedHours" IS NULL OR "EstimatedHours" >= 0),
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_LearningResources_Skills" 
        FOREIGN KEY ("SkillId") REFERENCES "skills"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_learning_resources_SkillId" ON "learning_resources" ("SkillId");
CREATE INDEX "IX_learning_resources_IsActive" ON "learning_resources" ("IsActive");
```

**Columns:**
- `Id`: Primary key
- `SkillId`: Kỹ năng liên quan (optional)
- `Title`: Tiêu đề tài nguyên
- `Url`: Link tài nguyên. Nếu là file upload thì lưu route download `/api/storage/learning-resources/{resourceId}/download`
- `StorageObjectName`: Object name trong Google Cloud Storage, null nếu resource là link ngoài
- `ContentType`: MIME type của file upload, ví dụ `application/pdf`, null nếu resource là link ngoài
- `FileSize`: Kích thước file upload theo byte, null nếu resource là link ngoài
- `ResourceType`: Loại (Video/Article/Course/Book/Tutorial)
- `Difficulty`: Độ khó
- `EstimatedHours`: Ước tính thời gian học
- `IsActive`: Còn sử dụng không

**Ghi chú migration:**
- Migration `20260518031004_AddLearningResourceFiles` thêm 3 cột nullable:
  - `StorageObjectName` (`character varying(1024)`)
  - `ContentType` (`character varying(100)`)
  - `FileSize` (`bigint`)
- Các learning resource seed/link cũ vẫn hoạt động vì 3 cột mới cho phép null.
- `SourceType` không lưu thành cột riêng; API suy ra:
  - `StorageObjectName == null` => `SourceType = "Link"`
  - `StorageObjectName != null` => `SourceType = "File"`

**Supported API Response Fields:**
```json
{
  "id": "resource-guid",
  "skillId": "skill-guid",
  "skillName": "Spring Boot",
  "title": "Spring Boot Core PDF",
  "url": "/api/storage/learning-resources/resource-guid/download",
  "sourceType": "File",
  "contentType": "application/pdf",
  "fileSize": 2485120,
  "resourceType": "Documentation",
  "difficulty": "Intermediate",
  "estimatedHours": 6,
  "isActive": true,
  "createdAt": "2026-05-18T03:10:04Z",
  "updatedAt": "2026-05-18T03:10:04Z"
}
```

---


## 6.6 GITHUB INTEGRATION TABLES

### Table: `github_connections`
**Mô tả:** Kết nối GitHub OAuth

```sql
CREATE TABLE "github_connections" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL UNIQUE,
    "GithubUserId" BIGINT NULL,
    "GithubUsername" VARCHAR(100) NOT NULL,
    "AccessToken" TEXT NOT NULL,
    "TokenType" VARCHAR(50) NOT NULL,
    "Scope" TEXT NULL,
    "ConnectedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_github_connections_users_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_github_connections_UserId" ON "github_connections" ("UserId");
CREATE INDEX "IX_github_connections_GithubUsername" ON "github_connections" ("GithubUsername");
```

**Columns:**
- `Id`: Primary key
- `UserId`: Foreign key to Users (unique, 1-1)
- `GithubUserId`: GitHub user ID
- `GithubUsername`: Tên GitHub
- `AccessToken`: OAuth access token
- `TokenType`: Loại token (thường là "bearer")
- `Scope`: Quyền truy cập
- `ConnectedAt`: Thời điểm kết nối lần đầu
- `UpdatedAt`: Thời điểm cập nhật

---

### Table: `github_oauth_states`
**Mô tả:** Lưu trạng thái OAuth tạm thời

```sql
CREATE TABLE "github_oauth_states" (
    "State" VARCHAR(128) PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "ReturnUrl" VARCHAR(2048) NULL,
    "ExpiresAt" TIMESTAMPTZ NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_github_oauth_states_users_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_github_oauth_states_UserId" ON "github_oauth_states" ("UserId");
CREATE INDEX "IX_github_oauth_states_ExpiresAt" ON "github_oauth_states" ("ExpiresAt");
```

**Columns:**
- `State`: Random string (primary key)
- `UserId`: Foreign key to Users
- `ReturnUrl`: URL để redirect sau khi OAuth
- `ExpiresAt`: Thời gian hết hạn (10 phút)
- `CreatedAt`: Thời điểm tạo

**Note:** Bảng này chỉ lưu tạm, cần cleanup thường xuyên

---

### Table: `github_repositories`
**Mô tả:** Danh sách repositories của user

```sql
CREATE TABLE "github_repositories" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "RepoName" VARCHAR(200) NOT NULL,
    "RepoUrl" VARCHAR(1024) NOT NULL,
    "Description" VARCHAR(2000) NULL,
    "MainLanguage" VARCHAR(100) NULL,
    "ReadmeContent" TEXT NULL,
    "AiSummary" TEXT NULL,
    "TechStackJson" JSONB NULL,
    "QualityScore" NUMERIC(5,2) NULL CHECK ("QualityScore" IS NULL OR ("QualityScore" >= 0 AND "QualityScore" <= 100)),
    "LastSyncedAt" TIMESTAMPTZ NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_github_repositories_users_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_github_repositories_User_RepoUrl" UNIQUE ("UserId", "RepoUrl")
);

CREATE UNIQUE INDEX "IX_github_repositories_UserId_RepoUrl" ON "github_repositories" ("UserId", "RepoUrl");
CREATE INDEX "IX_github_repositories_MainLanguage" ON "github_repositories" ("MainLanguage");
```

**Columns:**
- `Id`: Primary key
- `UserId`: Foreign key to Users
- `RepoName`: Tên repository
- `RepoUrl`: URL repository
- `Description`: Mô tả
- `MainLanguage`: Ngôn ngữ chính
- `ReadmeContent`: Nội dung README
- `TechStackJson`: Tech stack (JSON format)
- `AiSummary`: Tóm tắt bởi AI
- `QualityScore`: Điểm chất lượng (0-100)
- `LastSyncedAt`: Lần cuối đồng bộ

---

### Table: `github_repository_skills`
**Mô tả:** Mapping giữa repository và skills

```sql
CREATE TABLE "github_repository_skills" (
    "Id" UUID PRIMARY KEY,
    "GithubRepositoryId" UUID NOT NULL,
    "SkillId" UUID NOT NULL,
    "ConfidenceScore" NUMERIC(5,2) NULL CHECK ("ConfidenceScore" IS NULL OR ("ConfidenceScore" >= 0 AND "ConfidenceScore" <= 100)),
    "EvidenceText" VARCHAR(2000) NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_GithubRepositorySkills_GithubRepositories" 
        FOREIGN KEY ("GithubRepositoryId") REFERENCES "github_repositories"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_GithubRepositorySkills_Skills" 
        FOREIGN KEY ("SkillId") REFERENCES "skills"("Id") ON DELETE RESTRICT,
    CONSTRAINT "UQ_GithubRepositorySkills_Repo_Skill" UNIQUE ("GithubRepositoryId", "SkillId")
);

CREATE UNIQUE INDEX "IX_github_repository_skills_GithubRepositoryId_SkillId" ON "github_repository_skills" ("GithubRepositoryId", "SkillId");
CREATE INDEX "IX_github_repository_skills_SkillId" ON "github_repository_skills" ("SkillId");
```

**Columns:**
- `Id`: Primary key
- `GithubRepositoryId`: Foreign key to GithubRepositories
- `SkillId`: Foreign key to Skills
- `ConfidenceScore`: Độ tin cậy khi AI map skill
- `EvidenceText`: Bằng chứng từ README/code metadata

---

## 6.7 PORTFOLIO TABLES

### Table: `portfolios`
**Mô tả:** Portfolio công khai của sinh viên

```sql
CREATE TABLE "portfolios" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Slug" VARCHAR(150) NOT NULL UNIQUE,
    "Title" VARCHAR(200) NOT NULL,
    "Bio" VARCHAR(2000) NULL,
    "Theme" VARCHAR(80) NOT NULL DEFAULT 'Default',
    "IsPublished" BOOLEAN NOT NULL DEFAULT false,
    "PublishedAt" TIMESTAMPTZ NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_Portfolios_Users" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_portfolios_Slug" ON "portfolios" ("Slug");
CREATE INDEX "IX_portfolios_UserId_IsPublished" ON "portfolios" ("UserId", "IsPublished");
```

**Columns:**
- `Id`: Primary key
- `UserId`: Foreign key to Users
- `Slug`: URL slug (unique, dùng cho public URL)
- `Title`: Tiêu đề portfolio
- `Bio`: Giới thiệu bản thân
- `Theme`: Giao diện (Default/Modern/Minimal/Dark)
- `IsPublished`: Đã publish chưa
- `PublishedAt`: Thời điểm publish

---

### Table: `portfolio_projects`
**Mô tả:** Các project trong portfolio

```sql
CREATE TABLE "portfolio_projects" (
    "Id" UUID PRIMARY KEY,
    "PortfolioId" UUID NOT NULL,
    "GithubRepositoryId" UUID NULL,
    "Title" VARCHAR(200) NOT NULL,
    "Description" VARCHAR(3000) NULL,
    "TechStackJson" JSONB NULL,
    "ImageUrl" VARCHAR(1024) NULL,
    "DemoUrl" VARCHAR(1024) NULL,
    "SourceUrl" VARCHAR(1024) NULL,
    "OrderIndex" INTEGER NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_PortfolioProjects_Portfolios" 
        FOREIGN KEY ("PortfolioId") REFERENCES "portfolios"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PortfolioProjects_GithubRepositories" 
        FOREIGN KEY ("GithubRepositoryId") REFERENCES "github_repositories"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_portfolio_projects_PortfolioId_OrderIndex" ON "portfolio_projects" ("PortfolioId", "OrderIndex");
CREATE INDEX "IX_portfolio_projects_GithubRepositoryId" ON "portfolio_projects" ("GithubRepositoryId");
```

**Columns:**
- `Id`: Primary key
- `PortfolioId`: Foreign key to Portfolios
- `GithubRepositoryId`: Liên kết với GitHub repo (optional)
- `Title`: Tên project
- `Description`: Mô tả project
- `TechStackJson`: Công nghệ sử dụng (JSON)
- `ImageUrl`: Ảnh project
- `DemoUrl`: Link demo
- `SourceUrl`: Link source code
- `OrderIndex`: Thứ tự hiển thị

---

## 6.8 AI MENTOR TABLE

### Table: `mentor_sessions`
**Mô tả:** Lịch sử chat với AI Mentor

```sql
CREATE TABLE "mentor_sessions" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Question" TEXT NOT NULL,
    "Answer" TEXT NOT NULL,
    "ContextJson" JSONB NULL,
    "Model" VARCHAR(100) NULL,
    "TokensUsed" INTEGER NULL CHECK ("TokensUsed" IS NULL OR "TokensUsed" >= 0),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_MentorSessions_Users" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_mentor_sessions_UserId_CreatedAt" ON "mentor_sessions" ("UserId", "CreatedAt" DESC);
```

**Columns:**
- `Id`: Primary key
- `UserId`: Foreign key to Users
- `Question`: Câu hỏi của user
- `Answer`: Trả lời của AI
- `ContextJson`: Context đã gửi cho AI (JSON)
- `Model`: Model AI đã dùng (e.g., "gemini-pro")
- `TokensUsed`: Số token tiêu thụ
- `CreatedAt`: Thời điểm chat

---

## 6.9 FEEDBACK TABLES

### Table: `counselor_feedbacks`
**Mô tả:** Feedback từ Academic Counselor

```sql
CREATE TABLE "counselor_feedbacks" (
    "Id" UUID PRIMARY KEY,
    "CounselorId" UUID NOT NULL,
    "StudentId" UUID NOT NULL,
    "RoadmapId" UUID NULL,
    "SkillGapReportId" UUID NULL,
    "Comment" TEXT NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_counselor_feedbacks_users_CounselorId" 
        FOREIGN KEY ("CounselorId") REFERENCES "users"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_counselor_feedbacks_users_StudentId" 
        FOREIGN KEY ("StudentId") REFERENCES "users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_counselor_feedbacks_roadmaps_RoadmapId" 
        FOREIGN KEY ("RoadmapId") REFERENCES "roadmaps"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_counselor_feedbacks_skill_gap_reports_SkillGapReportId" 
        FOREIGN KEY ("SkillGapReportId") REFERENCES "skill_gap_reports"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_counselor_feedbacks_CounselorId" ON "counselor_feedbacks" ("CounselorId");
CREATE INDEX "IX_counselor_feedbacks_StudentId" ON "counselor_feedbacks" ("StudentId");
CREATE INDEX "IX_counselor_feedbacks_RoadmapId" ON "counselor_feedbacks" ("RoadmapId");
CREATE INDEX "IX_counselor_feedbacks_SkillGapReportId" ON "counselor_feedbacks" ("SkillGapReportId");
```

**Columns:**
- `Id`: Primary key
- `CounselorId`: Foreign key to Users (counselor)
- `StudentId`: Foreign key to Users (student)
- `RoadmapId`: Roadmap liên quan (optional)
- `SkillGapReportId`: Skill gap report liên quan (optional)
- `Comment`: Nội dung feedback

---

### Table: `mentor_feedbacks`
**Mô tả:** Feedback từ Industry Mentor

```sql
CREATE TABLE "mentor_feedbacks" (
    "Id" UUID PRIMARY KEY,
    "MentorId" UUID NOT NULL,
    "StudentId" UUID NOT NULL,
    "PortfolioId" UUID NULL,
    "GithubRepositoryId" UUID NULL,
    "Comment" TEXT NOT NULL,
    "Rating" INTEGER NULL CHECK ("Rating" IS NULL OR ("Rating" >= 1 AND "Rating" <= 5)),
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_mentor_feedbacks_users_MentorId" 
        FOREIGN KEY ("MentorId") REFERENCES "users"("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_mentor_feedbacks_users_StudentId" 
        FOREIGN KEY ("StudentId") REFERENCES "users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_mentor_feedbacks_portfolios_PortfolioId" 
        FOREIGN KEY ("PortfolioId") REFERENCES "portfolios"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_mentor_feedbacks_github_repositories_GithubRepositoryId" 
        FOREIGN KEY ("GithubRepositoryId") REFERENCES "github_repositories"("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_mentor_feedbacks_MentorId" ON "mentor_feedbacks" ("MentorId");
CREATE INDEX "IX_mentor_feedbacks_StudentId" ON "mentor_feedbacks" ("StudentId");
CREATE INDEX "IX_mentor_feedbacks_PortfolioId" ON "mentor_feedbacks" ("PortfolioId");
CREATE INDEX "IX_mentor_feedbacks_GithubRepositoryId" ON "mentor_feedbacks" ("GithubRepositoryId");
```

**Columns:**
- `Id`: Primary key
- `MentorId`: Foreign key to Users (mentor)
- `StudentId`: Foreign key to Users (student)
- `PortfolioId`: Portfolio được review (optional)
- `GithubRepositoryId`: Repository được review (optional)
- `Comment`: Nội dung feedback
- `Rating`: Đánh giá 1-5 (optional)

---

## 6.10 PAYMENT TABLES

### Table: `subscription_plans`
**Mô tả:** Các gói dịch vụ

```sql
CREATE TABLE "subscription_plans" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL UNIQUE,
    "Description" VARCHAR(2000) NULL,
    "Price" NUMERIC(18,2) NOT NULL DEFAULT 0,
    "Currency" VARCHAR(10) NOT NULL,
    "BillingCycle" VARCHAR(30) NOT NULL,
    "FeaturesJson" JSONB NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX "IX_subscription_plans_Name" ON "subscription_plans" ("Name");
CREATE INDEX "IX_subscription_plans_IsActive" ON "subscription_plans" ("IsActive");
```

---

### Table: `subscriptions`
**Mô tả:** Đăng ký gói dịch vụ của user

```sql
CREATE TABLE "subscriptions" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "PlanId" UUID NOT NULL,
    "Status" VARCHAR(30) NOT NULL,
    "StartedAt" TIMESTAMPTZ NULL,
    "ExpiredAt" TIMESTAMPTZ NULL,
    "CancelledAt" TIMESTAMPTZ NULL,
    "Provider" VARCHAR(50) NULL,
    "ProviderSubscriptionId" VARCHAR(200) NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_subscriptions_users_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_subscriptions_subscription_plans_PlanId" 
        FOREIGN KEY ("PlanId") REFERENCES "subscription_plans"("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_subscriptions_UserId_Status" ON "subscriptions" ("UserId", "Status");
CREATE INDEX "IX_subscriptions_ExpiredAt" ON "subscriptions" ("ExpiredAt");
CREATE INDEX "IX_subscriptions_ProviderSubscriptionId" ON "subscriptions" ("ProviderSubscriptionId");
```

---

### Table: `payment_transactions`
**Mô tả:** Giao dịch thanh toán

```sql
CREATE TABLE "payment_transactions" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "SubscriptionId" UUID NULL,
    "PlanId" UUID NOT NULL,
    "Amount" NUMERIC(18,2) NOT NULL,
    "Currency" VARCHAR(10) NOT NULL,
    "Status" VARCHAR(30) NOT NULL,
    "Provider" VARCHAR(50) NOT NULL,
    "ProviderTransactionId" VARCHAR(200) NULL UNIQUE,
    "CheckoutUrl" VARCHAR(2048) NULL,
    "PaidAt" TIMESTAMPTZ NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_payment_transactions_users_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_payment_transactions_subscriptions_SubscriptionId" 
        FOREIGN KEY ("SubscriptionId") REFERENCES "subscriptions"("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_payment_transactions_subscription_plans_PlanId" 
        FOREIGN KEY ("PlanId") REFERENCES "subscription_plans"("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_payment_transactions_UserId_CreatedAt" ON "payment_transactions" ("UserId", "CreatedAt" DESC);
CREATE INDEX "IX_payment_transactions_SubscriptionId" ON "payment_transactions" ("SubscriptionId");
CREATE INDEX "IX_payment_transactions_PlanId" ON "payment_transactions" ("PlanId");
CREATE INDEX "IX_payment_transactions_Status" ON "payment_transactions" ("Status");
CREATE INDEX "IX_payment_transactions_Provider" ON "payment_transactions" ("Provider");
CREATE UNIQUE INDEX "IX_payment_transactions_ProviderTransactionId" ON "payment_transactions" ("ProviderTransactionId");
```

---

### Table: `payment_webhook_events`
**Mô tả:** Webhook events từ payment gateway

```sql
CREATE TABLE "payment_webhook_events" (
    "Id" UUID PRIMARY KEY,
    "Provider" VARCHAR(50) NOT NULL,
    "EventId" VARCHAR(200) NOT NULL,
    "EventType" VARCHAR(100) NOT NULL,
    "PayloadJson" JSONB NOT NULL,
    "ProcessedAt" TIMESTAMPTZ NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "UQ_payment_webhook_events_Provider_EventId" UNIQUE ("Provider", "EventId")
);

CREATE UNIQUE INDEX "IX_payment_webhook_events_Provider_EventId" ON "payment_webhook_events" ("Provider", "EventId");
CREATE INDEX "IX_payment_webhook_events_EventType" ON "payment_webhook_events" ("EventType");
```

---

### Table: `invoices`
**Mô tả:** Hóa đơn phát sinh từ giao dịch thanh toán

```sql
CREATE TABLE "invoices" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "PaymentTransactionId" UUID NOT NULL UNIQUE,
    "InvoiceNumber" VARCHAR(100) NOT NULL UNIQUE,
    "Amount" NUMERIC(18,2) NOT NULL,
    "Currency" VARCHAR(10) NOT NULL,
    "IssuedAt" TIMESTAMPTZ NOT NULL,
    "PdfUrl" VARCHAR(1024) NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    
    CONSTRAINT "FK_invoices_users_UserId" 
        FOREIGN KEY ("UserId") REFERENCES "users"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_invoices_payment_transactions_PaymentTransactionId" 
        FOREIGN KEY ("PaymentTransactionId") REFERENCES "payment_transactions"("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_invoices_UserId" ON "invoices" ("UserId");
CREATE UNIQUE INDEX "IX_invoices_PaymentTransactionId" ON "invoices" ("PaymentTransactionId");
CREATE UNIQUE INDEX "IX_invoices_InvoiceNumber" ON "invoices" ("InvoiceNumber");
```

---

### Table: `coupons`
**Mô tả:** Mã giảm giá

```sql
CREATE TABLE "coupons" (
    "Id" UUID PRIMARY KEY,
    "Code" VARCHAR(80) NOT NULL UNIQUE,
    "DiscountType" VARCHAR(30) NOT NULL,
    "DiscountValue" NUMERIC(18,2) NOT NULL,
    "MaxUsage" INTEGER NULL,
    "UsedCount" INTEGER NOT NULL DEFAULT 0,
    "ExpiredAt" TIMESTAMPTZ NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT true,
    "CreatedAt" TIMESTAMPTZ NOT NULL,
    "UpdatedAt" TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX "IX_coupons_Code" ON "coupons" ("Code");
CREATE INDEX "IX_coupons_ExpiredAt" ON "coupons" ("ExpiredAt");
CREATE INDEX "IX_coupons_IsActive" ON "coupons" ("IsActive");
```

---

## 6.11 DATABASE RELATIONSHIPS DIAGRAM

> Diagram này dùng tên entity để dễ đọc; schema chuẩn phía trên dùng tên bảng thật theo `AppDbContext` như `users`, `roadmap_nodes`, `learning_resources`.

```
Users (1) ----< (N) UserSkills >---- (1) Skills
  |                                      |
  | (1:1)                                |
  |                                      |
StudentProfiles                         |
  |                                      |
  | (N:1)                                |
  |                                      |
CareerRoles (1) ----< (N) RoleSkillRequirements
  |                                      |
  | (1:N)                                |
  |                                      |
SkillGapReports (1) ----< (N) SkillGapReportItems
  |                                      |
  | (1:N)                                |
  |                                      |
Roadmaps (1) ----< (N) RoadmapNodes >---- (1) LearningResources
  |                                      |
  |                                      |
Users (1) ----< (N) MentorSessions     |
  |                                      |
  | (1:1)                                |
  |                                      |
GithubConnections                       |
  |                                      |
Users (1) ----< (N) GithubRepositories >---- (N) GithubRepositorySkills
  |                                      |
  | (1:N)                                |
  |                                      |
Portfolios (1) ----< (N) PortfolioProjects
  |                                      |
Users (1) ----< (N) CounselorFeedback  |
  |                                      |
Users (1) ----< (N) MentorFeedback     |
```

---

## 6.12 DATABASE INDEXES SUMMARY

**Performance Indexes theo `AppDbContext`:**
- `users`: `Username`, `Email`, `GoogleSubject`, `Role`, `IsActive`
- `student_profiles`: `UserId`, `TargetRoleId`, `GithubUsername`
- `user_skills`: `(UserId, SkillId)`, `SkillId`, `Level`, `IsVerified`
- `skills`: `(Name, Category)`, `Category`, `IsActive`
- `role_skill_requirements`: `(CareerRoleId, SkillId)`, `SkillId`, `Priority`
- `skill_gap_reports`: `(UserId, CreatedAt DESC)`, `CareerRoleId`
- `skill_gap_report_items`: `(SkillGapReportId, SkillId)`, `SkillId`, `Status`, `Priority`
- `roadmaps`: `(UserId, Status)`, `CareerRoleId`
- `roadmap_nodes`: `(RoadmapId, OrderIndex)`, `SkillId`, `LearningResourceId`, `PrerequisiteNodeId`, `Status`
- `learning_resources`: `SkillId`, `IsActive`
- `github_repositories`: `(UserId, RepoUrl)`, `MainLanguage`
- `github_repository_skills`: `(GithubRepositoryId, SkillId)`, `SkillId`
- `github_connections`: `UserId`, `GithubUsername`
- `github_oauth_states`: `UserId`, `ExpiresAt`
- `portfolios`: `Slug`, `(UserId, IsPublished)`
- `portfolio_projects`: `(PortfolioId, OrderIndex)`, `GithubRepositoryId`
- `mentor_sessions`: `(UserId, CreatedAt DESC)`
- `subscriptions`: `(UserId, Status)`, `ExpiredAt`, `ProviderSubscriptionId`
- `payment_transactions`: `(UserId, CreatedAt DESC)`, `SubscriptionId`, `PlanId`, `Status`, `Provider`, `ProviderTransactionId`
- `payment_webhook_events`: `(Provider, EventId)`, `EventType`
- `invoices`: `UserId`, `PaymentTransactionId`, `InvoiceNumber`
- `coupons`: `Code`, `ExpiredAt`, `IsActive`
- `mentor_feedbacks`: `MentorId`, `StudentId`, `PortfolioId`, `GithubRepositoryId`
- `counselor_feedbacks`: `CounselorId`, `StudentId`, `RoadmapId`, `SkillGapReportId`

---

## 6.13 DATABASE CONSTRAINTS SUMMARY

**Foreign Key Constraints:**
- ON DELETE CASCADE: Xóa parent thì xóa children
- ON DELETE SET NULL: Xóa parent thì set children FK = NULL

**Unique Constraints:**
- `users.Email`, `users.Username`, `users.GoogleSubject`
- `student_profiles.UserId`
- `skills.(Name, Category)`
- `user_skills.(UserId, SkillId)`
- `career_roles.Name`
- `role_skill_requirements.(CareerRoleId, SkillId)`
- `skill_gap_report_items.(SkillGapReportId, SkillId)`
- `roadmap_nodes.(RoadmapId, OrderIndex)`
- `github_connections.UserId`
- `github_repositories.(UserId, RepoUrl)`
- `github_repository_skills.(GithubRepositoryId, SkillId)`
- `portfolios.Slug`
- `payment_transactions.ProviderTransactionId`
- `payment_webhook_events.(Provider, EventId)`
- `invoices.PaymentTransactionId`, `invoices.InvoiceNumber`
- `coupons.Code`

**Check Constraints:**
- `student_profiles.Year`: 1-8 hoặc null
- `student_profiles.Gpa`: >= 0 hoặc null
- `student_profiles.PreferredLearningHoursPerWeek`: >= 0 hoặc null
- `role_skill_requirements.Priority`: 1-5
- `role_skill_requirements.Weight`: > 0
- `skill_gap_reports.MatchScore`: 0-100
- `skill_gap_report_items.Priority`: 1-5
- `roadmaps.Progress`: 0-100
- `roadmap_nodes.EstimatedHours`: >= 0 hoặc null
- `roadmap_nodes.Priority`: 1-5
- `learning_resources.EstimatedHours`: >= 0 hoặc null
- `mentor_sessions.TokensUsed`: >= 0 hoặc null
- `github_repositories.QualityScore`: 0-100 hoặc null
- `github_repository_skills.ConfidenceScore`: 0-100 hoặc null
- `mentor_feedbacks.Rating`: 1-5 hoặc null

---

## KẾT LUẬN

Tài liệu này đã mô tả chi tiết:

✅ **26 bảng database** với schema đầy đủ theo `AppDbContext`
✅ **Tất cả màn hình** của 4 roles: Student, Admin, Counselor, Mentor
✅ **Từng thao tác user** với API calls, backend logic, database operations
✅ **Request/Response examples** cho mỗi API
✅ **Database relationships** và constraints
✅ **Indexes** cho performance

**File location:** `D:\PRJ\SWP\SWP-BE\docs\Chi-Tiet-Man-Hinh-Va-Database.md`

