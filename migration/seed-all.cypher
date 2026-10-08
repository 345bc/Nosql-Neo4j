// NẠP TOÀN BỘ DỮ LIỆU PHÁT TRIỂN — chạy một file, một lần Run.
// Neo4j Browser: chọn database ứng dụng, bật Enable multi statement query editor.
// Không chạy trong system. Không cần APOC hoặc đọc file bên ngoài.
// File gồm nhiều câu lệnh độc lập, không phải một transaction cho toàn bộ file.
// Bao gồm core, 60 câu luyện tập và 63 KnowledgeItem của Vỷ.
// Không nạp fixture cũ; tạo tài khoản demo_tuan, không tạo lượt làm giả.
// Theo yêu cầu demo: chuyển mọi status DRAFT sang PUBLISHED. Không tạo người/thời gian duyệt giả.
// Migration snapshot lượt cũ vẫn dùng --migrate-attempts trong ứng dụng.

// 1. CONSTRAINT / INDEX CHO CORE VÀ LUYỆN TẬP
CREATE CONSTRAINT shape_id_unique IF NOT EXISTS
FOR (s:Shape)
REQUIRE s.id IS UNIQUE;

CREATE CONSTRAINT formula_id_unique IF NOT EXISTS
FOR (f:Formula)
REQUIRE f.id IS UNIQUE;

CREATE CONSTRAINT user_id_unique IF NOT EXISTS
FOR (u:User) REQUIRE u.id IS UNIQUE;

CREATE CONSTRAINT user_username_unique IF NOT EXISTS
FOR (u:User) REQUIRE u.normalizedUsername IS UNIQUE;

CREATE CONSTRAINT attempt_id_unique IF NOT EXISTS
FOR (a:Attempt) REQUIRE a.id IS UNIQUE;

CREATE CONSTRAINT question_id_unique IF NOT EXISTS
FOR (q:Question) REQUIRE q.id IS UNIQUE;

CREATE CONSTRAINT question_version_id_unique IF NOT EXISTS
FOR (v:QuestionVersion) REQUIRE v.id IS UNIQUE;

CREATE CONSTRAINT attempt_item_id_unique IF NOT EXISTS
FOR (i:AttemptItem) REQUIRE i.id IS UNIQUE;

CREATE CONSTRAINT audit_event_id_unique IF NOT EXISTS
FOR (e:AuditEvent) REQUIRE e.id IS UNIQUE;

CREATE INDEX attempt_status_submitted IF NOT EXISTS
FOR (a:Attempt) ON (a.status, a.submittedAt);

CREATE CONSTRAINT knowledge_id_unique IF NOT EXISTS
FOR (k:KnowledgeItem) REQUIRE k.id IS UNIQUE;

// TÀI KHOẢN DEMO: demo_tuan. Mật khẩu xem migration/README.md.
// PasswordHasher Identity V3; không lưu mật khẩu thô. Không đặt lại tài khoản đã có.
MERGE (u:User {normalizedUsername: 'DEMO_TUAN'})
ON CREATE SET u.id = randomUUID(), u.username = 'demo_tuan',
    u.displayName = 'Tuấn demo', u.passwordHash = 'AQAAAAIAAYagAAAAEI71QCUofjC5vhnMQ2Lfz0lp3TUwH1WIo4nw8n2IBb7NywnHXIP9X3hS1ZtNHBcf5Q==',
    u.role = 'USER', u.status = 'ACTIVE', u.securityStamp = randomUUID(), u.isDemo = true
RETURN u.username AS demoUsername, u.status AS accountStatus;

// Chuyển tất cả node DRAFT hiện có sang PUBLISHED theo yêu cầu demo.
MATCH (n {status: 'DRAFT'})
SET n.status = 'PUBLISHED', n.demo = coalesce(n.demo, true)
RETURN count(n) AS promotedNodes;

// Tương thích nguồn cũ của Vỷ: chỉ điền sourceLocator khi còn trống.
MATCH (n) WHERE n.sourceRef IS NOT NULL AND trim(coalesce(n.sourceLocator, '')) = ''
SET n.sourceLocator = n.sourceRef;

// 2. SÁU HÌNH VÀ ĐỒ THỊ PHÂN LOẠI
UNWIND [
  {id: 'TU_GIAC', name: 'Tứ giác'},
  {id: 'HINH_THANG', name: 'Hình thang'},
  {id: 'HINH_BINH_HANH', name: 'Hình bình hành'},
  {id: 'HINH_CHU_NHAT', name: 'Hình chữ nhật'},
  {id: 'HINH_THOI', name: 'Hình thoi'},
  {id: 'HINH_VUONG', name: 'Hình vuông'}
] AS row
MERGE (s:Shape {id: row.id})
ON CREATE SET s.aliases = [], s.status = 'PUBLISHED', s.name = row.name, s.demo = true;


UNWIND [
  ['HINH_THANG', 'TU_GIAC'],
  ['HINH_BINH_HANH', 'HINH_THANG'],
  ['HINH_CHU_NHAT', 'HINH_BINH_HANH'],
  ['HINH_THOI', 'HINH_BINH_HANH'],
  ['HINH_VUONG', 'HINH_CHU_NHAT'],
  ['HINH_VUONG', 'HINH_THOI']
] AS pair
MATCH (child:Shape {id: pair[0]})
MATCH (parent:Shape {id: pair[1]})
MERGE (child)-[:IS_A]->(parent);

