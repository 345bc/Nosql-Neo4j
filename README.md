# Nosql-Neo4j

Ứng dụng học toán tứ giác dùng ASP.NET Core MVC (.NET 10), Razor Views và Neo4j. Nhánh Tuấn có core CSDL, đăng nhập/đổi mật khẩu, luyện tập/chấm điểm, lịch sử và thống kê cá nhân. Các luồng tra cứu/đồ thị và so sánh do Vỷ/Tín bàn giao riêng.

## Chạy local

1. Cài .NET SDK 10 và bật instance Neo4j.
2. Tạo database `nosql-neo4j` (repository hiện dùng tên này).
3. Copy `.env.example` thành `.env`, điền URI, username và mật khẩu riêng.
4. Chạy `dotnet restore`, `dotnet run --launch-profile https -- --setup-data` để tạo schema/seed và nâng cấp lượt cũ. Xem [hướng dẫn CSDL](docs/database-operations.md).
5. Tạo tài khoản riêng: `dotnet run --launch-profile https -- --create-user tuan`; nhập mật khẩu 12–256 ký tự hai lần. Lệnh không đặt lại mật khẩu tài khoản có sẵn.
6. Chạy `dotnet run --launch-profile https`, mở [đăng nhập](https://localhost:7277/Account/Login), rồi [luyện tập](https://localhost:7277/Practice). Development có nút chạy thử từ câu nháp.
7. Menu Lịch sử/Thống kê có bộ lọc chính thức/chạy thử, chủ đề và khoảng ngày; menu Đổi mật khẩu kết thúc các phiên cũ.

Trang Dev chỉ mở trong Development và đọc DRAFT: `/Dev/Shapes`, `/Dev/Graph`, `/Dev/Questions`. Database seed có 6 hình, 6 cạnh IS_A, 10 công thức, 60 câu (10/chủ đề), 2 đường hình vuông → tứ giác. Câu hỏi/kiến thức chưa được duyệt nguồn nên giữ DRAFT; không tự công bố để demo. Mỗi thành viên có database local riêng; Git lưu script, không lưu database đang chạy.

## Làm việc qua fork

Fork repository này về tài khoản cá nhân, clone fork và thêm upstream trỏ về repository chung. Làm trên nhánh chức năng, gửi pull request về `master` của repo chung. Đồng bộ upstream trước khi bắt đầu công việc mới; giải quyết xung đột trước khi gửi PR. Không force-push lên repo chung.

- Tuấn: CSDL, repository và luyện tập/chấm điểm.
- Vỷ: tra cứu, tìm kiếm, đồ thị.
- Tín: so sánh và hướng dẫn sử dụng.

Xem [CONTRIBUTE.md](CONTRIBUTE.md), [cấu trúc MVC](mvc.md) và [bàn giao core](docs/core-handoff.md). Không commit `.env`; chỉ chia sẻ `.env.example`.

## Đăng nhập và luyện tập

Tạo lượt 10 câu không trùng; chụp phiên bản/nội dung; không đưa đáp án lên đề; chấm nguyên tử, chống nộp trùng và kiểm tra chủ lượt/hạn 24 giờ. Lịch sử/thống kê chỉ tính lượt SUBMITTED, tách demo khỏi chính thức. Kiến trúc: Controller → Service → Repository → Neo4j, không REST API.

Xem [luồng luyện tập](docs/practice.md), [bàn giao phần Tuấn](docs/tuan-handoff.md), [checklist tích hợp](docs/integration-checklist.md) và [vận hành CSDL](docs/database-operations.md). Nội dung PUBLISHED phải có nguồn/người duyệt; quản trị/công bố bằng giao diện chưa nằm trong bản này.

## Kiểm thử local

Neo4j phải bật và đã chạy `--setup-data`. Dùng PowerShell 7 từ thư mục dự án:

```powershell
./scripts/Check-Local.ps1
```

Script build sang thư mục riêng, kiểm tra service, giao dịch Neo4j và HTTP cả Development/Production; dùng hai tài khoản tạm và tự dọn dữ liệu của chúng. Web kiểm thử dùng cổng 7281/7282 và được tắt sau khi chạy; không dừng web của bạn. Kết quả và giới hạn xem tài liệu bàn giao. Dump/restore offline cần diễn tập trên instance riêng.
