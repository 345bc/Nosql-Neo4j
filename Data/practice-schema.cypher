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
