# Bàn giao phần Tuấn

Nhánh: `feature/tuan-database-practice`. ASP.NET Core MVC/Razor Views, không REST API. Phạm vi đối chiếu Jira NEO4 ngày 07/10/2026.

| Task | Kết quả trong checkout |
|---|---|
| NEO4-1/9 | Mô hình Shape/Formula/Question/Version/User/Attempt/AttemptItem/AuditEvent; constraints và quan hệ |
| NEO4-10 | Seed 6 hình, 6 cạnh, 10 công thức, 60 câu nháp; CLI setup/migration/verification |
| NEO4-11/12 | Core đọc danh sách/chi tiết/cặp hình/đường đi, metadata nguồn; Driver singleton, repository scoped, .env local |
| NEO4-15 | Đăng nhập/đổi mật khẩu, tạo 10 câu, nộp/chấm nguyên tử, kết quả có lời giải; lịch sử và thống kê cá nhân |
| NEO4-16 | Route/model/service/repository chốt trong mvc.md; templates, CONTRIBUTE và tài liệu bàn giao đồng bộ |
| NEO4-20 | Bộ kiểm thử tự động phần Tuấn và checklist tích hợp; chờ code Vỷ/Tín để kiểm thử đủ ba demo |
| NEO4-22 | README chạy local/fork/kiểm thử; hướng dẫn dữ liệu và backup/restore |

## Chạy và demo

1. Bật Neo4j, `.env` riêng; chọn database bằng `Neo4j__Database` (mặc định `nosql-neo4j`).
2. Dừng phiên web cũ trong Visual Studio trước nâng cấp. Chạy `dotnet run --launch-profile https -- --migrate`.
3. Khởi động `dotnet run --launch-profile https`; đăng nhập tài khoản đã cấp. Seed tạo `demo_tuan` / `DemoTuan@2026!` nếu chưa có, giữ nguyên mật khẩu nếu tài khoản đã tồn tại.
4. `/Practice`: chọn một trong sáu chủ đề ở phần chạy thử. Làm 10 câu, có thể bỏ trống, nộp rồi xem điểm/lời giải.
5. Bấm Lịch sử/Thống kê và chọn loại **Chạy thử**. Kết quả chính thức không tính lượt demo.
6. Trình bày CSDL qua `/Dev/Shapes`, `/Dev/Graph`, `/Dev/Questions` và các query trong Data. Production không mở các trang này.
7. Đổi mật khẩu bằng tài khoản kiểm thử riêng; sau đổi phải đăng nhập lại. Không dùng tài khoản nhóm dùng chung để thử thu hồi phiên.

## Bằng chứng và giới hạn

Bộ service và Neo4j/HTTP được chạy bằng `scripts/Check-Local.ps1`; kiểm tra giao dịch đồng thời trên Neo4j thật, quyền chủ lượt, chấm điểm, lịch sử/thống kê, mật khẩu và chặn demo ngoài Development. Kết quả cuối được ghi bên dưới sau lần chạy cuối. NuGet có thể cảnh báo NU1900 khi không truy cập được dịch vụ thông tin lỗ hổng; điều này không phải lỗi biên dịch, nhưng không coi audit dependency đã thành công.

Lần chạy cuối 07/10/2026: web build **0 warning/0 error**; **41 kiểm tra service + 45 kiểm tra Neo4j/HTTP = 86 PASS**; xác minh cấu trúc CSDL PASS, sáu chủ đề đều 10 phiên bản DRAFT. Script kết thúc mã 0, dọn tài khoản/lượt và hai tiến trình kiểm thử. Hai project kiểm thử có cảnh báo NU1900; audit dependency chưa xác minh được.

Không có `ShapesController`/`CompareController` trong checkout này, nên chưa có bằng chứng nghiệm thu ba luồng chung dù một số task Jira của người khác đang Done. Không tạo/duyệt nguồn thay giảng viên hoặc thành viên nhóm. 60 câu vẫn DRAFT; luồng chính thức sẵn sàng dùng các phiên bản PUBLISHED đủ metadata sau duyệt.

Quản trị tài khoản/công bố bằng giao diện, mustChangePassword, thống kê toàn hệ thống và kiểm thử tải không có trong phần đã triển khai. Dump/restore có quy trình nhưng chưa diễn tập; không tự dừng database của bạn để làm việc đó.

## Hợp đồng thay đổi

Giữ nguyên chữ ký IShapeRepository và constructor ShapeDetail; chỉ bổ sung metadata init. Schema mới tăng labels/constraints/indexes và các quan hệ snapshot, không xóa dữ liệu core. Đồng bộ schema bằng `--migrate` hoặc `--migrate-attempts` trước chạy bản mới. Các DTO mẫu luyện tập trong templates chỉ tham khảo, mã thật dùng Models/PracticeModels.cs và IPracticeService.

PR cần review phần hợp đồng dùng chung và checklist ba demo trước merge vào master; không đưa `.env`, dump hoặc dữ liệu kiểm thử cá nhân lên GitHub.
