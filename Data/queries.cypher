// Các truy vấn kiểm tra/tra cứu chạy riêng trong Neo4j Browser.
// Với Browser: chạy :params trước, không đặt chung với MATCH.
// :params {id:'HINH_VUONG',fromId:'HINH_VUONG',toId:'TU_GIAC'}
MATCH(s:Shape {status:'PUBLISHED'}) RETURN s.id,s.name,s.aliases ORDER BY s.name;
MATCH(s:Shape {id:$id,status:'PUBLISHED'})-[:HAS_KNOWLEDGE]->(k:KnowledgeItem {status:'PUBLISHED'}) RETURN k ORDER BY k.type,k.id;
MATCH p=(a:Shape {id:$fromId,status:'PUBLISHED'})-[:IS_A*0..10]->(b:Shape {id:$toId,status:'PUBLISHED'})
WHERE all(n IN nodes(p) WHERE n.status='PUBLISHED') AND all(r IN relationships(p) WHERE coalesce(r.active,true)) RETURN p;
MATCH(a:Shape {status:'PUBLISHED'})-[r:IS_A]->(b:Shape {status:'PUBLISHED'}) WHERE coalesce(r.active,true) RETURN a,r,b;
MATCH p=(s:Shape)-[:IS_A*1..100]->(s) RETURN p LIMIT 1;