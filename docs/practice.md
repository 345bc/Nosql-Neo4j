# Đăng nhập và luyện tập (MVC)

## Chuẩn bị local

1. Bật Neo4j, .env trỏ tới instance có database nosql-neo4j.
2. Chạy `dotnet run --launch-profile https -- --setup-data`: schema/kiến thức, 60 câu DRAFT (10 mỗi chủ đề) và migration snapshot của lượt cũ. Seed không tự công bố hoặc ghi đè phiên bản câu hỏi hiện có. Nếu chỉ cần nâng cấp lượt từ bản cũ, dùng `--migrate-attempts` để không chạy lại seed kiến thức.
3. Tạo tài khoản học viên từ terminal tương tác trong Development:

```powershell
dotnet run --launch-profile https -- --create-user tuan
```

Nhập mật khẩu 12–256 ký tự hai lần; màn hình không hiện mật khẩu. Lệnh tạo User ACTIVE vai trò USER, lưu hash PasswordHasher, không lưu mật khẩu thô. Tên đã có bị từ chối, lệnh không đặt lại mật khẩu hoặc hạ quyền người hiện có. Đây là công cụ local để cấp tài khoản, chưa phải màn hình quản trị tài khoản.

4. Khởi động web: dotnet run --launch-profile https.
5. /Account/Login để đăng nhập; /Practice để chọn chủ đề. Menu có Luyện tập, Lịch sử, Thống kê, Đổi mật khẩu và Đăng xuất POST.

## Nội dung cần được công bố

Trong Development, trang /Practice có nút **Chạy thử 10 câu**, mặc định Hình vuông. Chế độ này đọc DRAFT, không đổi trạng thái seed. Lượt lưu isDemo=true trên Attempt và trong snapshot, đề/kết quả có nhãn demo. Production chặn tạo, đọc và nộp các lượt demo, kể cả dùng lại URL/cookie. Khi làm thống kê chính thức, lọc coalesce(a.isDemo, false)=false. Lượt cũ không có thuộc tính isDemo được xem là lượt thường.

Trang sản phẩm chỉ tạo đề từ Shape và QuestionVersion PUBLISHED. Seed vẫn DRAFT nên chưa tạo được đề. Dùng /Dev/Questions kiểm tra 10 câu; rà soát kiến thức, ghi nguồn thực tế (sourceTitle/sourceLocator) và người duyệt (reviewedBy/reviewedAt) trên phiên bản câu và hình trước khi công bố. Giữ bản công bố bất biến, chỉnh sửa tạo phiên bản mới. Hiện chưa có màn hình công bố hoặc enforcement bất biến ở tầng quản trị: người quản lý CSDL thực hiện có kiểm soát.

Không tự đổi PUBLISHED chỉ để demo. Nhóm phải chốt nguồn và quy ước hình thang trước nghiệm thu. Ngân hàng có 60 câu nháp tính chu vi/diện tích/góc/đường chéo/đường trung bình; các câu hình vuông cũ được giữ nguyên. Vẫn cần người duyệt rà toán học và độ đa dạng để đáp ứng nghiệm thu nội dung SRS.

## Luồng và giới hạn

- Đăng nhập sai trả thông báo chung; POST login giới hạn 10 lần/phút/IP. Cookie HttpOnly, Secure, SameSite=Lax, 8 giờ. Mỗi request đã đăng nhập kiểm tra tài khoản ACTIVE, vai trò và securityStamp trong Neo4j; đổi stamp/khóa tài khoản thu hồi phiên.
- UserId lấy từ cookie server; form không được quyết định chủ lượt. Truy cập lượt khác trả 404.
- Tạo đúng 10 câu khác mã, lấy phiên bản CURRENT duy nhất và chủ đề duy nhất. Câu thiếu prompt/giải thích, lựa chọn trống/trùng hoặc đáp án không hợp lệ không được chọn.
- Snapshot server chứa phiên bản/nội dung/lời giải cố định, lưu dưới Attempt.stateJson. View Take chỉ nhận PracticePaper không có CorrectKey/Explanation.
- Nộp sử dụng write lock trên Attempt; kiểm tra và chấm trong cùng managed transaction, rồi redirect Result. Đúng=1, sai/trống=0. Item trùng/ngoài đề bị từ chối.
- Nộp cùng đáp án (sau chuẩn hóa thứ tự và item thiếu=null) trả kết quả cũ; thay đổi sau nộp trả xung đột. Hết 24 giờ không nộp; hết hạn được tính từ expiresAt, chưa có tác vụ nền chuyển status EXPIRED.
- Snapshot còn được ghi thành 10 AttemptItem, HAS_ITEM/OF_VERSION; tạo/nộp và cập nhật snapshot đồ thị cùng transaction. stateJson giữ dữ liệu chuẩn để đọc/chấm bản cũ.
- `/Progress/History`: chỉ chủ lượt, SUBMITTED, phân trang 10, lọc chủ đề/ngày theo thời điểm nộp. Ngày Việt Nam UTC+7, ngày cuối được tính hết ngày.
- `/Progress/Statistics`: số lượt, trung bình hai chữ số thập phân, tỷ lệ đúng theo chủ đề một chữ số; mẫu số gồm câu sai/bỏ trống. Không có dữ liệu hiện thông báo, không hiển thị trung bình giả bằng 0. Demo được chọn riêng chỉ trong Development.
- `/Account/ChangePassword`: kiểm tra mật khẩu cũ và xác nhận mật khẩu mới; đổi hash/stamp với kiểm tra cạnh tranh, ghi audit, đăng xuất phiên hiện tại. Cookie cũ bị thu hồi ở request kế tiếp.
- Chưa có màn hình quản trị/công bố, mustChangePassword hoặc kiểm thử tải. Audit hiện mới có đổi mật khẩu. Backup offline có hướng dẫn, chưa diễn tập.

## Kiểm tra

`./scripts/Check-Local.ps1` chạy bộ kiểm thử service, Neo4j và HTTP. Hai tài khoản tạm/mọi lượt của chúng được dọn sau chạy. Không dùng hoặc đổi mật khẩu tài khoản demo của bạn.

Harness service kiểm tra đề thiếu câu, giấu đáp án, chấm 7 đúng/2 sai/1 trống, item trùng/ngoài đề, nộp lặp/thay đáp án, quyền sở hữu, hết hạn, Production chặn demo, mật khẩu và ngày/rounding. Harness tích hợp kiểm tra nộp đồng thời cùng/khác đáp án trên Neo4j thật, snapshot đồ thị, lịch sử/thống kê/phân trang và HTTP login/CSRF/quyền/thu hồi cookie.

Thử bản tích hợp sau công bố dữ liệu: đăng nhập, tạo lượt, bỏ một câu, nộp, kiểm tra điểm/lời giải, refresh kết quả, đăng xuất. Đăng nhập tài khoản thứ hai rồi dùng URL lượt đầu phải 404.
