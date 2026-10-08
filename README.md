# Nosql-Neo4j

Ứng dụng học toán tứ giác dùng ASP.NET Core MVC (.NET 10), Razor Views và Neo4j. Nhánh Tuấn có core CSDL, đăng nhập/đổi mật khẩu, luyện tập/chấm điểm, lịch sử và thống kê cá nhân. Các luồng tra cứu/đồ thị và so sánh do Vỷ/Tín bàn giao riêng.

## Chuẩn bị kết nối

Cài .NET SDK 10, bật Neo4j và tạo database ứng dụng. Tạo file `.env` tại thư mục chứa `Program.cs`:

```dotenv
Neo4j__Uri=bolt://localhost:7687
Neo4j__Username=neo4j
Neo4j__Password=CHANGE_ME
Neo4j__Database=nosql-neo4j
```

Thay `CHANGE_ME` bằng mật khẩu Neo4j của bạn. Tất cả repository và lệnh dữ liệu dùng chung tên database này. Trong Neo4j Browser/Query, chọn cùng database trước khi chạy Cypher. `.env` được đọc trong Development và không được commit.

## Database đã có core

Project dùng một file seed duy nhất: [migration/seed-all.cypher](migration/seed-all.cypher). Chạy `./migration/Run-Migration.ps1` để bổ sung phần còn thiếu. File dùng MERGE/IF NOT EXISTS, giữ nội dung đã tồn tại và không xóa dữ liệu. Hình PUBLISHED/ARCHIVED không được bổ sung KnowledgeItem DRAFT. Nếu database có nội dung riêng, tổng số sau nạp có thể khác database trống.

## Database mới: nạp một file duy nhất

Dùng **[migration/seed-all.cypher](migration/seed-all.cypher)**. File có sẵn schema, core, 60 câu luyện tập, 63 KnowledgeItem (49 gốc của Vỷ và 14 bổ sung) và truy vấn kiểm tra; không cần chạy các file con trước hoặc sau nó.