// 3. KIẾN THỨC CORE VÀ MƯỜI FORMULA — chỉ điền phần còn thiếu trên PUBLISHED
// Nội dung phát triển cần rà soát theo giáo trình; chưa được duyệt/công bố.
// Chạy sau schema.cypher và seed.cypher. Chỉ bổ sung trên hình PUBLISHED.
// Không thay nội dung của hình PUBLISHED hoặc ARCHIVED.
UNWIND [
  {id: 'TU_GIAC', definition: 'Đa giác đơn, lồi, không suy biến có bốn cạnh.', properties: ['Có bốn cạnh, bốn đỉnh và hai đường chéo.', 'Tổng bốn góc trong bằng 360°.'], recognitionSigns: ['Đa giác đơn, lồi có đúng bốn cạnh là tứ giác.'], examples: ['Tứ giác có ba góc 80°, 90°, 100° thì góc còn lại bằng 90°.'], formulas: [{id: 'TU_GIAC_PERIMETER', name: 'Chu vi', expression: 'P = a + b + c + d', variables: 'a, b, c, d: độ dài bốn cạnh; P cùng đơn vị độ dài.', conditions: 'Các cạnh dương, cùng đơn vị.'}]},
  {id: 'HINH_THANG', definition: 'Tứ giác có ít nhất một cặp cạnh đối song song.', properties: ['Hai góc kề cùng một cạnh bên bù nhau khi hai cạnh đối được chọn làm đáy song song.', 'Hình bình hành là trường hợp đặc biệt của hình thang theo quy ước này.'], recognitionSigns: ['Tứ giác có một cặp cạnh đối song song là hình thang.'], examples: ['Hai đáy dài 6 cm, 10 cm và chiều cao 4 cm thì diện tích bằng 32 cm².'], formulas: [{id: 'HINH_THANG_AREA', name: 'Diện tích', expression: 'S = (a + b) × h / 2', variables: 'a, b: hai đáy; h: khoảng cách giữa hai đường thẳng chứa đáy; S: diện tích.', conditions: 'a, b, h > 0; độ dài cùng đơn vị.'}]},
  {id: 'HINH_BINH_HANH', definition: 'Tứ giác có hai cặp cạnh đối song song.', properties: ['Các cạnh đối bằng nhau.', 'Các góc đối bằng nhau; hai góc kề bù nhau.', 'Hai đường chéo cắt nhau tại trung điểm mỗi đường.'], recognitionSigns: ['Tứ giác có hai cặp cạnh đối song song là hình bình hành.', 'Tứ giác có hai đường chéo cắt nhau tại trung điểm mỗi đường là hình bình hành.'], examples: ['Đáy 8 cm, chiều cao tương ứng 3 cm thì diện tích bằng 24 cm².'], formulas: [{id: 'HINH_BINH_HANH_PERIMETER', name: 'Chu vi', expression: 'P = 2(a + b)', variables: 'a, b: hai cạnh kề; P: chu vi.', conditions: 'a, b > 0; cùng đơn vị.'}, {id: 'HINH_BINH_HANH_AREA', name: 'Diện tích', expression: 'S = a × h', variables: 'a: đáy; h: chiều cao tương ứng.', conditions: 'a, h > 0; cùng đơn vị.'}]},
  {id: 'HINH_CHU_NHAT', definition: 'Tứ giác có bốn góc vuông.', properties: ['Có mọi tính chất của hình bình hành.', 'Hai đường chéo bằng nhau và cắt nhau tại trung điểm mỗi đường.'], recognitionSigns: ['Tứ giác có ba góc vuông là hình chữ nhật.', 'Hình bình hành có một góc vuông là hình chữ nhật.', 'Hình bình hành có hai đường chéo bằng nhau là hình chữ nhật.'], examples: ['Hai cạnh kề 3 cm, 4 cm thì chu vi bằng 14 cm và diện tích bằng 12 cm².'], formulas: [{id: 'HINH_CHU_NHAT_PERIMETER', name: 'Chu vi', expression: 'P = 2(a + b)', variables: 'a, b: hai cạnh kề.', conditions: 'a, b > 0; cùng đơn vị.'}, {id: 'HINH_CHU_NHAT_AREA', name: 'Diện tích', expression: 'S = a × b', variables: 'a, b: hai cạnh kề.', conditions: 'a, b > 0; cùng đơn vị.'}]},
  {id: 'HINH_THOI', definition: 'Tứ giác có bốn cạnh bằng nhau.', properties: ['Có mọi tính chất của hình bình hành.', 'Hai đường chéo vuông góc, cắt nhau tại trung điểm mỗi đường.', 'Mỗi đường chéo là phân giác hai góc tại các đỉnh mà nó đi qua.'], recognitionSigns: ['Tứ giác có bốn cạnh bằng nhau là hình thoi.', 'Hình bình hành có hai cạnh kề bằng nhau là hình thoi.', 'Hình bình hành có hai đường chéo vuông góc là hình thoi.'], examples: ['Hai đường chéo dài 6 cm, 8 cm thì diện tích bằng 24 cm².'], formulas: [{id: 'HINH_THOI_PERIMETER', name: 'Chu vi', expression: 'P = 4a', variables: 'a: cạnh hình thoi.', conditions: 'a > 0.'}, {id: 'HINH_THOI_AREA', name: 'Diện tích', expression: 'S = d1 × d2 / 2', variables: 'd1, d2: hai đường chéo.', conditions: 'd1, d2 > 0; cùng đơn vị.'}]},
  {id: 'HINH_VUONG', definition: 'Tứ giác có bốn cạnh bằng nhau và bốn góc vuông.', properties: ['Có mọi tính chất của hình chữ nhật và hình thoi.', 'Hai đường chéo bằng nhau, vuông góc và cắt nhau tại trung điểm mỗi đường.'], recognitionSigns: ['Hình chữ nhật có hai cạnh kề bằng nhau là hình vuông.', 'Hình thoi có một góc vuông là hình vuông.'], examples: ['Cạnh 4 cm thì chu vi bằng 16 cm và diện tích bằng 16 cm².'], formulas: [{id: 'HINH_VUONG_PERIMETER', name: 'Chu vi', expression: 'P = 4a', variables: 'a: cạnh hình vuông.', conditions: 'a > 0.'}, {id: 'HINH_VUONG_AREA', name: 'Diện tích', expression: 'S = a²', variables: 'a: cạnh hình vuông.', conditions: 'a > 0.'}]}
] AS row
MATCH (s:Shape {id: row.id})
WHERE s.status = 'PUBLISHED'
SET s.definition = CASE WHEN trim(coalesce(s.definition, '')) = '' THEN row.definition ELSE s.definition END,
    s.properties = CASE WHEN size(coalesce(s.properties, [])) = 0 THEN row.properties ELSE s.properties END,
    s.recognitionSigns = CASE WHEN size(coalesce(s.recognitionSigns, [])) = 0 THEN row.recognitionSigns ELSE s.recognitionSigns END,
    s.examples = CASE WHEN size(coalesce(s.examples, [])) = 0 THEN row.examples ELSE s.examples END,
    s.convention = CASE WHEN trim(coalesce(s.convention, '')) = '' THEN 'Chỉ xét tứ giác đơn, lồi, không suy biến. Hình thang có ít nhất một cặp cạnh đối song song.' ELSE s.convention END,
    s.reviewStatus = coalesce(s.reviewStatus, 'PENDING'),
    s.revision = coalesce(s.revision, 1)
WITH s, row
UNWIND row.formulas AS formula
MERGE (f:Formula {id: formula.id})
ON CREATE SET f.name = formula.name,
    f.expression = formula.expression,
    f.variables = formula.variables,
    f.conditions = formula.conditions,
    f.status = 'PUBLISHED', f.demo = true
MERGE (s)-[:HAS_FORMULA]->(f);

