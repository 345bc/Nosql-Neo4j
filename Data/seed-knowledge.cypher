// Nội dung phát triển cần rà soát theo giáo trình; chưa được duyệt/công bố.
// Chạy sau schema.cypher và seed.cypher. Chỉ bổ sung trên hình DRAFT.
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
WHERE s.status = 'DRAFT'
SET s.definition = row.definition,
    s.properties = row.properties,
    s.recognitionSigns = row.recognitionSigns,
    s.examples = row.examples,
    s.convention = 'Chỉ xét tứ giác đơn, lồi, không suy biến. Hình thang có ít nhất một cặp cạnh đối song song.',
    s.reviewStatus = 'PENDING',
    s.revision = coalesce(s.revision, 1)
WITH s, row
UNWIND row.formulas AS formula
MERGE (f:Formula {id: formula.id})
SET f.name = formula.name,
    f.expression = formula.expression,
    f.variables = formula.variables,
    f.conditions = formula.conditions,
    f.status = 'DRAFT'
MERGE (s)-[:HAS_FORMULA]->(f);
