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
WHERE trim(coalesce(v.sourceTitle, '')) = '' OR trim(coalesce(v.sourceLocator, '')) = ''
   OR trim(coalesce(v.reviewedBy, '')) = '' OR v.reviewedAt IS NULL
RETURN v.id AS missingPublicationReview;

MATCH (s:Shape {status: 'PUBLISHED'})
WHERE trim(coalesce(s.sourceTitle, '')) = '' OR trim(coalesce(s.sourceLocator, '')) = ''
   OR trim(coalesce(s.reviewedBy, '')) = '' OR s.reviewedAt IS NULL
RETURN s.id AS missingPublicationReview;