// 4. 60 CÂU LUYỆN TẬP — JSON ĐÃ ĐƯỢC NHÚNG THÀNH MAP CYPHER
UNWIND [
  {id: "Q_TG_001", versionId: "Q_TG_001_V1", topicId: "TU_GIAC", prompt: "Tứ giác lồi có ba góc 64°, 84° và 104°. Góc còn lại bằng bao nhiêu?", a: "108 °", b: "106 °", c: "107 °", d: "109 °", correctKey: "A", explanation: "Tổng bốn góc của tứ giác lồi là 360°. Góc còn lại bằng 360 − 64 − 84 − 104 = 108°."},
  {id: "Q_TG_002", versionId: "Q_TG_002_V1", topicId: "TU_GIAC", prompt: "Tứ giác có bốn cạnh 5, 6, 7, 8 cm. Chu vi bằng bao nhiêu?", a: "24 cm", b: "26 cm", c: "25 cm", d: "27 cm", correctKey: "B", explanation: "Cộng độ dài bốn cạnh: 5 + 6 + 7 + 8 = 26 cm."},
  {id: "Q_TG_003", versionId: "Q_TG_003_V1", topicId: "TU_GIAC", prompt: "Tứ giác lồi có ba góc 66°, 86° và 106°. Góc còn lại bằng bao nhiêu?", a: "100 °", b: "101 °", c: "102 °", d: "103 °", correctKey: "C", explanation: "Tổng bốn góc của tứ giác lồi là 360°. Góc còn lại bằng 360 − 66 − 86 − 106 = 102°."},
  {id: "Q_TG_004", versionId: "Q_TG_004_V1", topicId: "TU_GIAC", prompt: "Tứ giác có bốn cạnh 7, 8, 9, 10 cm. Chu vi bằng bao nhiêu?", a: "32 cm", b: "33 cm", c: "35 cm", d: "34 cm", correctKey: "D", explanation: "Cộng độ dài bốn cạnh: 7 + 8 + 9 + 10 = 34 cm."},
  {id: "Q_TG_005", versionId: "Q_TG_005_V1", topicId: "TU_GIAC", prompt: "Tứ giác lồi có ba góc 68°, 88° và 108°. Góc còn lại bằng bao nhiêu?", a: "96 °", b: "94 °", c: "95 °", d: "97 °", correctKey: "A", explanation: "Tổng bốn góc của tứ giác lồi là 360°. Góc còn lại bằng 360 − 68 − 88 − 108 = 96°."},
  {id: "Q_TG_006", versionId: "Q_TG_006_V1", topicId: "TU_GIAC", prompt: "Tứ giác có bốn cạnh 9, 10, 11, 12 cm. Chu vi bằng bao nhiêu?", a: "40 cm", b: "42 cm", c: "41 cm", d: "43 cm", correctKey: "B", explanation: "Cộng độ dài bốn cạnh: 9 + 10 + 11 + 12 = 42 cm."},
  {id: "Q_TG_007", versionId: "Q_TG_007_V1", topicId: "TU_GIAC", prompt: "Tứ giác lồi có ba góc 70°, 90° và 110°. Góc còn lại bằng bao nhiêu?", a: "88 °", b: "89 °", c: "90 °", d: "91 °", correctKey: "C", explanation: "Tổng bốn góc của tứ giác lồi là 360°. Góc còn lại bằng 360 − 70 − 90 − 110 = 90°."},
  {id: "Q_TG_008", versionId: "Q_TG_008_V1", topicId: "TU_GIAC", prompt: "Tứ giác có bốn cạnh 11, 12, 13, 14 cm. Chu vi bằng bao nhiêu?", a: "48 cm", b: "49 cm", c: "51 cm", d: "50 cm", correctKey: "D", explanation: "Cộng độ dài bốn cạnh: 11 + 12 + 13 + 14 = 50 cm."},
  {id: "Q_TG_009", versionId: "Q_TG_009_V1", topicId: "TU_GIAC", prompt: "Tứ giác lồi có ba góc 72°, 92° và 112°. Góc còn lại bằng bao nhiêu?", a: "84 °", b: "82 °", c: "83 °", d: "85 °", correctKey: "A", explanation: "Tổng bốn góc của tứ giác lồi là 360°. Góc còn lại bằng 360 − 72 − 92 − 112 = 84°."},
  {id: "Q_TG_010", versionId: "Q_TG_010_V1", topicId: "TU_GIAC", prompt: "Tứ giác có bốn cạnh 13, 14, 15, 16 cm. Chu vi bằng bao nhiêu?", a: "56 cm", b: "58 cm", c: "57 cm", d: "59 cm", correctKey: "B", explanation: "Cộng độ dài bốn cạnh: 13 + 14 + 15 + 16 = 58 cm."},
  {id: "Q_HT_001", versionId: "Q_HT_001_V1", topicId: "HINH_THANG", prompt: "Hình thang có hai đáy 4 và 6 cm, chiều cao 4 cm. Diện tích bằng bao nhiêu?", a: "20 cm²", b: "18 cm²", c: "19 cm²", d: "21 cm²", correctKey: "A", explanation: "Diện tích bằng (tổng hai đáy) × chiều cao / 2 = (4 + 6) × 4 / 2 = 20 cm²."},
  {id: "Q_HT_002", versionId: "Q_HT_002_V1", topicId: "HINH_THANG", prompt: "Hình thang có hai đáy 5 và 7 cm. Đường trung bình dài bao nhiêu?", a: "4 cm", b: "6 cm", c: "5 cm", d: "7 cm", correctKey: "B", explanation: "Đường trung bình bằng nửa tổng hai đáy: (5 + 7) / 2 = 6 cm."},
  {id: "Q_HT_003", versionId: "Q_HT_003_V1", topicId: "HINH_THANG", prompt: "Hình thang có hai đáy 6, 8 cm và hai cạnh bên cùng dài 6 cm. Chu vi bằng bao nhiêu?", a: "24 cm", b: "25 cm", c: "26 cm", d: "27 cm", correctKey: "C", explanation: "Chu vi là tổng bốn cạnh: 6 + 8 + 6 + 6 = 26 cm."},
  {id: "Q_HT_004", versionId: "Q_HT_004_V1", topicId: "HINH_THANG", prompt: "Hình thang có hai đáy 7 và 9 cm, chiều cao 7 cm. Diện tích bằng bao nhiêu?", a: "54 cm²", b: "55 cm²", c: "57 cm²", d: "56 cm²", correctKey: "D", explanation: "Diện tích bằng (tổng hai đáy) × chiều cao / 2 = (7 + 9) × 7 / 2 = 56 cm²."},
  {id: "Q_HT_005", versionId: "Q_HT_005_V1", topicId: "HINH_THANG", prompt: "Hình thang có hai đáy 8 và 10 cm. Đường trung bình dài bao nhiêu?", a: "9 cm", b: "7 cm", c: "8 cm", d: "10 cm", correctKey: "A", explanation: "Đường trung bình bằng nửa tổng hai đáy: (8 + 10) / 2 = 9 cm."},
  {id: "Q_HT_006", versionId: "Q_HT_006_V1", topicId: "HINH_THANG", prompt: "Hình thang có hai đáy 9, 11 cm và hai cạnh bên cùng dài 9 cm. Chu vi bằng bao nhiêu?", a: "36 cm", b: "38 cm", c: "37 cm", d: "39 cm", correctKey: "B", explanation: "Chu vi là tổng bốn cạnh: 9 + 11 + 9 + 9 = 38 cm."},
  {id: "Q_HT_007", versionId: "Q_HT_007_V1", topicId: "HINH_THANG", prompt: "Hình thang có hai đáy 10 và 12 cm, chiều cao 10 cm. Diện tích bằng bao nhiêu?", a: "108 cm²", b: "109 cm²", c: "110 cm²", d: "111 cm²", correctKey: "C", explanation: "Diện tích bằng (tổng hai đáy) × chiều cao / 2 = (10 + 12) × 10 / 2 = 110 cm²."},
  {id: "Q_HT_008", versionId: "Q_HT_008_V1", topicId: "HINH_THANG", prompt: "Hình thang có hai đáy 11 và 13 cm. Đường trung bình dài bao nhiêu?", a: "10 cm", b: "11 cm", c: "13 cm", d: "12 cm", correctKey: "D", explanation: "Đường trung bình bằng nửa tổng hai đáy: (11 + 13) / 2 = 12 cm."},
  {id: "Q_HT_009", versionId: "Q_HT_009_V1", topicId: "HINH_THANG", prompt: "Hình thang có hai đáy 12, 14 cm và hai cạnh bên cùng dài 12 cm. Chu vi bằng bao nhiêu?", a: "50 cm", b: "48 cm", c: "49 cm", d: "51 cm", correctKey: "A", explanation: "Chu vi là tổng bốn cạnh: 12 + 14 + 12 + 12 = 50 cm."},
  {id: "Q_HT_010", versionId: "Q_HT_010_V1", topicId: "HINH_THANG", prompt: "Hình thang có hai đáy 13 và 15 cm, chiều cao 13 cm. Diện tích bằng bao nhiêu?", a: "180 cm²", b: "182 cm²", c: "181 cm²", d: "183 cm²", correctKey: "B", explanation: "Diện tích bằng (tổng hai đáy) × chiều cao / 2 = (13 + 15) × 13 / 2 = 182 cm²."},
  {id: "Q_HBH_001", versionId: "Q_HBH_001_V1", topicId: "HINH_BINH_HANH", prompt: "Hình bình hành có đáy 6 cm và chiều cao tương ứng 4 cm. Diện tích bằng bao nhiêu?", a: "24 cm²", b: "22 cm²", c: "23 cm²", d: "25 cm²", correctKey: "A", explanation: "Diện tích bằng đáy × chiều cao tương ứng: 6 × 4 = 24 cm²."},
  {id: "Q_HBH_002", versionId: "Q_HBH_002_V1", topicId: "HINH_BINH_HANH", prompt: "Hình bình hành có hai cạnh kề 5 và 7 cm. Chu vi bằng bao nhiêu?", a: "22 cm", b: "24 cm", c: "23 cm", d: "25 cm", correctKey: "B", explanation: "Hai cặp cạnh đối bằng nhau nên chu vi = 2 × (5 + 7) = 24 cm."},
  {id: "Q_HBH_003", versionId: "Q_HBH_003_V1", topicId: "HINH_BINH_HANH", prompt: "Hình bình hành có một góc 56°. Góc kề với nó bằng bao nhiêu?", a: "122 °", b: "123 °", c: "124 °", d: "125 °", correctKey: "C", explanation: "Hai góc kề của hình bình hành bù nhau: 180 − 56 = 124°."},
  {id: "Q_HBH_004", versionId: "Q_HBH_004_V1", topicId: "HINH_BINH_HANH", prompt: "Hình bình hành có đáy 9 cm và chiều cao tương ứng 7 cm. Diện tích bằng bao nhiêu?", a: "61 cm²", b: "62 cm²", c: "64 cm²", d: "63 cm²", correctKey: "D", explanation: "Diện tích bằng đáy × chiều cao tương ứng: 9 × 7 = 63 cm²."},
  {id: "Q_HBH_005", versionId: "Q_HBH_005_V1", topicId: "HINH_BINH_HANH", prompt: "Hình bình hành có hai cạnh kề 8 và 10 cm. Chu vi bằng bao nhiêu?", a: "36 cm", b: "34 cm", c: "35 cm", d: "37 cm", correctKey: "A", explanation: "Hai cặp cạnh đối bằng nhau nên chu vi = 2 × (8 + 10) = 36 cm."},
  {id: "Q_HBH_006", versionId: "Q_HBH_006_V1", topicId: "HINH_BINH_HANH", prompt: "Hình bình hành có một góc 59°. Góc kề với nó bằng bao nhiêu?", a: "119 °", b: "121 °", c: "120 °", d: "122 °", correctKey: "B", explanation: "Hai góc kề của hình bình hành bù nhau: 180 − 59 = 121°."},
  {id: "Q_HBH_007", versionId: "Q_HBH_007_V1", topicId: "HINH_BINH_HANH", prompt: "Hình bình hành có đáy 12 cm và chiều cao tương ứng 10 cm. Diện tích bằng bao nhiêu?", a: "118 cm²", b: "119 cm²", c: "120 cm²", d: "121 cm²", correctKey: "C", explanation: "Diện tích bằng đáy × chiều cao tương ứng: 12 × 10 = 120 cm²."},
  {id: "Q_HBH_008", versionId: "Q_HBH_008_V1", topicId: "HINH_BINH_HANH", prompt: "Hình bình hành có hai cạnh kề 11 và 13 cm. Chu vi bằng bao nhiêu?", a: "46 cm", b: "47 cm", c: "49 cm", d: "48 cm", correctKey: "D", explanation: "Hai cặp cạnh đối bằng nhau nên chu vi = 2 × (11 + 13) = 48 cm."},
  {id: "Q_HBH_009", versionId: "Q_HBH_009_V1", topicId: "HINH_BINH_HANH", prompt: "Hình bình hành có một góc 62°. Góc kề với nó bằng bao nhiêu?", a: "118 °", b: "116 °", c: "117 °", d: "119 °", correctKey: "A", explanation: "Hai góc kề của hình bình hành bù nhau: 180 − 62 = 118°."},
  {id: "Q_HBH_010", versionId: "Q_HBH_010_V1", topicId: "HINH_BINH_HANH", prompt: "Hình bình hành có đáy 15 cm và chiều cao tương ứng 13 cm. Diện tích bằng bao nhiêu?", a: "193 cm²", b: "195 cm²", c: "194 cm²", d: "196 cm²", correctKey: "B", explanation: "Diện tích bằng đáy × chiều cao tương ứng: 15 × 13 = 195 cm²."},
  {id: "Q_HCN_001", versionId: "Q_HCN_001_V1", topicId: "HINH_CHU_NHAT", prompt: "Hình chữ nhật có hai cạnh 4 và 6 cm. Diện tích bằng bao nhiêu?", a: "24 cm²", b: "22 cm²", c: "23 cm²", d: "25 cm²", correctKey: "A", explanation: "Diện tích = dài × rộng = 6 × 4 = 24 cm²."},
  {id: "Q_HCN_002", versionId: "Q_HCN_002_V1", topicId: "HINH_CHU_NHAT", prompt: "Hình chữ nhật có hai cạnh 5 và 7 cm. Chu vi bằng bao nhiêu?", a: "22 cm", b: "24 cm", c: "23 cm", d: "25 cm", correctKey: "B", explanation: "Chu vi = 2 × (5 + 7) = 24 cm."},
  {id: "Q_HCN_003", versionId: "Q_HCN_003_V1", topicId: "HINH_CHU_NHAT", prompt: "Hình chữ nhật có hai cạnh 9 và 12 cm. Đường chéo dài bao nhiêu?", a: "13 cm", b: "14 cm", c: "15 cm", d: "16 cm", correctKey: "C", explanation: "Theo định lý Pythagore, d² = 9² + 12² = 225, do đó d = 15 cm."},
  {id: "Q_HCN_004", versionId: "Q_HCN_004_V1", topicId: "HINH_CHU_NHAT", prompt: "Hình chữ nhật có hai cạnh 7 và 9 cm. Diện tích bằng bao nhiêu?", a: "61 cm²", b: "62 cm²", c: "64 cm²", d: "63 cm²", correctKey: "D", explanation: "Diện tích = dài × rộng = 9 × 7 = 63 cm²."},
  {id: "Q_HCN_005", versionId: "Q_HCN_005_V1", topicId: "HINH_CHU_NHAT", prompt: "Hình chữ nhật có hai cạnh 8 và 10 cm. Chu vi bằng bao nhiêu?", a: "36 cm", b: "34 cm", c: "35 cm", d: "37 cm", correctKey: "A", explanation: "Chu vi = 2 × (8 + 10) = 36 cm."},
  {id: "Q_HCN_006", versionId: "Q_HCN_006_V1", topicId: "HINH_CHU_NHAT", prompt: "Hình chữ nhật có hai cạnh 18 và 24 cm. Đường chéo dài bao nhiêu?", a: "28 cm", b: "30 cm", c: "29 cm", d: "31 cm", correctKey: "B", explanation: "Theo định lý Pythagore, d² = 18² + 24² = 900, do đó d = 30 cm."},
  {id: "Q_HCN_007", versionId: "Q_HCN_007_V1", topicId: "HINH_CHU_NHAT", prompt: "Hình chữ nhật có hai cạnh 10 và 12 cm. Diện tích bằng bao nhiêu?", a: "118 cm²", b: "119 cm²", c: "120 cm²", d: "121 cm²", correctKey: "C", explanation: "Diện tích = dài × rộng = 12 × 10 = 120 cm²."},
  {id: "Q_HCN_008", versionId: "Q_HCN_008_V1", topicId: "HINH_CHU_NHAT", prompt: "Hình chữ nhật có hai cạnh 11 và 13 cm. Chu vi bằng bao nhiêu?", a: "46 cm", b: "47 cm", c: "49 cm", d: "48 cm", correctKey: "D", explanation: "Chu vi = 2 × (11 + 13) = 48 cm."},
  {id: "Q_HCN_009", versionId: "Q_HCN_009_V1", topicId: "HINH_CHU_NHAT", prompt: "Hình chữ nhật có hai cạnh 27 và 36 cm. Đường chéo dài bao nhiêu?", a: "45 cm", b: "43 cm", c: "44 cm", d: "46 cm", correctKey: "A", explanation: "Theo định lý Pythagore, d² = 27² + 36² = 2025, do đó d = 45 cm."},
  {id: "Q_HCN_010", versionId: "Q_HCN_010_V1", topicId: "HINH_CHU_NHAT", prompt: "Hình chữ nhật có hai cạnh 13 và 15 cm. Diện tích bằng bao nhiêu?", a: "193 cm²", b: "195 cm²", c: "194 cm²", d: "196 cm²", correctKey: "B", explanation: "Diện tích = dài × rộng = 15 × 13 = 195 cm²."},
  {id: "Q_HTH_001", versionId: "Q_HTH_001_V1", topicId: "HINH_THOI", prompt: "Hình thoi có hai đường chéo 8 và 10 cm. Diện tích bằng bao nhiêu?", a: "40 cm²", b: "38 cm²", c: "39 cm²", d: "41 cm²", correctKey: "A", explanation: "Diện tích = tích hai đường chéo / 2 = 8 × 10 / 2 = 40 cm²."},
  {id: "Q_HTH_002", versionId: "Q_HTH_002_V1", topicId: "HINH_THOI", prompt: "Hình thoi có cạnh 5 cm. Chu vi bằng bao nhiêu?", a: "18 cm", b: "20 cm", c: "19 cm", d: "21 cm", correctKey: "B", explanation: "Bốn cạnh bằng nhau: chu vi = 4 × 5 = 20 cm."},
  {id: "Q_HTH_003", versionId: "Q_HTH_003_V1", topicId: "HINH_THOI", prompt: "Hình thoi có một góc 66°. Góc kề với nó bằng bao nhiêu?", a: "112 °", b: "113 °", c: "114 °", d: "115 °", correctKey: "C", explanation: "Hình thoi là hình bình hành nên hai góc kề bù nhau: 180 − 66 = 114°."},
  {id: "Q_HTH_004", versionId: "Q_HTH_004_V1", topicId: "HINH_THOI", prompt: "Hình thoi có hai đường chéo 14 và 16 cm. Diện tích bằng bao nhiêu?", a: "110 cm²", b: "111 cm²", c: "113 cm²", d: "112 cm²", correctKey: "D", explanation: "Diện tích = tích hai đường chéo / 2 = 14 × 16 / 2 = 112 cm²."},
  {id: "Q_HTH_005", versionId: "Q_HTH_005_V1", topicId: "HINH_THOI", prompt: "Hình thoi có cạnh 8 cm. Chu vi bằng bao nhiêu?", a: "32 cm", b: "30 cm", c: "31 cm", d: "33 cm", correctKey: "A", explanation: "Bốn cạnh bằng nhau: chu vi = 4 × 8 = 32 cm."},
  {id: "Q_HTH_006", versionId: "Q_HTH_006_V1", topicId: "HINH_THOI", prompt: "Hình thoi có một góc 69°. Góc kề với nó bằng bao nhiêu?", a: "109 °", b: "111 °", c: "110 °", d: "112 °", correctKey: "B", explanation: "Hình thoi là hình bình hành nên hai góc kề bù nhau: 180 − 69 = 111°."},
  {id: "Q_HTH_007", versionId: "Q_HTH_007_V1", topicId: "HINH_THOI", prompt: "Hình thoi có hai đường chéo 20 và 22 cm. Diện tích bằng bao nhiêu?", a: "218 cm²", b: "219 cm²", c: "220 cm²", d: "221 cm²", correctKey: "C", explanation: "Diện tích = tích hai đường chéo / 2 = 20 × 22 / 2 = 220 cm²."},
  {id: "Q_HTH_008", versionId: "Q_HTH_008_V1", topicId: "HINH_THOI", prompt: "Hình thoi có cạnh 11 cm. Chu vi bằng bao nhiêu?", a: "42 cm", b: "43 cm", c: "45 cm", d: "44 cm", correctKey: "D", explanation: "Bốn cạnh bằng nhau: chu vi = 4 × 11 = 44 cm."},
  {id: "Q_HTH_009", versionId: "Q_HTH_009_V1", topicId: "HINH_THOI", prompt: "Hình thoi có một góc 72°. Góc kề với nó bằng bao nhiêu?", a: "108 °", b: "106 °", c: "107 °", d: "109 °", correctKey: "A", explanation: "Hình thoi là hình bình hành nên hai góc kề bù nhau: 180 − 72 = 108°."},
  {id: "Q_HTH_010", versionId: "Q_HTH_010_V1", topicId: "HINH_THOI", prompt: "Hình thoi có hai đường chéo 26 và 28 cm. Diện tích bằng bao nhiêu?", a: "362 cm²", b: "364 cm²", c: "363 cm²", d: "365 cm²", correctKey: "B", explanation: "Diện tích = tích hai đường chéo / 2 = 26 × 28 / 2 = 364 cm²."},
  {id: "Q_HV_001", versionId: "Q_HV_001_V1", topicId: "HINH_VUONG", prompt: "Hình vuông có cạnh 4 cm. Chu vi bằng bao nhiêu?", a: "16 cm", b: "14 cm", c: "15 cm", d: "17 cm", correctKey: "A", explanation: "Chu vi hình vuông bằng 4 lần cạnh: 4 × 4 = 16 cm."},
  {id: "Q_HV_002", versionId: "Q_HV_002_V1", topicId: "HINH_VUONG", prompt: "Hình vuông có cạnh 5 cm. Chu vi bằng bao nhiêu?", a: "18 cm", b: "20 cm", c: "19 cm", d: "21 cm", correctKey: "B", explanation: "Chu vi hình vuông bằng 4 lần cạnh: 4 × 5 = 20 cm."},
  {id: "Q_HV_003", versionId: "Q_HV_003_V1", topicId: "HINH_VUONG", prompt: "Hình vuông có cạnh 6 cm. Chu vi bằng bao nhiêu?", a: "22 cm", b: "23 cm", c: "24 cm", d: "25 cm", correctKey: "C", explanation: "Chu vi hình vuông bằng 4 lần cạnh: 4 × 6 = 24 cm."},
  {id: "Q_HV_004", versionId: "Q_HV_004_V1", topicId: "HINH_VUONG", prompt: "Hình vuông có cạnh 7 cm. Chu vi bằng bao nhiêu?", a: "26 cm", b: "27 cm", c: "29 cm", d: "28 cm", correctKey: "D", explanation: "Chu vi hình vuông bằng 4 lần cạnh: 4 × 7 = 28 cm."},
  {id: "Q_HV_005", versionId: "Q_HV_005_V1", topicId: "HINH_VUONG", prompt: "Hình vuông có cạnh 8 cm. Chu vi bằng bao nhiêu?", a: "32 cm", b: "30 cm", c: "31 cm", d: "33 cm", correctKey: "A", explanation: "Chu vi hình vuông bằng 4 lần cạnh: 4 × 8 = 32 cm."},
  {id: "Q_HV_006", versionId: "Q_HV_006_V1", topicId: "HINH_VUONG", prompt: "Hình vuông có cạnh 9 cm. Chu vi bằng bao nhiêu?", a: "34 cm", b: "36 cm", c: "35 cm", d: "37 cm", correctKey: "B", explanation: "Chu vi hình vuông bằng 4 lần cạnh: 4 × 9 = 36 cm."},
  {id: "Q_HV_007", versionId: "Q_HV_007_V1", topicId: "HINH_VUONG", prompt: "Hình vuông có cạnh 10 cm. Chu vi bằng bao nhiêu?", a: "38 cm", b: "39 cm", c: "40 cm", d: "41 cm", correctKey: "C", explanation: "Chu vi hình vuông bằng 4 lần cạnh: 4 × 10 = 40 cm."},
  {id: "Q_HV_008", versionId: "Q_HV_008_V1", topicId: "HINH_VUONG", prompt: "Hình vuông có cạnh 11 cm. Chu vi bằng bao nhiêu?", a: "42 cm", b: "43 cm", c: "45 cm", d: "44 cm", correctKey: "D", explanation: "Chu vi hình vuông bằng 4 lần cạnh: 4 × 11 = 44 cm."},
  {id: "Q_HV_009", versionId: "Q_HV_009_V1", topicId: "HINH_VUONG", prompt: "Hình vuông có cạnh 12 cm. Chu vi bằng bao nhiêu?", a: "48 cm", b: "46 cm", c: "47 cm", d: "49 cm", correctKey: "A", explanation: "Chu vi hình vuông bằng 4 lần cạnh: 4 × 12 = 48 cm."},
  {id: "Q_HV_010", versionId: "Q_HV_010_V1", topicId: "HINH_VUONG", prompt: "Hình vuông có cạnh 13 cm. Chu vi bằng bao nhiêu?", a: "50 cm", b: "52 cm", c: "51 cm", d: "53 cm", correctKey: "B", explanation: "Chu vi hình vuông bằng 4 lần cạnh: 4 × 13 = 52 cm."}
] AS row
MATCH (s:Shape {id: row.topicId})
MERGE (q:Question {id: row.id})
MERGE (v:QuestionVersion {id: row.versionId})
ON CREATE SET v.version = 1, v.prompt = row.prompt,
    v.optionA = row.a, v.optionB = row.b, v.optionC = row.c, v.optionD = row.d,
    v.correctKey = row.correctKey, v.explanation = row.explanation,
    v.status = 'PUBLISHED', v.reviewStatus = 'PENDING', v.demo = true
