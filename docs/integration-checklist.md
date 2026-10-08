# NEO4-20 — tích hợp ba luồng demo

Checklist được chuẩn bị từ nhánh `feature/tuan-database-practice`. Module Vỷ/Tín chưa có trong checkout này; trạng thái Done trên Jira không thay bằng chứng chạy bản merge. Chỉ đánh dấu các mục sau khi module tương ứng có mặt và được chạy.

## Tuấn — đã kiểm tra

- Build web thành công; không cảnh báo/lỗi biên dịch.
- Schema/seed chạy lại: 6 hình, 6 IS_A, 10 công thức, 60 câu DRAFT; không nhân bản ID.
- Hai đường hình vuông → tứ giác đúng chiều; mảng kiến thức ánh xạ từ Neo4j; đọc cặp chữ nhật/thoi.
- Đăng nhập có hash mật khẩu, cookie Secure/HttpOnly và URL trở về local; form ghi có antiforgery.
- Đề 10 câu khác mã, không chứa đáp án/giải thích; snapshot giữ nội dung/phiên bản/thứ tự.
- Nộp 7 đúng/2 sai/1 trống được 7/10; nộp đồng thời cùng đáp án trả cùng kết quả; khác đáp án chỉ một lượt được ghi.
- GET/POST lượt người khác bị chặn; kết quả cũ vẫn đọc được sau 24 giờ; nộp quá hạn bị chặn.
- Lịch sử phân trang/lọc chủ đề/ngày UTC+7; thống kê tính câu trống; demo tách khỏi chính thức.
- Đổi mật khẩu đổi hash/stamp, ghi audit; cookie cũ bị thu hồi ở request kế tiếp.
- HTTP Production chặn Dev, nút/action demo và đọc đề/kết quả/báo cáo demo.
- Kiểm thử dùng tài khoản/lượt tạm riêng, dọn sau chạy; không đổi tài khoản `demo_tuan`.

Chạy lại: `./scripts/Check-Local.ps1`. Các lỗi dữ liệu khiến `--verify-data` thất bại. Nếu còn ứng dụng cũ chạy song song và tạo lượt chưa có AttemptItem, dừng bản cũ rồi chạy `--migrate-attempts`.

## Vỷ — chờ module và bản merge

- `/Shapes`: tìm `hinh thoi`, tên/bí danh bỏ dấu, phân trang; query GET không ghi dữ liệu.
- Chi tiết đủ định nghĩa/tính chất/dấu hiệu/công thức/ví dụ/nguồn/quy ước; chỉ PUBLISHED.
- `/Shapes/Graph`: sáu node/sáu cạnh trực tiếp, hai đường đúng chiều, không chu trình; có danh sách quan hệ bằng chữ.
- Thử không có kết quả, ID sai, dữ liệu chưa công bố và mất kết nối CSDL.
- Thử desktop/mobile, menu trang chủ và các link sang chi tiết/đồ thị.

## Tín — chờ module và bản merge

- `/Compare`: chọn chữ nhật/thoi; bảng Cạnh/Góc/Đường chéo/Đối xứng/Công thức và tính chất chung/riêng có điều kiện rõ.
- Không chọn: form rỗng; thiếu/trùng ID: validation; ID sai/chưa công bố: 404.
- Không suy ra tính chất chung từ so sánh chuỗi; chỉ đọc dữ liệu công bố.
- Thử bảng trên desktop/mobile; cung cấp ảnh chạy thật cho tài liệu NEO4-21.

## Nghiệm thu cuối

- Merge PR của từng người sau review; đồng bộ upstream rồi build và chạy lại cả ba luồng.
- Người duyệt rà 60 câu/kiến thức, ghi nguồn thật và công bố phiên bản hợp lệ. Seed DRAFT hiện chưa nghiệm thu AC nội dung.
- Kiểm tra request lỗi CSDL hiển thị 503 và không báo lưu thành công giả; cần diễn tập mất kết nối có kiểm soát.
- Kiểm tra điều hướng bàn phím, nhãn form và responsive thủ công; kiểm thử HTTP không thay kiểm tra trình bày.
- Diễn tập dump/load trên instance riêng và so sánh kết quả/lịch sử phục hồi.
- Ghi lỗi còn lại và ảnh ba người thao tác chức năng riêng; chỉ sau đó hoàn tất NEO4-20.