1. Tạo/chọn database ứng dụng trong Neo4j, cùng tên với `Neo4j__Database` trong `.env`. File seed không tạo database vật lý.
2. Trong **Neo4j Browser → Settings**, bật **Enable multi statement query editor**. [Hướng dẫn Neo4j](https://neo4j.com/docs/browser/legacy/visual-tour/).
3. Mở `migration/seed-all.cypher`, copy toàn bộ vào ô query, bấm **Run một lần**.
4. Xem bảng tổng ở phần 6: `shapes=6`, `classificationEdges=6`, `formulas=12`, `questions=60`, `questionVersions=60`, `knowledgeItems=63`, `knowledgeLinks=63`.
5. Các truy vấn lỗi ở phần 7 phải trả 0 dòng. Bảng tổng câu theo chủ đề trả 6 dòng, mỗi chủ đề có 10 câu.

File gồm nhiều câu lệnh được phân cách bằng `;`; chế độ multi statement cho phép gửi cả file với một lần Run. Mỗi câu lệnh có transaction riêng, nên nếu có lỗi cần sửa lỗi và chạy lại. File dùng MERGE/IF NOT EXISTS và giữ dữ liệu đã tồn tại; không xóa database. Phần core chỉ điền kiến thức còn thiếu trên hình DRAFT, câu hỏi đã tồn tại không bị ghi đè.

Nếu công cụ Neo4j Query bạn đang dùng không có chế độ multi statement, có thể chạy cả file bằng Cypher Shell:

```powershell
cypher-shell -a bolt://localhost:7687 -u neo4j -d nosql-neo4j -f migration/seed-all.cypher
```

Nhập mật khẩu Neo4j khi được hỏi. Thay URI, username và database bằng cấu hình của bạn.

Seed tạo tài khoản demo_tuan, không tạo lượt làm giả, không nâng cấp snapshot lượt cũ và không tự công bố. Nội dung mẫu hiện dùng PUBLISHED theo yêu cầu demo, giữ thông tin chưa được giảng viên duyệt. Chi tiết từng phần và cách xử lý database đã có core: [migration/README.md](migration/README.md).

Lệnh seed hiện tại là `--migrate`. Bộ nạp `--setup-data` và các file seed lẻ đã bỏ; không cần chạy chúng.

## Chạy migration bằng ứng dụng (không cần bật multi statement)

Bật Neo4j, tạo/chọn database ứng dụng và điền kết nối trong `.env`. Từ thư mục gốc project, chạy:

```powershell
./migration/Run-Migration.ps1
```

Muốn nạp dữ liệu xong rồi mở web:

```powershell
./migration/Run-Migration.ps1 -StartApp
```

Hoặc gọi thẳng ứng dụng:

```powershell
dotnet run --launch-profile https -- --migrate
```

Ứng dụng đọc `migration/seed-all.cypher`, tách các câu lệnh và chạy lần lượt bằng driver dùng chung. Cuối mỗi bước in kết quả; lỗi kiểm tra dữ liệu khiến lệnh báo thất bại. Các bước đã thành công vẫn được lưu. Lệnh chỉ chạy trong Development, không tạo database vật lý. Để nâng cấp snapshot lượt cũ, dùng `--migrate-attempts` riêng.

## Đăng nhập và mở ứng dụng

Sau khi chạy seed tổng hợp hoặc `--migrate`, đăng nhập bằng **`demo_tuan` / `DemoTuan@2026!`**. Nếu tài khoản này đã tồn tại thì seed giữ mật khẩu cũ. Mật khẩu ứng dụng độc lập với mật khẩu Neo4j. Bản hiện tại chưa nối lệnh CLI cấp tài khoản riêng; không dùng `--create-user`.

```powershell
dotnet run --launch-profile https
```

Mở [đăng nhập](https://localhost:7277/Account/Login), rồi [luyện tập](https://localhost:7277/Practice). Trong Development, dùng nút **Chạy thử 10 câu** để làm câu DRAFT. Trang chẩn đoán: `/Dev/Shapes`, `/Dev/Graph`, `/Dev/Questions`.

Lịch sử/thống kê tự lấy từ các lượt đã nộp; không cần nạp một file seed riêng. Tạo tài khoản cũng không cần chạy một file Cypher riêng.

## Trạng thái và nguồn của dữ liệu demo

Seed hiện chuyển toàn bộ node có status DRAFT sang PUBLISHED theo yêu cầu demo. Sau nạp, sáu hình, các KnowledgeItem, Formula và QuestionVersion đều dùng PUBLISHED. Trang tra cứu cần dùng PUBLISHED cho tất cả truy vấn; không trộn truy vấn Shape DRAFT với KnowledgeItem PUBLISHED.

Các node mẫu giữ demo=true và trạng thái chưa duyệt; seed không tạo người/thời gian duyệt giả. Luồng luyện tập chính thức vẫn yêu cầu đủ metadata nguồn và duyệt, nên việc đổi status không tự đáp ứng điều kiện này.

Nguồn dùng sourceLocator cho cả core và KnowledgeItem. Seed điền từ sourceRef cũ nếu sourceLocator còn trống, không ghi đè nguồn đang có.

## Làm việc qua fork

Fork repository này về tài khoản cá nhân, clone fork và thêm upstream trỏ về repository chung. Làm trên nhánh chức năng, gửi pull request về `master` của repo chung. Đồng bộ upstream trước khi bắt đầu công việc mới; giải quyết xung đột trước khi gửi PR. Không force-push lên repo chung.

- Tuấn: CSDL, repository và luyện tập/chấm điểm.
- Vỷ: tra cứu, tìm kiếm, đồ thị.
- Tín: so sánh và hướng dẫn sử dụng.

Xem [CONTRIBUTE.md](CONTRIBUTE.md), [cấu trúc MVC](mvc.md) và [bàn giao core](docs/core-handoff.md). Không commit `.env`; cấu hình mẫu chia sẻ cho nhóm phải dùng mật khẩu `CHANGE_ME`.

## Đăng nhập và luyện tập

Tạo lượt 10 câu không trùng; chụp phiên bản/nội dung; không đưa đáp án lên đề; chấm nguyên tử, chống nộp trùng và kiểm tra chủ lượt/hạn 24 giờ. Lịch sử/thống kê chỉ tính lượt SUBMITTED, tách demo khỏi chính thức. Kiến trúc: Controller → Service → Repository → Neo4j, không REST API.

Xem [luồng luyện tập](docs/practice.md), [bàn giao phần Tuấn](docs/tuan-handoff.md), [checklist tích hợp](docs/integration-checklist.md) và [vận hành CSDL](docs/database-operations.md). Nội dung PUBLISHED phải có nguồn/người duyệt; quản trị/công bố bằng giao diện chưa nằm trong bản này.

## Kiểm thử local

Neo4j phải bật và đã chạy `--migrate`. Dùng PowerShell 7 từ thư mục dự án:

```powershell
./scripts/Check-Local.ps1
```

Script build sang thư mục riêng, kiểm tra service, giao dịch Neo4j và HTTP cả Development/Production; dùng hai tài khoản tạm và tự dọn dữ liệu của chúng. Web kiểm thử dùng cổng 7281/7282 và được tắt sau khi chạy; không dừng web của bạn. Kết quả và giới hạn xem tài liệu bàn giao. Dump/restore offline cần diễn tập trên instance riêng.
