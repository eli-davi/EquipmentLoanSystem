# Equipment Loan System – Web API

מערכת להשאלת ציוד: משתמשים מתחברים, צופים ברשימת הציוד (עם חיפוש, מיון ו-pagination מול בסיס הנתונים) ומשאילים או מחזירים פריטים. מנהל (Admin) מוסיף, מעדכן ומוחק פריטים ורואה את כל ההשאלות. השרת בנוי ב-ASP.NET Core 8 בארבע שכבות (Core / Data / Service / API) עם EF Core ו-SQL Server, JWT, AutoMapper, NLog ובדיקות xUnit + Moq.

## המשאב המוגבל והטיפול בתחרות

**המשאב:** העותק הפיזי (`ItemCopy`). לכל פריט יש עותקים, ולכל עותק יכולה להיות רק השאלה פעילה אחת.

**איפה נמצאת התחרות:** כששני משתמשים לוחצים "השאל" על אותו פריט כמעט באותו רגע, שתי הבקשות קוראות את אותו עותק כשהוא עדיין `Available`. "שני משתמשים פעלו בו-זמנית, שניהם קיבלו אישור, ואסור היה ששניהם יקבלו."

**איך המערכת מגיבה (Optimistic Concurrency):**
1. ל-`ItemCopy` יש שדה `Version` (Guid) שמוגדר כ-concurrency token ב-Fluent API (`IsConcurrencyToken`). הדריסה של `SaveChangesAsync` ב-`AppDbContext` מחליפה אותו בכל עדכון.
2. `LoanService.CreateLoanAsync` קורא את העותק עם מעקב, בודק שהוא פנוי, מסמן אותו `Loaned` ומוסיף `Loan`. שמירה אחת (`SaveChangesAsync`) מבצעת את ה-UPDATE ואת ה-INSERT בטרנזקציה אחת, וה-UPDATE כולל `WHERE Version = @original`.
3. אם בקשה אחרת הספיקה לשנות את העותק, אף שורה לא מתעדכנת ו-EF זורק `DbUpdateConcurrencyException`. ה-`UnitOfWork` (שכבת Data) תופס **רק** את החריגה הזו ועוטף אותה ב-`ConcurrencyConflictException` של Core, כדי ששכבת ה-Service לא תהיה תלויה ב-EF Core.
4. ה-Service תופס את `ConcurrencyConflictException`, כותב לוג ברמת **Warning** ומחזיר **409 Conflict**. גם כשאין עותק פנוי בכלל מוחזר 409.
5. הלקוח מציג **"הפריט הושאל בהצלחה"** כשהשרת מחזיר 201, ו-**"זה כבר מושאל"** כשהשרת מחזיר 409.

הבדיקה `ConcurrencyTests.TwoContexts_UpdateSameCopy_FirstSaves_SecondThrowsDbUpdateConcurrencyException` מוכיחה את המנגנון: שני `DbContext` נפרדים קוראים את אותו עותק, הראשון שומר בהצלחה והשני מקבל חריגה.

## הרצה מקומית

**דרישות:** .NET 8 SDK ו-SQL Server LocalDB (מותקן עם Visual Studio). אפשר להשתמש גם ב-SQL Server Express (ראו בהמשך).

```bash

# הרצה
dotnet run --project EquipmentLoanSystem.API --launch-profile http
```

- Swagger: http://localhost:5000/swagger
- **אין צורך להריץ `database update`.** בעלייה, השרת מריץ `Database.MigrateAsync()`, יוצר את בסיס הנתונים וטוען את כל הציוד, הקטגוריות ומשתמשי הדמו מתוך הקוד (`SeedData` ב-`AppDbContext.cs`).
- הלוגים נכתבים ל-`EquipmentLoanSystem.API/bin/.../logs/`: הקובץ `app-*.log` מכיל הכול, והקובץ `problems-*.log` מכיל רק Warning ו-Error.

### סודות (User Secrets)
אין סוד ב-repo. בפיתוח, אם לא מוגדר מפתח JWT, השרת יוצר מפתח אקראי לזמן הריצה, כך שאפשר להריץ מיד אחרי clone. כדי להגדיר מפתח קבוע:

```bash
dotnet user-secrets set "JwtSettings:Secret" "<מחרוזת אקראית של 32 תווים לפחות>" --project EquipmentLoanSystem.API
# שימוש ב-SQL Server Express במקום LocalDB:
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost\SQLEXPRESS;Database=EquipmentLoanDb;Trusted_Connection=True;TrustServerCertificate=True" --project EquipmentLoanSystem.API
```

### בדיקות
```bash
dotnet test
```
בדיקות הקונקרנטיות רצות מול SQLite in-memory (בסיס נתונים רלציוני אמיתי) ולא דורשות SQL Server.

## משתמשי דמו

| Role  | Email               | Password |
|-------|---------------------|----------|
| Admin | admin@example.com   | 123456   |
| User  | user@example.com    | 123456   |
| User  | user2@example.com   | 123456   |

ב-Seed, המקרן (פריט 2) כבר מושאל על ידי `user2`. כך אפשר לראות את "זה כבר מושאל" מיד.

## Endpoints

| Method | Route | הרשאה | תשובות |
|---|---|---|---|
| POST | /api/auth/register | פתוח | 201, 400, 409 |
| POST | /api/auth/login | פתוח | 200, 400, 401 |
| GET | /api/items?page=&pageSize=&search=&categoryId=&sortBy=&descending=&onlyAvailable= | פתוח | 200, 400 |
| GET | /api/items/{id} | פתוח | 200, 404 |
| GET | /api/categories | פתוח | 200 |
| POST | /api/items | Admin | 201 + Location, 400, 401, 403 |
| PUT | /api/items/{id} | Admin | 204, 400, 403, 404 |
| DELETE | /api/items/{id} | Admin | 204, 403, 404, 409 |
| POST | /api/loans | מחובר | 201 + Location, 400, 401, 404, **409** |
| GET | /api/loans/{id} | בעלים / Admin | 200, 403, 404 |
| GET | /api/loans/mine | מחובר | 200 |
| GET | /api/loans | Admin | 200, 403 |
| PATCH | /api/loans/{id} `{"status":"Returned"}` | בעלים / Admin | 204, 400, 403, 404, 409 |

כל שגיאה מוחזרת כ-JSON אחיד: `{ "status", "message", "correlationId" }`.
