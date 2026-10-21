# Equipment Loan System – Web API

מערכת להשאלת ציוד: משתמשים מתחברים, צופים ברשימת הציוד (עם חיפוש, מיון ו-pagination מול בסיס הנתונים) ומשאילים או מחזירים פריטים. מנהל (Admin) מוסיף, מעדכן ומוחק פריטים ורואה את כל ההשאלות. השרת בנוי ב-ASP.NET Core 8 בארבע שכבות (Core / Data / Service / API) עם EF Core ו-PostgreSQL, JWT, AutoMapper, NLog ובדיקות xUnit + Moq.

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

**דרישות:** .NET 8 SDK ו-PostgreSQL (מקומי, או בסיס נתונים חינמי בענן כמו Neon / Supabase).

```bash
# מחרוזת החיבור נשמרת ב-User Secrets ולא ב-repo:
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=EquipmentLoanDb;Username=postgres;Password=<הסיסמה>" --project EquipmentLoanSystem.API
dotnet user-secrets set "JwtSettings:Secret" "<מחרוזת אקראית של 32 תווים לפחות>" --project EquipmentLoanSystem.API

dotnet run --project EquipmentLoanSystem.API --launch-profile http
```

- Swagger: http://localhost:5000/swagger
- **אין צורך להריץ `database update`.** בעלייה, השרת מריץ `Database.MigrateAsync()`, יוצר את הטבלאות וטוען את כל הציוד, הקטגוריות ומשתמשי הדמו מתוך הקוד (`SeedData` ב-`AppDbContext.cs`).
- concurrency token: עמודת `Version` (Guid) שמנוהלת בקוד, ולכן מתאימה ל-PostgreSQL (שבו אין `rowversion`).

### הרצה בענן
משתני סביבה שצריך להגדיר בשרת (בפלטפורמת האירוח, לא בקוד):

| משתנה | ערך |
|---|---|
| `ConnectionStrings__Default` | מחרוזת החיבור של PostgreSQL בענן (כולל `SSL Mode=Require`) |
| `JwtSettings__Secret` | מחרוזת אקראית של 32 תווים לפחות |
| `Cors__AllowedOrigins__0` | כתובת הלקוח, למשל `https://my-client.netlify.app` |

בשורש ה-solution יש `Dockerfile` לפלטפורמות שמריצות .NET דרך Docker (למשל Render).

### בדיקות
```bash
dotnet test
```
בדיקות הקונקרנטיות רצות מול SQLite in-memory (בסיס נתונים רלציוני אמיתי) ולא דורשות PostgreSQL.

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
