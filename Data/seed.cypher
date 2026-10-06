UNWIND [
  {id: 'TU_GIAC', name: 'Tứ giác'},
  {id: 'HINH_THANG', name: 'Hình thang'},
  {id: 'HINH_BINH_HANH', name: 'Hình bình hành'},
  {id: 'HINH_CHU_NHAT', name: 'Hình chữ nhật'},
  {id: 'HINH_THOI', name: 'Hình thoi'},
  {id: 'HINH_VUONG', name: 'Hình vuông'}
] AS row
MERGE (s:Shape {id: row.id})
ON CREATE SET s.aliases = [], s.status = 'DRAFT'
SET s.name = row.name;


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
