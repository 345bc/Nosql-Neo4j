# Nạp database mới

## File cần chạy

**[seed-all.cypher](seed-all.cypher)** là file seed tổng hợp cho database trống. Chỉ chạy file này; không cần nạp lại schema.cypher, seed.cypher, seed-knowledge.cypher, practice-schema.cypher hay seed-vy-additions.cypher.

File không tạo database vật lý. Tạo/chọn database ứng dụng trong Neo4j trước, dùng cùng tên với `Neo4j__Database` của ứng dụng.

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

## Chạy trong Neo4j Browser một lần

1. Chọn database ứng dụng.
2. Trong Settings, bật **Enable multi statement query editor**. Xem [hướng dẫn Neo4j Browser](https://neo4j.com/docs/browser/legacy/visual-tour/).
3. Copy toàn bộ `seed-all.cypher`, dán vào query editor và bấm Run một lần.
4. Kiểm tra bảng tổng ở phần 6 và các kết quả kiểm tra ở phần 7.

Nếu dùng Neo4j Query không có multi statement, chạy file bằng Cypher Shell:

```powershell
cypher-shell -a bolt://localhost:7687 -u neo4j -d nosql-neo4j -f migration/seed-all.cypher
```

Chạy lệnh từ thư mục gốc project, thay URI/username/database theo cấu hình rồi nhập mật khẩu khi được hỏi.

## File nạp những nghiệp vụ nào?

| Phần | Dữ liệu | Nghiệp vụ |
| --- | --- | --- |
| 1 | Constraint và index | Core, tài khoản, câu hỏi, lượt làm, audit |
| 2 | 6 Shape, 6 IS_A | Danh sách hình, đồ thị phân loại |
| 3 | Định nghĩa, tính chất, nhận biết, ví dụ; 12 Formula (10 core, 2 bổ sung) | Chi tiết hình và so sánh |
| 4 | 60 Question/QuestionVersion, 10 câu mỗi chủ đề | Luyện tập, chấm điểm |
| 5 | 63 KnowledgeItem, 63 HAS_KNOWLEDGE; metadata minh họa | Phần tra cứu của Vỷ |
| 6–7 | Tổng số và truy vấn kiểm tra | Xác nhận cấu trúc sau nạp |

`seed-all.cypher` là nguồn seed hiện tại của project. Bộ file Data cũ đã bỏ; bản tổng hợp chứa dữ liệu trực tiếp, không cần APOC, LOAD CSV hoặc JSON ngoài khi chạy. Chỉnh nội dung seed tại file này.

`dotnet run --launch-profile https -- --verify-data` chỉ chạy các truy vấn ở phần 7 của file, không nạp lại dữ liệu. Lệnh `--migrate` chạy toàn bộ file rồi thoát, không mở web; dùng `Run-Migration.ps1 -StartApp` nếu muốn mở web sau khi nạp thành công.

## Kết quả mong đợi trên database trống

`shapes=6`, `classificationEdges=6`, `formulas=12`, `questions=60`, `questionVersions=60`, `knowledgeItems=63`, `knowledgeLinks=63`.

Các bảng lỗi phải rỗng. Bảng câu hỏi theo chủ đề có 6 dòng, mỗi dòng 10 câu. KnowledgeItem loại FORMULA thuộc mô hình Vỷ, không tạo thêm node Formula của core.

Theo yêu cầu demo, seed chuyển mọi node DRAFT sang PUBLISHED và nội dung mới cũng là PUBLISHED; giữ demo=true và reviewStatus chưa duyệt, không tạo người/thời gian duyệt giả. Không tạo người duyệt giả hoặc lượt làm mẫu. Seed tạo tài khoản demo ở phần dưới. Lệnh CLI cấp tài khoản riêng chưa được nối trong Program.cs hiện tại. Trong Development, dùng luyện tập chạy thử để làm câu nháp; trang chính thức chỉ đọc PUBLISHED đủ metadata.

## Database đã có dữ liệu

Chạy lại `--migrate` để bổ sung phần còn thiếu. Seed tổng hợp dùng MERGE/IF NOT EXISTS; không xóa dữ liệu hay ghi đè phiên bản câu đã tồn tại. KnowledgeItem mới dùng cùng trạng thái PUBLISHED với Shape; dữ liệu ARCHIVED vẫn giữ nguyên. Tổng số trên database đã có nội dung riêng có thể khác.

File chứa nhiều transaction, không bảo đảm toàn bộ file cùng thành công hoặc cùng rollback. Khi một câu lệnh báo lỗi, sửa nguyên nhân rồi chạy lại; xem kết quả cuối để phát hiện phần chưa nạp.

Nâng cấp snapshot lượt cũ từ stateJson vẫn cần lệnh `--migrate-attempts` của ứng dụng; seed Cypher không xử lý bước này. Không cần migration này cho database trống.

## Tài khoản demo được tạo sẵn

- Tên đăng nhập: `demo_tuan`
- Mật khẩu: `DemoTuan@2026!`
- Vai trò: USER; trạng thái: ACTIVE.

Chạy seed/migration rồi đăng nhập tại `/Account/Login`. Seed lưu hash tương thích ASP.NET Core Identity, không lưu mật khẩu thô trong node. Chạy lại không đổi mật khẩu, vai trò hoặc trạng thái của tài khoản đã có. Nếu `demo_tuan` đã tồn tại với mật khẩu khác, mật khẩu mẫu này không thay mật khẩu cũ.

## Dữ liệu bổ sung

Bản hiện tại thêm 12 ví dụ (hai mỗi hình), công thức đường chéo hình chữ nhật và đường trung bình hình thang. Tổng sau nạp bộ mẫu: 63 KnowledgeItem, 12 Formula. Các nội dung mẫu được hiển thị PUBLISHED theo yêu cầu demo, nhưng chưa được giảng viên phê duyệt. Seed đồng bộ nguồn cũ sourceRef vào sourceLocator khi sourceLocator còn trống.
