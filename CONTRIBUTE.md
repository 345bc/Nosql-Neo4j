# Hướng dẫn đóng góp — Razor Pages

Dùng ASP.NET Core .NET 10 Razor Pages như repo hiện tại: mỗi trang gồm .cshtml và .cshtml.cs (PageModel). Razor Pages dùng hạ tầng MVC nhưng nhóm không tổ chức Controller + Views và không xây REST API.

Luồng: trình duyệt → PageModel → Service → Repository → Neo4j → ViewModel → Razor HTML. Form GET để tìm/lọc, POST để ghi. Không chạy Cypher trong Razor hay kết nối CSDL từ trình duyệt.

## Phân công

| Người | Trách nhiệm |
|---|---|
| Tuấn | CSDL Neo4j, seed, Cypher, kết nối; xác thực, luyện tập/chấm điểm; tích hợp và README |
| Vỷ | Trang tra cứu, tìm kiếm, chi tiết, đồ thị; PageModel/service tương ứng; SRS |
| Tín | Trang so sánh, PageModel/service tương ứng; hướng dẫn sử dụng |

Tín giữ phạm vi nhỏ hơn. Mỗi người làm trọn trang mình. Tuấn cung cấp repository/truy vấn chung. Task Done được tiếp nhận để bảo trì; lịch sử người thực hiện vẫn giữ trong Jira.

## Bắt đầu và làm song song

Cài .NET SDK 10; chạy `dotnet restore`, `dotnet build`, `dotnet run --launch-profile https`. URL hiện tại https://localhost:7277. Mã hiện tại mới là khung Razor Pages, chưa có Neo4j driver/xác thực/chức năng học tập. Thư mục hiện tại chưa có Git; quy trình PR áp dụng khi đưa vào repo chung.

1. Đọc [pages.md](pages.md), chốt route/handler/InputModel/ViewModel và interface service trong NEO4-16. Tuấn xác nhận schema/ID seed. Không ghi phê duyệt thay thành viên.
2. Vỷ/Tín dựng Razor và PageModel bằng service giả trả ViewModel C# trong môi trường phát triển, trong khi Tuấn làm repository thật. Không dùng API/JSON mock.
3. Giữ interface service khi chuyển sang Neo4j thật, tích hợp từng trang sớm. Mock không phải bằng chứng Done.
4. Mỗi người kiểm tra trang mình, Tuấn kiểm tra bản chung; chụp ảnh hướng dẫn từ bản chạy thật.

## Cấu trúc thư mục khi triển khai

- Pages/Shapes/: Index, Details, Graph — Vỷ.
- Pages/Compare/: Index — Tín.
- Pages/Practice/: Index, Take, Result; Pages/Account/: Login, Logout — Tuấn.
- Models/ViewModels/: dữ liệu hiển thị; Models/InputModels/: input form.
- Services/: nghiệp vụ; Repositories/: driver và Cypher; Data/: schema/seed.
- Program.cs, DI, cấu hình và layout chung do Tuấn điều phối; báo trong task trước khi sửa.

## Quy tắc

C# PascalCase, namespace Nosql_Neo4j; async có hậu tố Async và CancellationToken. GET chỉ đọc, POST dùng antiforgery mặc định của Razor Pages. Dùng form Tag Helpers; không tắt token.

Bind InputModel riêng bằng [BindProperty], không bind entity Neo4j hoặc điểm/userId/role/đáp án đúng. Query dùng tham số OnGetAsync. Khi ModelState sai phải nạp lại dropdown/ViewModel rồi return Page(); thành công dùng RedirectToPage (Post/Redirect/Get).

Service kiểm tra quyền/chủ lượt từ danh tính server. [Authorize] đặt ở PageModel/trang, không đặt riêng lên handler. Razor encode nội dung; không Html.Raw dữ liệu người dùng. Không tin hidden field để chấm bài.

Cypher tham số hóa; ID nghiệp vụ ổn định, không internal node ID. Không commit secret. Biến cấu hình Neo4j dự kiến: Neo4j__Uri, Neo4j__Username, Neo4j__Password; chưa có code kết nối đọc chúng.

## PR và thay đổi

Nhánh: feature/NEO4-13-shapes-pages; commit: NEO4-13: add shape search page. Dùng templates/pull-request.md. Không commit bin/, obj/, .user hay secret.

Trước merge: build thành công, kiểm tra GET/POST/validation/antiforgery/quyền và dữ liệu thật, một người khác review. Done cần commit/PR và bằng chứng kiểm tra.

Thay route/handler/model/interface: dùng templates/structure-change.md; cập nhật pages.md và template cùng PR, thống nhất với người bị ảnh hưởng trước merge.

Demo: Vỷ tra cứu/đồ thị; Tuấn CSDL và luyện tập/chấm điểm; Tín so sánh chữ nhật/thoi.
