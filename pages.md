# Hợp đồng Razor Pages

Quy ước triển khai cho nhóm; chưa xác nhận chức năng đã có trong mã. Không có REST API.

## Trang và handler

| File trong Pages / route | Người | GET | POST |
|---|---|---|---|
| Shapes/Index /Shapes | Vỷ | OnGetAsync(q, page=1): tìm/danh sách | Không |
| Shapes/Details /Shapes/Details/{id} | Vỷ | OnGetAsync(id): chi tiết | Không |
| Shapes/Graph /Shapes/Graph | Vỷ | OnGetAsync(fromId, toId): đồ thị | Không |
| Compare/Index /Compare | Tín | OnGetAsync(leftId, rightId): form/bảng | Không; form GET |
| Account/Login /Account/Login | Tuấn | OnGet(returnUrl): form | OnPostAsync: xác thực, redirect URL local |
| Account/Logout /Account/Logout | Tuấn | Không đăng xuất | OnPostAsync: hủy phiên |
| Practice/Index /Practice | Tuấn | OnGetAsync: chủ đề | OnPostAsync: tạo lượt, redirect Take |
| Practice/Take /Practice/Take/{id} | Tuấn | OnGetAsync(id): đề | OnPostSubmitAsync(id): chấm, redirect Result |
| Practice/Result /Practice/Result/{id} | Tuấn | OnGetAsync(id): kết quả | Không |

Route ID dùng @page "{id}". Link dùng asp-page/asp-route-id; OnPostSubmitAsync tương ứng asp-page-handler="Submit". Không thêm Controller cho cùng luồng.

## Models và service

Chữ ký chuẩn trong templates/PageContracts.cs.template. Không serialize ViewModel thành JSON.

- ShapeSummaryVm: Id, Name, Aliases, ImageUrl nullable.
- ShapeDetailVm: Summary, Definition, Properties, RecognitionSigns, Formulas, Examples, Sources, Convention nullable. Formula có Name/Expression/Variables/Conditions; nguồn có Title/Locator.
- ShapeListVm: Items, Page, PageSize=20, TotalItems. Page 1-based, <=0 báo lỗi.
- GraphVm: Nodes, Edges (SourceId/TargetId/Type=IS_A), Paths (chuỗi ID), Convention. Hai filter cùng có/cùng vắng; ID sai báo validation; không có đường trả mảng rỗng.
- CompareVm: Left, Right, Criteria, CommonProperties, LeftOnlyProperties, RightOnlyProperties. Năm nhóm theo thứ tự: Cạnh, Góc, Đường chéo, Đối xứng, Công thức. Nội dung phải có điều kiện áp dụng và được rà soát.
- CreateAttemptInput: TopicId nullable (null=Tất cả).
- SubmitAttemptInput: Items gồm ItemId/SelectedKey nullable A/B/C/D. Tên field form: Input.Items[0].ItemId, Input.Items[0].SelectedKey... Không gửi điểm/đáp án đúng.
- AttemptPaperVm tách khỏi AttemptResultVm: đề không chứa CorrectKey/Explanation; kết quả giữ snapshot câu và lời giải. DateTimeOffset UTC, hiển thị giờ Việt Nam.

ID seed: TU_GIAC, HINH_THANG, HINH_BINH_HANH, HINH_CHU_NHAT, HINH_THOI, HINH_VUONG. Tuấn bàn giao mapping nếu seed cũ khác.

IShapeService do Vỷ triển khai; ICompareService do Tín; IPracticeService và ICurrentUser do Tuấn. Repository/schema do Tuấn chốt theo CSDL thật. Service giả và thật dùng cùng interface.

## Quyền, lỗi và điều hướng

Public chỉ đọc PUBLISHED. Practice yêu cầu đăng nhập; cookie auth redirect Login khi chưa đăng nhập. Service lấy danh tính server qua ICurrentUser, không nhận userId từ form.

DomainValidationException có Code và lỗi theo field (Input.Items/rightId...), PageModel chuyển vào ModelState. Khi lỗi form nạp lại dữ liệu lựa chọn/đề từ server rồi Page(). Detail không có/chưa công bố hoặc lượt không thuộc chủ trả NotFound. Neo4j lỗi hiển thị thông báo tạm thời với status 503, không lộ exception.

GetPaperAsync báo ATTEMPT_ALREADY_SUBMITTED thì redirect Result; ATTEMPT_EXPIRED thì hiển thị hết hạn/link tạo mới. GetResultAsync báo ATTEMPT_IN_PROGRESS thì redirect Take; ATTEMPT_EXPIRED hiển thị hết hạn. Service trả null cho đối tượng không có/không thuộc chủ. Submit trả null thì NotFound. POST thành công redirect GET; không báo thành công trước commit.

POST dùng antiforgery mặc định, [Authorize] trên PageModel. Login chỉ LocalRedirect returnUrl sau kiểm tra local. Không dùng GET để ghi/đăng xuất.

## Nghiệp vụ

- Tìm tên/bí danh bỏ dấu, đ→d, không phân biệt hoa thường, thu gọn khoảng trắng; khớp toàn bộ → tiền tố → chứa, rồi tên chuẩn hóa/ID.
- IS_A từ con tới cha. Sáu cạnh: thang→tứ giác; bình hành→thang; chữ nhật→bình hành; thoi→bình hành; vuông→chữ nhật; vuông→thoi. Không chu trình/tự nối. Hình thang có ít nhất một cặp cạnh đối song song.
- Compare mở chưa chọn gì chỉ hiển thị form; thiếu một ID/trùng ID báo lỗi; ID không công bố/không có trả NotFound.
- Đề đúng 10 câu không trùng, giữ phiên bản/thứ tự. Thiếu câu báo số hiện có và giữ form. Đúng=1, sai/trống=0; chấm server và commit nguyên tử; bộ đếm cộng bằng 10.
- Nộp lặp nội dung tương đương trả kết quả đã lưu; khác nội dung trả ATTEMPT_ALREADY_SUBMITTED. Item thiếu=null; item trùng/ngoài đề hoặc đáp án sai miền báo lỗi. Chỉ một kết quả commit khi nộp đồng thời.
- Hạn 24 giờ, hết hạn từ chối nộp/không lộ đáp án. Mọi GET/POST lượt kiểm tra chủ sở hữu ở server.

## SRS còn lại

Lịch sử/thống kê, quản trị kiến thức/câu hỏi/tài khoản và đổi mật khẩu chưa chốt trang trong tài liệu này. Bổ sung bằng templates/page-spec.md; không coi là đã hoàn thành hoặc bị loại khỏi SRS.
