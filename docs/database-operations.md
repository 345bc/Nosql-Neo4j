# CSDL của Tuấn

Database ứng dụng đọc từ `Neo4j:Database`, mặc định `nosql-neo4j`. Đặt `Neo4j__Database` trong `.env` local hoặc biến môi trường khi deploy. Nếu dùng tên khác, thay tên database trong các lệnh backup/restore bên dưới cho khớp. Mỗi thành viên chạy instance riêng. Git lưu schema/seed, không chứa mật khẩu, dump hoặc dữ liệu học viên.

## Khởi tạo và nâng cấp local

Sau khi bật Neo4j, tạo database và điền `.env`:

```powershell
dotnet run --launch-profile https -- --migrate
dotnet run --launch-profile https -- --verify-data
```

`--migrate` chỉ chạy trong Development. Chạy constraints/indexes, seed sáu hình/mười công thức, 60 câu nháp, 49 KnowledgeItem của Vỷ và tài khoản demo_tuan. Snapshot lượt cũ nâng cấp riêng bằng --migrate-attempts. Chạy lại không nhân bản ID, không xóa lượt/tài khoản, không đổi đáp án hoặc lời giải của phiên bản câu hỏi đã tồn tại. Seed kiến thức có cập nhật các trường kiến thức DRAFT; dùng bootstrap khi không ai đang chỉnh nội dung. Không dùng lệnh này thay công cụ biên tập dữ liệu thật.

`--verify-data` chỉ đọc. Các dòng `invalid…`, `cycle`, `missingPublicationReview` khiến lệnh thất bại; sáu dòng đếm chủ đề không phải lỗi. Xem bảng tổng số ở phần 6 của `migration/seed-all.cypher` để kiểm tra kiến thức/phân loại.
Nếu chỉ nâng cấp lượt từ bản cũ, dừng web cũ rồi chạy `dotnet run --launch-profile https -- --migrate-attempts`. Lệnh tạo snapshot đồ thị từ stateJson và kiểm tra dữ liệu, không chạy lại seed kiến thức.

## Mô hình

| Node/cạnh | Quy tắc |
|---|---|
| Shape, Formula | ID duy nhất; IS_A con → cha; HAS_FORMULA |
| Question, QuestionVersion | ID duy nhất; HAS_VERSION giữ các phiên bản; CURRENT duy nhất khi chọn đề; ABOUT đúng một hình |
| User | ID/normalizedUsername duy nhất; username lookup không phân biệt hoa thường; passwordHash; role; status; securityStamp |
| User → STARTED → Attempt | Một chủ; IN_PROGRESS/SUBMITTED; roleAtStart; isDemo; startedAt/expiresAt/submittedAt; score; topicIds |
| Attempt → HAS_ITEM → AttemptItem | 10 item ID duy nhất; thứ tự, nội dung, lựa chọn, đáp án, giải thích được chụp lúc tạo |
| AttemptItem → OF_VERSION → QuestionVersion | Tham chiếu phiên bản gốc; kết quả vẫn dùng snapshot nếu phiên bản gốc bị sửa/xóa |
| User → PERFORMED → AuditEvent | Đổi mật khẩu ghi sự kiện/thời gian; không ghi mật khẩu/hash vào sự kiện |

`Attempt.stateJson` là snapshot chuẩn phục vụ chấm điểm và đọc kết quả cũ. Các item đồ thị được ghi trong cùng transaction khi tạo/nộp; không dùng nội dung câu hỏi live để chấm lại. Lịch sử phân trang 10 lượt; thống kê đọc tuần tự các snapshot SUBMITTED theo bộ lọc, không đưa snapshot đáp án lên trang thống kê. Chưa đo tải lớn; khi dữ liệu tăng có thể chuyển aggregate sang AttemptItem với kiểm thử tương đương.

## Công bố nội dung

60 câu là bản nháp tính toán dùng kiểm thử, chưa phải 60 câu đã nghiệm thu. Bộ câu hình vuông đã tồn tại giữ nguyên lựa chọn/đáp án; các chủ đề khác luân phiên vị trí đáp án. Người duyệt cần rà toán học, độ đa dạng, quy ước hình thang và bổ sung nguồn thật.

Trên Shape và QuestionVersion trước công bố cần `sourceTitle`, `sourceLocator`, `reviewedBy`, `reviewedAt` (datetime). Repository luyện tập yêu cầu những trường này cùng `status=PUBLISHED`. Formula cần cùng trạng thái Shape để được đọc. Không công bố bằng một câu SET hàng loạt khi chưa duyệt.

Khi sửa câu đã công bố: CREATE phiên bản ID mới, nối HAS_VERSION và ABOUT; rà/duyệt phiên bản mới, thay CURRENT trong một transaction; giữ phiên bản cũ. Không sửa các trường nội dung của phiên bản công bố tại chỗ. Chưa có màn hình quản trị/công bố; quy trình này do người quản lý CSDL thực hiện.

## Backup và phục hồi

Neo4j hỗ trợ dump/load database offline. Lệnh sau theo [tài liệu dump](https://neo4j.com/docs/operations-manual/current/backup-restore/offline-backup/) và [tài liệu load](https://neo4j.com/docs/operations-manual/current/backup-restore/restore-dump/). Dùng neo4j-admin của chính phiên bản instance, kiểm tra `--help` khi phiên bản khác.

1. Dừng web và instance qua Neo4j Desktop. Không chạy dump khi database còn mounted.
2. Mở terminal của instance; chọn thư mục backup riêng, đã tồn tại, có quyền truy cập giới hạn.

```powershell
neo4j-admin database dump nosql-neo4j --to-path="C:\Neo4jBackups\2026-10-07"
```

Dump chứa tài khoản ứng dụng và hash mật khẩu, lịch sử học tập. Bảo vệ file như dữ liệu cá nhân. Dump database ứng dụng không thay thế backup database `system`/cấu hình hoặc thông tin tài khoản DBMS.

3. Phục hồi trước trên **instance kiểm thử trống**, cùng phiên bản; dừng instance đó, không dùng cờ ghi đè database có sẵn:

```powershell
neo4j-admin database load nosql-neo4j --from-path="C:\Neo4jBackups\2026-10-07"
```

4. Bật instance kiểm thử, cập nhật `.env` riêng và chạy `--verify-data`. So sánh số node/cạnh, tài khoản, lượt và điểm; đăng nhập bằng một tài khoản kiểm thử để xem kết quả cũ. Chỉ phục hồi instance dùng thật sau khi bản thử đạt.

Không tự dừng/xóa database đang chạy. Dump/load offline chưa được thực hành trong phiên phát triển này; cần một lần diễn tập trên instance riêng trước nghiệm thu vận hành.
