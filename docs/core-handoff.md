# Bàn giao core đọc dữ liệu Neo4j

Ứng dụng .NET 10 MVC với Controller và Razor Views, không REST API. Các repository đã đăng ký trong Program.cs và dùng chung IDriver. Tất cả repository, phần so sánh và lệnh dữ liệu đọc chung `Neo4j:Database`, mặc định `nosql-neo4j`. Trong Development có thể đặt `Neo4j__Database=nosql-neo4j` trong `.env`; khi deploy dùng biến môi trường cùng tên. Mọi thành viên cần tạo/seed đúng database được cấu hình.

## Cách dùng IShapeRepository

Inject IShapeRepository qua constructor service. Controller gọi service để xử lý nghiệp vụ; repository chỉ đọc dữ liệu. Core hiện có IShapeDiagnosticService/ShapeDiagnosticService cho ba action Dev; chức năng công khai bổ sung service riêng.

| Hàm | Kết quả | Người dùng chính |
|---|---|---|
| GetPublishedAsync() | Danh sách ID/tên hình công bố | Vỷ/Tín |
| GetPublishedByIdAsync(id) | Chi tiết và công thức; null nếu không có/chưa công bố | Vỷ |
| GetPublishedGraphAsync(fromId, toId) | Nodes, Edges, Paths | Vỷ |
| GetPublishedPairAsync(leftId, rightId) | Hai chi tiết; null nếu một hình không có/chưa công bố | Tín |

Các hàm Draft tương ứng dành cho trang chẩn đoán Development. Trang kiểm tra phải chặn môi trường khác trước khi gọi repository. Không dùng hàm Draft trong trang công khai.

Pair đọc hai hình trong cùng giao dịch đọc; thiếu/trùng ID gây ArgumentException. Controller/service cần validate trước và hiển thị lỗi form. Pair chỉ trả nội dung gốc, không suy ra tính chất chung/riêng bằng cách so sánh chuỗi. Tín xây nghiệp vụ/bảng năm tiêu chí riêng theo mvc.md, thống nhất dữ liệu có cấu trúc với Tuấn khi cần.

GetGraph trả toàn bộ node/cạnh đúng trạng thái cùng các đường giữa hai ID. ID không có hoặc không có đường trả Paths rỗng; service phân biệt ID sai nếu cần. Truy vấn đường giới hạn 5 cạnh cho bộ sáu hình; thêm loại hình phải rà lại giới hạn. Node lẻ vẫn có trong Nodes.

Model thật nằm trong Models/: ShapeSummary, ShapeDetail, ShapeFormula, ShapeGraph, ShapeEdge, ShapePair. Template ViewModel trong templates/ là hướng dẫn, không phải lớp đã đăng ký hay được biên dịch. Service ánh xạ model repository sang ViewModel của trang.
ShapeDetail bổ sung các thuộc tính init SourceTitle/SourceLocator/ReviewedBy/ReviewedAt/Convention để Vỷ ánh xạ nguồn/quy ước; giữ nguyên constructor và chữ ký IShapeRepository. Chuỗi rỗng biểu thị chưa có metadata, ReviewedAt là chuỗi datetime Neo4j. Không tự tạo nguồn khi trường trống.

## Chạy và kiểm tra

1. Copy .env.example thành .env, điền mật khẩu riêng, bật Neo4j.
2. Chạy schema.cypher, seed.cypher, seed-knowledge.cypher theo data/README.md.
3. dotnet build, dotnet run --launch-profile https.
4. /Dev/Shapes: dự kiến 6 hình; bấm tên xem kiến thức/công thức.
5. /Dev/Graph: dự kiến 6 cạnh và 2 đường hình vuông → tứ giác.

Seed vẫn DRAFT nên hàm Published trả rỗng/null cho đến khi nội dung/nguồn được duyệt. Shape và Formula phải có cùng trạng thái để công thức xuất hiện. Không tự đổi PUBLISHED để vượt kiểm tra.

## Phần chưa hoàn tất

Core hỗ trợ đọc kiến thức/phân loại; nhánh Tuấn bổ sung đăng nhập/đổi mật khẩu, 60 câu nháp, luyện tập/chấm điểm, lịch sử/thống kê cá nhân và snapshot AttemptItem. Xem docs/tuan-handoff.md, docs/practice.md và docs/database-operations.md. Có audit đổi mật khẩu và hướng dẫn backup offline; chưa có quản trị tài khoản/công bố bằng giao diện hoặc diễn tập dump/restore. Tìm kiếm/đồ thị công khai và bảng so sánh năm tiêu chí cần bàn giao module của Vỷ/Tín; không coi toàn bộ SRS đã hoàn tất.

Tuấn đưa thay đổi lên repo chung; Vỷ/Tín fork hoặc đồng bộ upstream rồi làm nhánh chức năng. Không gửi .env thật lên GitHub. Đổi interface/schema cần báo hai người còn lại và cập nhật tài liệu cùng PR.
