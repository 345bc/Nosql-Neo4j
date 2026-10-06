# Dữ liệu Neo4j dùng chung

Chọn database học tập trong Neo4j Query, không chạy seed vào system. Chạy từng câu lệnh (kết thúc bằng dấu ;) theo thứ tự:

1. schema.cypher: constraints ID cho Shape và Formula.
2. seed.cypher: sáu hình, sáu cạnh IS_A.
3. seed-knowledge.cypher: kiến thức nền và mười công thức.
4. verify.cypher: chỉ đọc để kiểm tra.

Chạy seed hai lần rồi kiểm tra: vẫn 6 Shape, 6 IS_A, 10 Formula và 2 đường hình vuông → tứ giác. Các kiểm tra ID trùng, chu trình và thiếu kiến thức phải không có dòng. Các tổng trên dành cho database mới chỉ chứa seed này; dữ liệu nhóm bổ sung có thể làm tổng tăng.

## Schema đang chốt

- Shape: id, name, aliases (string[]), status, definition, properties (string[]), recognitionSigns (string[]), examples (string[]), convention, reviewStatus, revision.
- Formula: id, name, expression, variables, conditions, status.
- (Shape)-[:IS_A]->(Shape): con → cha.
- (Shape)-[:HAS_FORMULA]->(Formula): công thức của từng hình; repository ánh xạ thành FormulaVm.

Seed cơ bản cập nhật name theo bộ chuẩn. Seed kiến thức chỉ cập nhật hình DRAFT, giữ revision hiện có và dùng ID công thức cố định. Đây là bootstrap phát triển, không phải cơ chế cập nhật nội dung quản trị/audit. Chạy khi không có người đang sửa dữ liệu; không dùng seed để ghi đè dữ liệu sản xuất. Seed không xóa node/cạnh cũ ngoài bộ này; nếu kiểm tra phát hiện cạnh sai, rà soát riêng.

## Trạng thái và nguồn

Nội dung là bản nháp phát triển do nhóm cần rà soát; chưa có giáo trình do giảng viên chỉ định, người duyệt hay tham chiếu trang. Không tạo nguồn hoặc phê duyệt giả. Giữ status=DRAFT, reviewStatus=PENDING. Seed chưa đủ điều kiện nghiệm thu nội dung SRS.

Trước công bố: bổ sung nguồn thật, đối chiếu định nghĩa/điều kiện/công thức, ghi người và thời gian duyệt; chốt schema nguồn với cả nhóm. Luồng học tập chỉ đọc PUBLISHED. Để kiểm tra CSDL lúc phát triển, dùng verify.cypher hoặc trang chẩn đoán Development đọc DRAFT; không bỏ bộ lọc PUBLISHED ở trang công khai.

Chưa có câu hỏi, tài khoản, lượt làm bài hoặc lịch sử. Đây mới là nền kiến thức/phân loại; các module đó triển khai sau bằng schema riêng đã thống nhất.
