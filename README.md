# Nosql-Neo4j

Ứng dụng học kiến thức tứ giác với ASP.NET Core .NET 10 Razor Pages và Neo4j. Đây là bản core đọc dữ liệu, chưa phải ứng dụng đầy đủ.

## Chạy local

1. Cài .NET SDK 10 và bật instance Neo4j.
2. Tạo database `nosql-neo4j` (repository hiện dùng tên này).
3. Copy `.env.example` thành `.env`, điền URI, username và mật khẩu riêng.
4. Chọn database `nosql-neo4j` trong Neo4j Query, chạy lần lượt `Data/schema.cypher`, `Data/seed.cypher`, `Data/seed-knowledge.cypher`. Xem [hướng dẫn dữ liệu](Data/README.md).
5. Chạy `dotnet restore`, `dotnet build`, `dotnet run --launch-profile https`.
6. Mở https://localhost:7277/Dev/Shapes để kiểm tra danh sách/chi tiết và https://localhost:7277/Dev/Graph để kiểm tra phân loại.

Trang Dev chỉ mở trong Development và đọc DRAFT. Database chỉ chứa bộ seed này dự kiến có 6 hình, 6 cạnh IS_A, 10 công thức và 2 đường hình vuông → tứ giác. Chưa duyệt nguồn nên không tự chuyển dữ liệu sang PUBLISHED. Mỗi thành viên có database local riêng; Git lưu script, không lưu database đang chạy.

## Làm việc qua fork

Fork repository này về tài khoản cá nhân, clone fork và thêm upstream trỏ về repository chung. Làm trên nhánh chức năng, gửi pull request về `master` của repo chung. Đồng bộ upstream trước khi bắt đầu công việc mới; giải quyết xung đột trước khi gửi PR. Không force-push lên repo chung.

- Tuấn: CSDL, repository và luyện tập/chấm điểm.
- Vỷ: tra cứu, tìm kiếm, đồ thị.
- Tín: so sánh và hướng dẫn sử dụng.

Xem [CONTRIBUTE.md](CONTRIBUTE.md), [cấu trúc trang](pages.md) và [bàn giao core](docs/core-handoff.md). Không commit `.env`; chỉ chia sẻ `.env.example`. Chức năng xác thực, câu hỏi, luyện tập, quản trị và nguồn/người duyệt chưa hoàn tất.
