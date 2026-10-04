-- Active students SQL
SELECT "s"."Id", "s"."IsActive", "s"."Name"
FROM "Students" AS "s"
WHERE "s"."IsActive"
ORDER BY "s"."Name"

-- Available equipment SQL
SELECT "e"."Id", "e"."IsAvailable", "e"."Name"
FROM "Equipment" AS "e"
WHERE "e"."IsAvailable"
ORDER BY "e"."Name"

-- Active borrowings join SQL
SELECT "s"."Name" AS "Student", "e"."Name" AS "Equipment", "b"."BorrowDate", "b"."ExpectedReturnDate"
FROM "Borrowings" AS "b"
INNER JOIN "Students" AS "s" ON "b"."StudentId" = "s"."Id"
INNER JOIN "Equipment" AS "e" ON "b"."EquipmentId" = "e"."Id"
WHERE "b"."Status" = 0

