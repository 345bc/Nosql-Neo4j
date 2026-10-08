# Hướng dẫn đóng góp — ASP.NET Core MVC

Dự án .NET 10 dùng Controller + Razor Views, không REST API và không PageModel.
Luồng: trình duyệt → Controller → Service → Repository → Neo4j; Controller trả ViewModel cho View .cshtml.

## Phân công

| Người | Trách nhiệm                                                              |
| ----- | ------------------------------------------------------------------------ |
| Tuấn  | Neo4j/schema/seed/repository, xác thực, luyện tập/chấm điểm, tích hợp    |
| Vỷ    | ShapesController, service, Views/Shapes cho tra cứu/tìm kiếm/đồ thị; SRS |
| Tín   | CompareController, service, Views/Compare; hướng dẫn sử dụng             |

Tín giữ phạm vi nhỏ hơn. Mỗi người làm trọn chức năng trên fork và gửi PR vào repo chung.

## Chạy local

Đọc README.md và Data/README.md. Copy .env.example thành .env, điền mật khẩu thật, bật instance và seed database nosql-neo4j.
Chạy dotnet restore, dotnet build, dotnet run --launch-profile https.
Development đọc .env; biến môi trường thật ưu tiên hơn .env, .env ưu tiên hơn user-secrets/appsettings. Không commit .env.

## Làm song song

Chốt route/action/InputModel/ViewModel/service theo [mvc.md](mvc.md) trong NEO4-16. Tuấn xác nhận schema/ID seed. Không ghi phê duyệt thay người khác.
Vỷ/Tín dựng View bằng service giả trả model C# trong Development; dùng cùng interface khi nối Neo4j thật.
Tích hợp từng chức năng sớm, kiểm tra dữ liệu thật trước Done. Tuấn điều phối Program.cs/DI/layout chung.

## Cấu trúc

- Controllers/: nhận request, validate input, gọi service, trả View/redirect/status.
- Views/{Controller}/: .cshtml; Views/Shared/: layout, partial, Error; không @page.
- Models/InputModels/: dữ liệu được bind từ form; Models/ViewModels/: dữ liệu hiển thị.
- Services/: quy tắc nghiệp vụ; Repositories/: Neo4j/Cypher; Data/: schema/seed.
- DevController và IShapeDiagnosticService chỉ phục vụ chẩn đoán DRAFT trong Development.

## Quy tắc

GET chỉ đọc. POST dùng [HttpPost]; bộ lọc AutoValidateAntiforgeryToken đã đăng ký toàn cục trong Program.cs.
Form Tag Helper sinh token; không tắt kiểm tra. Action mẫu có [ValidateAntiForgeryToken] để thể hiện yêu cầu rõ ràng.
Dùng asp-controller/asp-action/asp-route-id, không asp-page hay asp-page-handler.

Bind InputModel riêng, không bind entity hoặc điểm/userId/role/đáp án đúng.
ModelState sai phải nạp lại dropdown/ViewModel rồi View(model); thành công RedirectToAction (Post/Redirect/Get).
[Authorize] có thể đặt ở Controller hoặc action khi xác thực đã được triển khai.
Quyền/chủ lượt kiểm tra từ danh tính server trong service. GET không đăng xuất.
Login chỉ LocalRedirect với returnUrl local. Razor encode nội dung, không Html.Raw dữ liệu người dùng.

C# PascalCase, namespace Nosql_Neo4j; async dùng hậu tố Async. Cypher tham số hóa, ID nghiệp vụ ổn định.
Repository không trả HTTP/View, service không phụ thuộc Controller/HttpContext khi không cần thiết.
Các model repository đang có giữ nguyên; service ánh xạ sang ViewModel.

## Fork và PR

Nhánh feature/NEO4-13-shapes-mvc, commit NEO4-13: add shape search.
Dùng templates/pull-request.md. Không commit bin/, obj/, .user hoặc secret.
Trước merge: build, kiểm tra GET/POST/validation/antiforgery/quyền và dữ liệu thật, người khác review.
Đổi route/model/interface/schema: dùng templates/structure-change.md, đồng bộ mvc.md/template và báo người bị ảnh hưởng.

Demo: Vỷ tra cứu/đồ thị; Tuấn CSDL/luyện tập; Tín so sánh.
