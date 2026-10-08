# Deploy Docker lên Render + Neo4j Aura

## 1. Chuẩn bị database

Tạo AuraDB Free, lưu mật khẩu và URI `neo4j+s://...databases.neo4j.io`.
Chạy `migration/seed-all.cypher` trong Query của Aura, chọn database `neo4j`.
Dữ liệu Neo4j Desktop không tự chuyển lên Aura. Web không tự seed khi khởi động.

## 2. Deploy trên Render

Commit và push Dockerfile, .dockerignore, render.yaml cùng code đã tổng hợp.
Trong Render chọn New → Blueprint, kết nối repository và chọn nhánh muốn deploy.
Render đọc render.yaml. Nhập `Neo4j__Uri` và `Neo4j__Password` của Aura khi được yêu cầu.
Kiểm tra gói Free trước khi tạo service. Username/database mặc định là `neo4j`;
nếu Aura cấp tên khác, sửa hai biến môi trường tương ứng.

Cũng có thể tạo New → Web Service, runtime Docker, Dockerfile `./Dockerfile`,
gói Free và nhập các biến môi trường trong render.yaml bằng tay.
Không nhập Build Command hoặc Start Command: Dockerfile đã định nghĩa chúng.

Render cấp URL HTTPS. Container nghe HTTP cổng 8080, Render xử lý TLS.
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` giúp ứng dụng nhận scheme HTTPS từ proxy,
để cookie đăng nhập và redirect hoạt động. Chỉ bật tùy chọn này khi chạy sau proxy
của nền tảng; không mở trực tiếp cổng container này ra Internet.

Production không đọc .env; cấu hình trên Render bằng biến môi trường.
Không đưa .env/mật khẩu vào Git hoặc Docker image.
Ứng dụng kiểm tra kết nối Neo4j ngay lúc khởi động: Aura phải hoạt động và thông tin phải đúng.
Free web service có thể ngủ khi không có truy cập. Mở URL và kiểm tra trước giờ demo.
Khởi động lại/redeploy có thể làm phiên đăng nhập mất hiệu lực vì khóa cookie lưu trong container;
người dùng đăng nhập lại. Kết quả luyện tập vẫn lưu trong Neo4j.

## 3. Kiểm tra Docker trên máy

Bật Docker Desktop ở chế độ Linux containers rồi chạy tại thư mục dự án:

```powershell
docker build -t tu-giac-lab .
```

Để thử đầy đủ đăng nhập, deploy lên Render HTTPS hoặc chạy sau reverse proxy HTTPS.
Nếu thử kết nối DB local từ container, dùng `host.docker.internal` thay vì `localhost`.
Image chạy Production, nên không dùng nó để chạy lệnh migration Development.

## 4. Kiểm tra sau deploy

- Trang chủ và CSS tải đúng.
- Tra cứu có sáu chủ đề, trang chi tiết và đồ thị có dữ liệu.
- Đăng nhập bằng tài khoản đã seed; đổi mật khẩu demo trước khi chia sẻ rộng.
- Tạo lượt luyện tập, nộp bài và mở lại lịch sử.
- Trong Logs không có lỗi kết nối Neo4j hoặc redirect HTTPS lặp lại.

Tài liệu: https://render.com/docs/docker,
https://render.com/docs/free,
https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0