MERGE (q)-[:HAS_VERSION]->(v)
FOREACH (_ IN CASE WHEN v.status = 'PUBLISHED' AND NOT EXISTS { MATCH (v)-[:ABOUT]->() }
    THEN [1] ELSE [] END | MERGE (v)-[:ABOUT]->(s))
WITH q, v WHERE NOT EXISTS { MATCH (q)-[:CURRENT]->() }
MERGE (q)-[:CURRENT]->(v);

// 5. PHẦN MỚI CỦA VỶ — chỉ gắn KnowledgeItem vào Shape PUBLISHED
WITH [
  {id: "TU_GIAC", code: "TuGiac", aliases: ["tứ giác lồi", "quadrilateral"], imageUrl: "/images/tu_giac.svg"},
  {id: "HINH_THANG", code: "HinhThang", aliases: ["trapezoid"], imageUrl: "/images/hinh_thang.svg"},
  {id: "HINH_BINH_HANH", code: "HinhBinhHanh", aliases: ["bình hành", "parallelogram"], imageUrl: "/images/hinh_binh_hanh.svg"},
  {id: "HINH_CHU_NHAT", code: "HinhChuNhat", aliases: ["chữ nhật", "rectangle"], imageUrl: "/images/hinh_chu_nhat.svg"},
  {id: "HINH_THOI", code: "HinhThoi", aliases: ["thoi", "rhombus"], imageUrl: "/images/hinh_thoi.svg"},
  {id: "HINH_VUONG", code: "HinhVuong", aliases: ["vuông", "square"], imageUrl: "/images/hinh_vuong.svg"}
] AS shapes, [
  {id: "TuGiac_DEF_01", type: "DEFINITION", title: "Định nghĩa tứ giác", content: "Tứ giác lồi là đa giác đơn có bốn cạnh, bốn đỉnh và mọi góc trong nhỏ hơn 180 độ.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "TU_GIAC", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#tu-giac", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "TuGiac_PROP_01", type: "PROPERTY", title: "Tính chất 1", content: "Có bốn cạnh và bốn đỉnh.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "TU_GIAC", propertyCode: "FOUR_SIDES_VERTICES", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#tu-giac", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "TuGiac_PROP_02", type: "PROPERTY", title: "Tính chất 2", content: "Tổng bốn góc trong bằng 360 độ.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "TU_GIAC", propertyCode: "ANGLE_SUM_360", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#tu-giac", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "TuGiac_PROP_03", type: "PROPERTY", title: "Tính chất 3", content: "Có hai đường chéo nối các cặp đỉnh không kề nhau.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "TU_GIAC", propertyCode: "TWO_DIAGONALS", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#tu-giac", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThang_DEF_01", type: "DEFINITION", title: "Định nghĩa hình thang", content: "Hình thang là tứ giác có ít nhất một cặp cạnh đối song song.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_THANG", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thang", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThang_PROP_01", type: "PROPERTY", title: "Tính chất 1", content: "Có ít nhất một cặp cạnh đối song song.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_THANG", propertyCode: "AT_LEAST_ONE_PARALLEL_PAIR", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thang", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThang_PROP_02", type: "PROPERTY", title: "Tính chất 2", content: "Hai góc trong kề một cạnh bên của hình thang có tổng bằng 180 độ.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_THANG", propertyCode: "LEG_ANGLES_SUPPLEMENTARY", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thang", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhBinhHanh_DEF_01", type: "DEFINITION", title: "Định nghĩa hình bình hành", content: "Hình bình hành là tứ giác có hai cặp cạnh đối song song.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_BINH_HANH", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-binh-hanh", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhBinhHanh_PROP_01", type: "PROPERTY", title: "Tính chất 1", content: "Hai cặp cạnh đối song song.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_BINH_HANH", propertyCode: "TWO_PARALLEL_PAIRS", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-binh-hanh", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhBinhHanh_PROP_02", type: "PROPERTY", title: "Tính chất 2", content: "Các cạnh đối bằng nhau.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_BINH_HANH", propertyCode: "OPPOSITE_SIDES_EQUAL", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-binh-hanh", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhBinhHanh_PROP_03", type: "PROPERTY", title: "Tính chất 3", content: "Các góc đối bằng nhau.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_BINH_HANH", propertyCode: "OPPOSITE_ANGLES_EQUAL", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-binh-hanh", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhBinhHanh_PROP_04", type: "PROPERTY", title: "Tính chất 4", content: "Hai góc kề có tổng bằng 180 độ.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_BINH_HANH", propertyCode: "ADJACENT_ANGLES_SUPPLEMENTARY", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-binh-hanh", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhBinhHanh_PROP_05", type: "PROPERTY", title: "Tính chất 5", content: "Hai đường chéo cắt nhau tại trung điểm của mỗi đường.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_BINH_HANH", propertyCode: "DIAGONALS_BISECT_EACH_OTHER", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-binh-hanh", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhChuNhat_DEF_01", type: "DEFINITION", title: "Định nghĩa hình chữ nhật", content: "Hình chữ nhật là tứ giác có bốn góc vuông.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_CHU_NHAT", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-chu-nhat", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhChuNhat_PROP_01", type: "PROPERTY", title: "Tính chất 1", content: "Bốn góc đều bằng 90 độ.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_CHU_NHAT", propertyCode: "FOUR_RIGHT_ANGLES", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-chu-nhat", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhChuNhat_PROP_02", type: "PROPERTY", title: "Tính chất 2", content: "Hai đường chéo bằng nhau.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_CHU_NHAT", propertyCode: "DIAGONALS_EQUAL", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-chu-nhat", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThoi_DEF_01", type: "DEFINITION", title: "Định nghĩa hình thoi", content: "Hình thoi là tứ giác có bốn cạnh bằng nhau.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_THOI", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thoi", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThoi_PROP_01", type: "PROPERTY", title: "Tính chất 1", content: "Bốn cạnh bằng nhau.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_THOI", propertyCode: "FOUR_EQUAL_SIDES", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thoi", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThoi_PROP_02", type: "PROPERTY", title: "Tính chất 2", content: "Hai đường chéo vuông góc với nhau.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_THOI", propertyCode: "DIAGONALS_PERPENDICULAR", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thoi", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThoi_PROP_03", type: "PROPERTY", title: "Tính chất 3", content: "Mỗi đường chéo là đường phân giác của hai góc đối mà nó đi qua.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_THOI", propertyCode: "DIAGONALS_BISECT_ANGLES", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thoi", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhVuong_DEF_01", type: "DEFINITION", title: "Định nghĩa hình vuông", content: "Hình vuông là tứ giác có bốn cạnh bằng nhau và bốn góc vuông.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_VUONG", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-vuong", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhVuong_PROP_01", type: "PROPERTY", title: "Tính chất 1", content: "Bốn cạnh bằng nhau.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_VUONG", propertyCode: "FOUR_EQUAL_SIDES", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-vuong", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhVuong_PROP_02", type: "PROPERTY", title: "Tính chất 2", content: "Bốn góc đều bằng 90 độ.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", shapeId: "HINH_VUONG", propertyCode: "FOUR_RIGHT_ANGLES", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-vuong", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "TuGiac_FORM_01", type: "FORMULA", title: "Chu vi", content: "P = a + b + c + d", expression: "P = a + b + c + d", variables: ["a,b,c,d: độ dài bốn cạnh"], conditions: "Tứ giác đơn không suy biến; các cạnh cùng đơn vị.", unit: "Đơn vị độ dài", shapeId: "TU_GIAC", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#tu-giac", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "TuGiac_FORM_02", type: "FORMULA", title: "Diện tích theo đường chéo", content: "S = d1 * d2 * sin(phi) / 2", expression: "S = d1 * d2 * sin(phi) / 2", variables: ["d1,d2: độ dài hai đường chéo", "phi: góc giữa hai đường chéo, tính bằng radian khi dùng hàm sin"], conditions: "Tứ giác lồi không suy biến; d1,d2 > 0; 0 < phi < pi; độ dài cùng đơn vị.", unit: "Đơn vị diện tích", shapeId: "TU_GIAC", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#tu-giac", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThang_FORM_01", type: "FORMULA", title: "Chu vi", content: "P = a + b + c + d", expression: "P = a + b + c + d", variables: ["a,b: độ dài hai đáy", "c,d: độ dài hai cạnh bên"], conditions: "Hai đáy song song; các độ dài cùng đơn vị.", unit: "Đơn vị độ dài", shapeId: "HINH_THANG", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thang", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThang_FORM_02", type: "FORMULA", title: "Diện tích", content: "S = (a + b) * h / 2", expression: "S = (a + b) * h / 2", variables: ["a,b: độ dài hai đáy", "h: khoảng cách vuông góc giữa hai đáy"], conditions: "a,b,h > 0; các độ dài cùng đơn vị.", unit: "Đơn vị diện tích", shapeId: "HINH_THANG", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thang", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhBinhHanh_FORM_01", type: "FORMULA", title: "Chu vi", content: "P = 2 * (a + b)", expression: "P = 2 * (a + b)", variables: ["a,b: độ dài hai cạnh kề"], conditions: "a,b > 0; các độ dài cùng đơn vị.", unit: "Đơn vị độ dài", shapeId: "HINH_BINH_HANH", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-binh-hanh", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhBinhHanh_FORM_02", type: "FORMULA", title: "Diện tích", content: "S = a * h", expression: "S = a * h", variables: ["a: độ dài cạnh chọn làm đáy", "h: chiều cao vuông góc ứng với đáy a"], conditions: "a,h > 0; các độ dài cùng đơn vị.", unit: "Đơn vị diện tích", shapeId: "HINH_BINH_HANH", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-binh-hanh", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhChuNhat_FORM_01", type: "FORMULA", title: "Chu vi", content: "P = 2 * (a + b)", expression: "P = 2 * (a + b)", variables: ["a,b: độ dài hai cạnh kề"], conditions: "a,b > 0; các độ dài cùng đơn vị.", unit: "Đơn vị độ dài", shapeId: "HINH_CHU_NHAT", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-chu-nhat", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhChuNhat_FORM_02", type: "FORMULA", title: "Diện tích", content: "S = a * b", expression: "S = a * b", variables: ["a,b: độ dài hai cạnh kề"], conditions: "a,b > 0; các độ dài cùng đơn vị.", unit: "Đơn vị diện tích", shapeId: "HINH_CHU_NHAT", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-chu-nhat", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThoi_FORM_01", type: "FORMULA", title: "Chu vi", content: "P = 4 * a", expression: "P = 4 * a", variables: ["a: độ dài một cạnh"], conditions: "a > 0.", unit: "Đơn vị độ dài", shapeId: "HINH_THOI", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thoi", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThoi_FORM_02", type: "FORMULA", title: "Diện tích theo đáy và chiều cao", content: "S = a * h", expression: "S = a * h", variables: ["a: độ dài cạnh chọn làm đáy", "h: chiều cao vuông góc ứng với đáy a"], conditions: "a,h > 0; các độ dài cùng đơn vị.", unit: "Đơn vị diện tích", shapeId: "HINH_THOI", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thoi", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhThoi_FORM_03", type: "FORMULA", title: "Diện tích theo đường chéo", content: "S = d1 * d2 / 2", expression: "S = d1 * d2 / 2", variables: ["d1,d2: độ dài hai đường chéo"], conditions: "d1,d2 > 0; các đường chéo vuông góc; độ dài cùng đơn vị.", unit: "Đơn vị diện tích", shapeId: "HINH_THOI", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thoi", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhVuong_FORM_01", type: "FORMULA", title: "Chu vi", content: "P = 4 * a", expression: "P = 4 * a", variables: ["a: độ dài một cạnh"], conditions: "a > 0.", unit: "Đơn vị độ dài", shapeId: "HINH_VUONG", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-vuong", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhVuong_FORM_02", type: "FORMULA", title: "Diện tích theo cạnh", content: "S = a * a", expression: "S = a * a", variables: ["a: độ dài một cạnh"], conditions: "a > 0.", unit: "Đơn vị diện tích", shapeId: "HINH_VUONG", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-vuong", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HinhVuong_FORM_03", type: "FORMULA", title: "Diện tích theo đường chéo", content: "S = d * d / 2", expression: "S = d * d / 2", variables: ["d: độ dài một đường chéo"], conditions: "d > 0.", unit: "Đơn vị diện tích", shapeId: "HINH_VUONG", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-vuong", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "TU_GIAC_RECOGNITION_01", shapeId: "TU_GIAC", type: "RECOGNITION", title: "Dấu hiệu nhận biết", content: "Đa giác đơn có bốn cạnh và mọi góc trong nhỏ hơn 180 độ là tứ giác lồi.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#tu-giac", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "TU_GIAC_EXAMPLE_01", shapeId: "TU_GIAC", type: "EXAMPLE", title: "Ví dụ", content: "Tứ giác lồi có bốn cạnh 3 cm, 4 cm, 5 cm, 6 cm: chu vi P = 3 + 4 + 5 + 6 = 18 cm.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#tu-giac", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_THANG_RECOGNITION_01", shapeId: "HINH_THANG", type: "RECOGNITION", title: "Dấu hiệu nhận biết", content: "Tứ giác có ít nhất một cặp cạnh đối song song là hình thang.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thang", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_THANG_EXAMPLE_01", shapeId: "HINH_THANG", type: "EXAMPLE", title: "Ví dụ", content: "Hai đáy 6 cm và 10 cm, chiều cao 4 cm: S = (6 + 10) × 4 / 2 = 32 cm².", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thang", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_BINH_HANH_RECOGNITION_01", shapeId: "HINH_BINH_HANH", type: "RECOGNITION", title: "Dấu hiệu nhận biết", content: "Tứ giác có hai cặp cạnh đối song song; hoặc có hai đường chéo cắt nhau tại trung điểm mỗi đường là hình bình hành.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-binh-hanh", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_BINH_HANH_EXAMPLE_01", shapeId: "HINH_BINH_HANH", type: "EXAMPLE", title: "Ví dụ", content: "Đáy 8 cm, chiều cao ứng với đáy 5 cm: S = 8 × 5 = 40 cm².", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-binh-hanh", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_CHU_NHAT_RECOGNITION_01", shapeId: "HINH_CHU_NHAT", type: "RECOGNITION", title: "Dấu hiệu nhận biết", content: "Hình bình hành có một góc vuông; hoặc có hai đường chéo bằng nhau là hình chữ nhật.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-chu-nhat", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_CHU_NHAT_EXAMPLE_01", shapeId: "HINH_CHU_NHAT", type: "EXAMPLE", title: "Ví dụ", content: "Chiều dài 8 cm, chiều rộng 5 cm: P = 2 × (8 + 5) = 26 cm; S = 8 × 5 = 40 cm².", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-chu-nhat", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_THOI_RECOGNITION_01", shapeId: "HINH_THOI", type: "RECOGNITION", title: "Dấu hiệu nhận biết", content: "Hình bình hành có hai cạnh kề bằng nhau; hoặc có hai đường chéo vuông góc là hình thoi.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thoi", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_THOI_EXAMPLE_01", shapeId: "HINH_THOI", type: "EXAMPLE", title: "Ví dụ", content: "Hai đường chéo 6 cm và 8 cm: S = 6 × 8 / 2 = 24 cm².", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-thoi", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_VUONG_RECOGNITION_01", shapeId: "HINH_VUONG", type: "RECOGNITION", title: "Dấu hiệu nhận biết", content: "Hình chữ nhật có hai cạnh kề bằng nhau; hoặc hình thoi có một góc vuông là hình vuông.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-vuong", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_VUONG_EXAMPLE_01", shapeId: "HINH_VUONG", type: "EXAMPLE", title: "Ví dụ", content: "Cạnh 5 cm: P = 4 × 5 = 20 cm; S = 5 × 5 = 25 cm².", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — kiến thức mẫu v1 (chưa được giảng viên phê duyệt)", sourceLocator: "docs/KNOWLEDGE.md#hinh-vuong", reviewStatus: "PENDING_TEACHER_REVIEW"}
] AS items
OPTIONAL MATCH (s:Shape) WHERE s.id IN [row IN shapes | row.id]
WITH shapes, items, collect(s) AS existing
WHERE size(existing) = size(shapes)
UNWIND shapes AS row
MATCH (s:Shape {id: row.id})
WHERE s.status = 'PUBLISHED'
SET s.code = CASE WHEN trim(coalesce(s.code, '')) = '' THEN row.code ELSE s.code END,
    s.imageUrl = CASE WHEN trim(coalesce(s.imageUrl, '')) = '' THEN row.imageUrl ELSE s.imageUrl END,
    s.aliases = CASE WHEN size(coalesce(s.aliases, [])) = 0 THEN row.aliases ELSE s.aliases END
WITH items, count(s) AS shapesMatched
UNWIND items AS row
MATCH (s:Shape {id: row.shapeId})
WHERE s.status = 'PUBLISHED'
MERGE (k:KnowledgeItem {id: row.id})
ON CREATE SET k = row, k.status = 'PUBLISHED', k.demo = true, k.createdAt = datetime()
MERGE (s)-[:HAS_KNOWLEDGE]->(k)
RETURN shapesMatched, count(DISTINCT k) AS knowledgeItemsMatched;


// BỔ SUNG: 12 ví dụ (hai mỗi hình), hai công thức.
UNWIND [
  {id: "TU_GIAC_EXAMPLE_02", shapeId: "TU_GIAC", type: "EXAMPLE", title: "Ví dụ bổ sung 1", content: "Ba góc của tứ giác lồi là 70°, 80°, 110°: góc còn lại = 360° − 70° − 80° − 110° = 100°.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "TU_GIAC_EXAMPLE_03", shapeId: "TU_GIAC", type: "EXAMPLE", title: "Ví dụ bổ sung 2", content: "Bốn cạnh dài 5, 6, 7, 8 cm: chu vi P = 5 + 6 + 7 + 8 = 26 cm.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_THANG_EXAMPLE_02", shapeId: "HINH_THANG", type: "EXAMPLE", title: "Ví dụ bổ sung 1", content: "Hai đáy 6 cm, 10 cm: đường trung bình m = (6 + 10)/2 = 8 cm.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_THANG_EXAMPLE_03", shapeId: "HINH_THANG", type: "EXAMPLE", title: "Ví dụ bổ sung 2", content: "Hai đáy 8 cm, 12 cm, chiều cao 5 cm: diện tích S = (8 + 12) × 5/2 = 50 cm².", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_BINH_HANH_EXAMPLE_02", shapeId: "HINH_BINH_HANH", type: "EXAMPLE", title: "Ví dụ bổ sung 1", content: "Hai cạnh kề 6 cm, 4 cm: chu vi P = 2 × (6 + 4) = 20 cm.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_BINH_HANH_EXAMPLE_03", shapeId: "HINH_BINH_HANH", type: "EXAMPLE", title: "Ví dụ bổ sung 2", content: "Đáy 9 cm, chiều cao tương ứng 4 cm: diện tích S = 9 × 4 = 36 cm².", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_CHU_NHAT_EXAMPLE_02", shapeId: "HINH_CHU_NHAT", type: "EXAMPLE", title: "Ví dụ bổ sung 1", content: "Hai cạnh 3 cm và 4 cm: đường chéo d = √(3² + 4²) = 5 cm.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_CHU_NHAT_EXAMPLE_03", shapeId: "HINH_CHU_NHAT", type: "EXAMPLE", title: "Ví dụ bổ sung 2", content: "Chiều dài 12 cm, chiều rộng 5 cm: diện tích S = 12 × 5 = 60 cm².", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_THOI_EXAMPLE_02", shapeId: "HINH_THOI", type: "EXAMPLE", title: "Ví dụ bổ sung 1", content: "Cạnh 7 cm: chu vi P = 4 × 7 = 28 cm.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_THOI_EXAMPLE_03", shapeId: "HINH_THOI", type: "EXAMPLE", title: "Ví dụ bổ sung 2", content: "Hai đường chéo 10 cm và 12 cm: diện tích S = 10 × 12/2 = 60 cm².", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_VUONG_EXAMPLE_02", shapeId: "HINH_VUONG", type: "EXAMPLE", title: "Ví dụ bổ sung 1", content: "Cạnh 6 cm: đường chéo d = 6√2 cm.", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HINH_VUONG_EXAMPLE_03", shapeId: "HINH_VUONG", type: "EXAMPLE", title: "Ví dụ bổ sung 2", content: "Đường chéo 10 cm: diện tích S = 10²/2 = 50 cm².", expression: "", variables: [], conditions: "Tứ giác đơn, lồi, không suy biến; độ dài cùng đơn vị.", unit: "", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — ví dụ bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HCN_DIAGONAL_EXTRA", shapeId: "HINH_CHU_NHAT", type: "FORMULA", title: "Đường chéo hình chữ nhật", content: "d = sqrt(a*a + b*b)", expression: "d = sqrt(a*a + b*b)", variables: ["a, b: hai cạnh kề", "d: độ dài đường chéo"], conditions: "a, b > 0; cùng đơn vị độ dài.", unit: "Đơn vị độ dài", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — công thức bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"},
  {id: "HT_MIDLINE_EXTRA", shapeId: "HINH_THANG", type: "FORMULA", title: "Đường trung bình hình thang", content: "m = (a + b) / 2", expression: "m = (a + b) / 2", variables: ["a, b: hai đáy", "m: độ dài đường trung bình"], conditions: "a, b > 0; cùng đơn vị độ dài.", unit: "Đơn vị độ dài", propertyCode: "", sourceTitle: "Nhóm dự án — nội dung mẫu bổ sung, chưa được giảng viên duyệt", sourceLocator: "migration/seed-all.cypher — công thức bổ sung", reviewStatus: "PENDING_TEACHER_REVIEW"}
] AS row
MATCH (s:Shape {id: row.shapeId, status: 'PUBLISHED'})
MERGE (k:KnowledgeItem {id: row.id})
ON CREATE SET k = row, k.status = 'PUBLISHED', k.demo = true, k.createdAt = datetime()
MERGE (s)-[:HAS_KNOWLEDGE]->(k)
WITH s, k, row
FOREACH (_ IN CASE WHEN row.type = 'FORMULA' THEN [1] ELSE [] END |
    MERGE (f:Formula {id: row.id})
    ON CREATE SET f.name = row.title, f.expression = row.expression,
        f.variables = reduce(text = '', v IN row.variables | text + CASE WHEN text = '' THEN '' ELSE '; ' END + v),
        f.conditions = row.conditions, f.status = 'PUBLISHED', f.demo = true,
        f.reviewStatus = row.reviewStatus, f.sourceTitle = row.sourceTitle, f.sourceLocator = row.sourceLocator
    MERGE (s)-[:HAS_FORMULA]->(f))
RETURN count(DISTINCT k) AS supplementalKnowledgeItems;

// 6. KẾT QUẢ CUỐI — database mới: 6 / 6 / 12 / 60 / 60 / 63 / 63.
CALL { MATCH (s:Shape) RETURN count(s) AS shapes }
CALL { MATCH (:Shape)-[r:IS_A]->(:Shape) RETURN count(r) AS classificationEdges }
CALL { MATCH (f:Formula) RETURN count(f) AS formulas }
CALL { MATCH (q:Question) RETURN count(q) AS questions }
CALL { MATCH (v:QuestionVersion) RETURN count(v) AS questionVersions }
CALL { MATCH (k:KnowledgeItem) RETURN count(k) AS knowledgeItems }
CALL { MATCH (:Shape)-[r:HAS_KNOWLEDGE]->(:KnowledgeItem) RETURN count(r) AS knowledgeLinks }
RETURN shapes, classificationEdges, formulas, questions, questionVersions, knowledgeItems, knowledgeLinks;

// 7. KIỂM TRA CẤU TRÚC LUYỆN TẬP (các bảng lỗi phải rỗng).
// Read-only verification. Counts alone do not imply content approval.
MATCH (s:Shape) OPTIONAL MATCH (v:QuestionVersion)-[:ABOUT]->(s)
RETURN s.id AS topic, s.status AS status, count(v) AS versions ORDER BY topic;

MATCH (q:Question) OPTIONAL MATCH (q)-[:CURRENT]->(v:QuestionVersion)
WITH q, count(v) AS versions WHERE versions <> 1
RETURN q.id AS invalidCurrent, versions;

MATCH (v:QuestionVersion) OPTIONAL MATCH (v)-[:ABOUT]->(s:Shape)
WITH v, count(s) AS topics WHERE topics <> 1
RETURN v.id AS invalidTopic, topics;

MATCH (v:QuestionVersion)
WHERE NOT coalesce(v.correctKey, '') IN ['A','B','C','D']
   OR trim(coalesce(v.prompt, '')) = '' OR trim(coalesce(v.explanation, '')) = ''
   OR any(x IN [v.optionA, v.optionB, v.optionC, v.optionD] WHERE trim(coalesce(x, '')) = '')
   OR size([x IN [v.optionA,v.optionB,v.optionC,v.optionD] WHERE
       single(y IN [v.optionA,v.optionB,v.optionC,v.optionD] WHERE trim(x) = trim(y))]) <> 4
RETURN v.id AS invalidQuestion;

MATCH (s:Shape) WHERE EXISTS { MATCH (s)-[:IS_A*1..6]->(s) }
RETURN s.id AS cycle;

MATCH (a:Attempt) OPTIONAL MATCH (a)-[:HAS_ITEM]->(i:AttemptItem)
WITH a, count(i) AS items WHERE items <> 10
RETURN a.id AS invalidAttemptItems, items;

MATCH (a:Attempt) OPTIONAL MATCH (u:User)-[:STARTED]->(a)
WITH a, count(u) AS owners WHERE owners <> 1
RETURN a.id AS invalidAttemptOwner, owners;

MATCH (a:Attempt {status: 'SUBMITTED'})-[:HAS_ITEM]->(i:AttemptItem)
WITH a, sum(CASE WHEN i.isCorrect THEN 1 ELSE 0 END) AS itemScore
WHERE a.score <> itemScore OR a.submittedAt IS NULL
RETURN a.id AS invalidScore, a.score AS storedScore, itemScore;

MATCH (v:QuestionVersion {status: 'PUBLISHED'})
WHERE coalesce(v.demo, false) = false AND ( trim(coalesce(v.sourceTitle, '')) = '' OR trim(coalesce(v.sourceLocator, '')) = ''
   OR trim(coalesce(v.reviewedBy, '')) = '' OR v.reviewedAt IS NULL)
RETURN v.id AS missingPublicationReview;

MATCH (s:Shape {status: 'PUBLISHED'})
WHERE coalesce(s.demo, false) = false AND ( trim(coalesce(s.sourceTitle, '')) = '' OR trim(coalesce(s.sourceLocator, '')) = ''
   OR trim(coalesce(s.reviewedBy, '')) = '' OR s.reviewedAt IS NULL)
RETURN s.id AS missingPublicationReview;

// Hiển thị nhãn chưa duyệt cho nội dung mẫu PUBLISHED; không tạo người duyệt giả.
MATCH (n) WHERE (n:Shape OR n:KnowledgeItem OR n:QuestionVersion OR n:Formula)
    AND n.status = 'PUBLISHED' AND n.demo = true
RETURN labels(n) AS labels, count(*) AS publishedDemoNodes;
