MATCH (s:Shape {id: 'HINH_VUONG'})

MERGE (q:Question {id: 'Q_HV_001'})

MERGE (v:QuestionVersion {id: 'Q_HV_001_V1'})
ON CREATE SET
    v.version = 1,
    v.prompt = 'Hình vuông có cạnh 4 cm. Chu vi bằng bao nhiêu?',
    v.optionA = '8 cm',
    v.optionB = '12 cm',
    v.optionC = '16 cm',
    v.optionD = '20 cm',
    v.correctKey = 'C',
    v.explanation = 'Chu vi hình vuông bằng 4 lần cạnh: 4 × 4 = 16 cm.',
    v.status = 'DRAFT'

MERGE (q)-[:CURRENT]->(v)
MERGE (v)-[:ABOUT]->(s)

RETURN q.id, v.id, s.name;

//Kiểm tra câu vừa tạo
MATCH (q:Question)-[:CURRENT]->(v:QuestionVersion)
      -[:ABOUT]->(s:Shape)
RETURN
    q.id AS questionId,
    v.id AS versionId,
    v.prompt AS question,
    v.optionA AS A,
    v.optionB AS B,
    v.optionC AS C,
    v.optionD AS D,
    v.correctKey AS correctAnswer,
    v.status AS status,
    s.name AS topic;

MATCH (q:Question)
RETURN count(q) AS questionCount;
