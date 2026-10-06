# Hợp đồng MVC

Controller + Razor Views, không REST API. Tài liệu này thay pages.md. Các controller chức năng dưới đây là cấu trúc dự kiến; hiện mới có HomeController và DevController.

## Route/action

| Controller/action | Route | Người | Cách gọi |
|---|---|---|---|
| Home/Index, Privacy | /, /Home/Privacy | Chung | GET |
| Home/Error | /Error | Chung | Exception handler, giữ status lỗi |
| Dev/Shapes | /Dev/Shapes | Tuấn | GET, danh sách DRAFT |
| Dev/ShapeDetails | /Dev/ShapeDetails/{id} | Tuấn | GET, chi tiết DRAFT |
| Dev/Graph | /Dev/Graph | Tuấn | GET, đồ thị DRAFT |
| Shapes/Index | /Shapes | Vỷ | GET(q, page=1) |
| Shapes/Details | /Shapes/Details/{id} | Vỷ | GET(id) |
| Shapes/Graph | /Shapes/Graph | Vỷ | GET(fromId, toId) |
| Compare/Index | /Compare | Tín | GET(leftId, rightId), form GET |
| Account/Login | /Account/Login | Tuấn | GET form, POST InputModel |
| Account/Logout | /Account/Logout | Tuấn | Chỉ POST |
| Practice/Index | /Practice | Tuấn | GET chủ đề |
| Practice/Create | /Practice/Create | Tuấn | POST TopicId, redirect Take |
| Practice/Take | /Practice/Take/{id} | Tuấn | GET đề |
| Practice/Submit | /Practice/Submit/{id} | Tuấn | POST câu trả lời, redirect Result |
| Practice/Result | /Practice/Result/{id} | Tuấn | GET kết quả |

Route mặc định {controller=Home}/{action=Index}/{id?}; tên action không cần hậu tố Async trên URL.
View mặc định: Views/{Controller}/{Action}.cshtml. Dùng @model kiểu ViewModel, không @page.
Model binding: action nhận InputModel, field form phải khớp tên thuộc tính; nếu binding prefix Input thì field Input.Items[0].ItemId và khai báo [Bind(Prefix = "Input")].

## Hợp đồng nội bộ

Model thật: ShapeSummary, ShapeDetail/ShapeFormula, ShapeGraph/ShapeEdge, ShapePair trong Models/.
IShapeRepository giữ nguyên, xem docs/core-handoff.md. Dữ liệu public dùng các hàm Published, Dev dùng Draft.
Service chuyển model repository sang ViewModel; template hợp đồng dữ liệu trong templates/MvcContracts.cs.template được giữ vì không phụ thuộc PageModel.

Danh sách ViewModel: Items/Page/PageSize/TotalItems; page 1-based, pageSize=20.
Chi tiết gồm định nghĩa/tính chất/dấu hiệu/công thức/ví dụ/nguồn/quy ước.
So sánh có Left/Right, năm tiêu chí Cạnh/Góc/Đường chéo/Đối xứng/Công thức và tính chất chung/riêng; phải rà soát điều kiện, không suy ra từ so sánh chuỗi.
Đề và kết quả dùng kiểu tách riêng, đề không có CorrectKey/Explanation.
Input tạo lượt chỉ TopicId nullable; submit chỉ ItemId/SelectedKey nullable A/B/C/D, không nhận điểm hoặc userId.

## Validation, lỗi và quyền

Controller kiểm tra ModelState, gọi service, nạp lại ViewModel khi lỗi; POST thành công RedirectToAction.
Service kiểm tra nghiệp vụ/quyền/chủ lượt từ danh tính server. NotFound cho dữ liệu không có/không thuộc chủ; Neo4j không sẵn sàng trả View lỗi 503, không lộ exception/secret.
DomainValidationException (template) chuyển lỗi vào ModelState; code trạng thái lượt quyết định redirect Take/Result hoặc thông báo hết hạn.
DevController chặn mọi action ngoài Development và no-store. Không dùng hàm Draft trên trang công khai.
[Authorize] ở controller/action sau khi triển khai auth; antiforgery toàn cục cho request ghi. Login returnUrl phải local.

## Nghiệp vụ giữ nguyên

Public chỉ PUBLISHED; ID seed thống nhất trong Data/README.md.
Tìm tên/bí danh bỏ dấu, đ→d, không phân biệt hoa thường, thu gọn khoảng trắng; xếp khớp toàn bộ → tiền tố → chứa → tên chuẩn hóa/ID.
IS_A con→cha, không chu trình; sáu cạnh như Data/seed.cypher, hình thang có ít nhất một cặp cạnh đối song song.
Compare chưa chọn gì chỉ hiện form; thiếu một ID/trùng ID báo validation.
Đề 10 câu không trùng và cố định phiên bản; thiếu câu báo số hiện có.
Chấm server đúng=1, sai/trống=0, commit nguyên tử; nộp lặp tương đương trả kết quả cũ, đổi đáp án sau nộp bị chặn.
Item thiếu=null, trùng/ngoài đề/sai miền báo lỗi; hạn 24 giờ. GET/POST kiểm tra chủ lượt.

Lịch sử/thống kê/quản trị/đổi mật khẩu chưa chốt action/model; dùng templates/mvc-spec.md trước triển khai.
