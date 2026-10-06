// Bộ fixture hình vuông, chưa được duyệt; không thay đổi phiên bản đã tồn tại.
// Chạy practice-schema.cypher trước. Q_HV_001_V1 có sẵn được giữ nguyên.
UNWIND [
  {id: 'Q_HV_001', versionId: 'Q_HV_001_V1', prompt: 'Hình vuông có cạnh 4 cm. Chu vi bằng bao nhiêu?', a: '8 cm', b: '12 cm', c: '16 cm', d: '20 cm', explanation: 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 4 = 16 cm.'},
  {id: 'Q_HV_002', versionId: 'Q_HV_002_V1', prompt: 'Hình vuông có cạnh 5 cm. Chu vi bằng bao nhiêu?', a: '16 cm', b: '18 cm', c: '20 cm', d: '24 cm', explanation: 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 5 = 20 cm.'},
  {id: 'Q_HV_003', versionId: 'Q_HV_003_V1', prompt: 'Hình vuông có cạnh 6 cm. Chu vi bằng bao nhiêu?', a: '20 cm', b: '22 cm', c: '24 cm', d: '28 cm', explanation: 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 6 = 24 cm.'},
  {id: 'Q_HV_004', versionId: 'Q_HV_004_V1', prompt: 'Hình vuông có cạnh 7 cm. Chu vi bằng bao nhiêu?', a: '24 cm', b: '26 cm', c: '28 cm', d: '32 cm', explanation: 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 7 = 28 cm.'},
  {id: 'Q_HV_005', versionId: 'Q_HV_005_V1', prompt: 'Hình vuông có cạnh 8 cm. Chu vi bằng bao nhiêu?', a: '28 cm', b: '30 cm', c: '32 cm', d: '36 cm', explanation: 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 8 = 32 cm.'},
  {id: 'Q_HV_006', versionId: 'Q_HV_006_V1', prompt: 'Hình vuông có cạnh 9 cm. Chu vi bằng bao nhiêu?', a: '32 cm', b: '34 cm', c: '36 cm', d: '40 cm', explanation: 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 9 = 36 cm.'},
  {id: 'Q_HV_007', versionId: 'Q_HV_007_V1', prompt: 'Hình vuông có cạnh 10 cm. Chu vi bằng bao nhiêu?', a: '36 cm', b: '38 cm', c: '40 cm', d: '44 cm', explanation: 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 10 = 40 cm.'},
  {id: 'Q_HV_008', versionId: 'Q_HV_008_V1', prompt: 'Hình vuông có cạnh 11 cm. Chu vi bằng bao nhiêu?', a: '40 cm', b: '42 cm', c: '44 cm', d: '48 cm', explanation: 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 11 = 44 cm.'},
  {id: 'Q_HV_009', versionId: 'Q_HV_009_V1', prompt: 'Hình vuông có cạnh 12 cm. Chu vi bằng bao nhiêu?', a: '44 cm', b: '46 cm', c: '48 cm', d: '52 cm', explanation: 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 12 = 48 cm.'},
  {id: 'Q_HV_010', versionId: 'Q_HV_010_V1', prompt: 'Hình vuông có cạnh 13 cm. Chu vi bằng bao nhiêu?', a: '48 cm', b: '50 cm', c: '52 cm', d: '56 cm', explanation: 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 13 = 52 cm.'}
] AS row
MATCH (s:Shape {id: 'HINH_VUONG'})
MERGE (q:Question {id: row.id})
MERGE (v:QuestionVersion {id: row.versionId})
ON CREATE SET v.version = 1, v.prompt = row.prompt,
    v.optionA = row.a, v.optionB = row.b, v.optionC = row.c, v.optionD = row.d,
    v.correctKey = 'C', v.explanation = row.explanation, v.status = 'DRAFT'
MERGE (v)-[:ABOUT]->(s)
WITH q, v
WHERE NOT EXISTS { MATCH (q)-[:CURRENT]->() }
MERGE (q)-[:CURRENT]->(v);
