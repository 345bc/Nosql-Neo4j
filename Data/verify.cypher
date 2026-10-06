// Chạy từng câu trong Neo4j Query, database chứa dữ liệu học tập.
// Bộ seed nền mong đợi: 6 Shape, 6 IS_A, 10 Formula.
MATCH (s:Shape)
RETURN count(s) AS shapeCount;

MATCH (:Shape)-[r:IS_A]->(:Shape)
RETURN count(r) AS classificationEdgeCount;

MATCH (f:Formula)
RETURN count(f) AS formulaCount;

MATCH (s:Shape)
OPTIONAL MATCH (s)-[:HAS_FORMULA]->(f:Formula)
RETURN s.id AS id, s.name AS name, s.status AS status,
       s.definition AS definition, count(f) AS formulas
ORDER BY id;

// Mong đợi 2 đường phân loại.
MATCH p = (:Shape {id: 'HINH_VUONG'})-[:IS_A*1..5]->(:Shape {id: 'TU_GIAC'})
RETURN [s IN nodes(p) | s.id] AS path;

// Mong đợi không có dòng: ID trùng.
MATCH (s:Shape)
WITH s.id AS id, count(*) AS copies
WHERE copies > 1
RETURN id, copies;

// Mong đợi không có dòng: chu trình, bao gồm tự nối.
MATCH p = (s:Shape)-[:IS_A*1..6]->(s)
RETURN p;

// Mong đợi không có dòng sau seed-knowledge: kiến thức thiếu.
MATCH (s:Shape)
WHERE trim(coalesce(s.definition, '')) = ''
   OR size(coalesce(s.properties, [])) = 0
   OR size(coalesce(s.recognitionSigns, [])) = 0
   OR size(coalesce(s.examples, [])) = 0
   OR NOT EXISTS { MATCH (s)-[:HAS_FORMULA]->(:Formula) }
RETURN s.id AS incompleteShape;

// Mong đợi có cả 6 hình khi chưa duyệt: không được tự công bố.
MATCH (s:Shape)
WHERE s.status = 'DRAFT'
RETURN s.id AS awaitingReview, s.reviewStatus AS reviewStatus;

SHOW CONSTRAINTS;
