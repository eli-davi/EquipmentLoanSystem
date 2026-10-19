# בדיקה מול דרישות הפרויקט

✅ = מכוסה בקוד · ⚠️ = דורש פעולה שלכם לפני ההגשה

## הבעיות שנמצאו בגרסה הקודמת ותוקנו

| בעיה | תיקון |
|---|---|
| ההתחברות הייתה בדיקת אימייל/סיסמה קשיחה ב-Controller, ולא הייתה הרשמה | `AuthService` עם טבלת `Members`, hash של PBKDF2 והרשמה אמיתית |
| מפתח JWT ברירת מחדל כתוב בקוד (`DEFAULT_SECRET_KEY...`) | נמחק. המפתח מגיע מ-User Secrets, ואם לא הוגדר נוצר מפתח אקראי לזמן הריצה |
| connection string ב-`appsettings.json` שנכנס ל-repo | הועבר ל-`appsettings.Development.json` או ל-User Secrets |
| ה-Seed הכיל פריט אחד בלי עותקים, ולכן לא היה מה להשאיל | `SeedData`: 8 פריטים, 8 עותקים, 5 קטגוריות, 3 משתמשים והשאלה אחת. הכול בקוד |
| בסיס הנתונים דרש `database update` ידני | `MigrateAsync()` בעלייה |
| `MemberId` נשלח מהלקוח, כך שכל אחד יכול היה להשאיל בשם אחר | המזהה נלקח מה-JWT |
| `ItemsController` כלל רק GET ולא היו בו סינון, מיון או CRUD | נוספו GET by id, POST (201 + Location), PUT (204), DELETE (204) וחיפוש/סינון/מיון |
| `ItemDto` לא הכיל זמינות, ולכן הלקוח הציג "זמין" תמיד | נוספו `AvailableCopies` ו-`TotalCopies` |
| `LoansController` החזיר 200 במקום 201 | 201 + Location, ו-409 עם "זה כבר מושאל" |
| `CORS AllowAnyOrigin` תמיד | מקורות מוגדרים בקונפיגורציה |

## חלק ב – דרישות השרת

| # | דרישה | סטטוס | איפה |
|---|---|---|---|
| 1 | REST, שמות ברבים, HTTP methods, סטטוסים 200/201+Location/204/400/401/403/404/409, Swagger | ✅ | Controllers, Swagger עם כפתור Authorize |
| 2 | `[FromRoute]`/`[FromQuery]`/`[FromBody]`, Data Annotations, ModelState, ולידציה עסקית ב-Service | ✅ | DTOs, `LoanService` |
| 3 | 4 פרויקטים, כיוון תלויות נכון, Core בלי EF | ✅ | ראו את ה-csproj (Core ללא חבילות) |
| 4 | DI דרך constructor, interfaces ב-Core, DbContext scoped | ✅ | `Program.cs` |
| 5 | Code-First, DbSet לכל ישות, **2 migrations לפחות**, connection string מקונפיגורציה, Seed | ⚠️ | יש להריץ `dotnet ef migrations add` (ראו README) ולוודא שבהיסטוריה יש לפחות שתיים |
| 6 | CRUD, LINQ עם סינון ומיון, Skip/Take בשאילתה, AsNoTracking בקריאה | ✅ | `ItemRepository.GetPagedAsync` |
| 7 | one-to-many (Item→Copies), many-to-many (Item↔Category), Include/ThenInclude, Fluent API, בלי N+1 | ✅ | `AppDbContext`, `LoanRepository` |
| 8 | DTOs נפרדים ל-request/response, AutoMapper Profile | ✅ | `MappingProfile` |
| 9 | async מקצה לקצה + CancellationToken | ✅ | |
| 10 | ErrorHandling middleware (JSON אחיד), CorrelationId middleware, סדר pipeline | ✅ | `Middlewares/` |
| 11 | appsettings + Development, IOptions, User Secrets, אין סודות ב-repo | ✅ | `JwtSettings` + `IOptions` |
| 12 | הרשמה/התחברות, claims, 2 roles עם הרשאות שונות, `[Authorize]` | ✅ | Admin: ניהול פריטים וכל ההשאלות. User: השאלה והחזרה |
| 13 | xUnit + Moq על Service, כולל מקרה תקין ומקרה שנדחה על המשאב | ✅ | `LoanServiceTests` |

## חלק ג – תחרות על משאב
| דרישה | סטטוס |
|---|---|
| concurrency token על `ItemCopy` | ✅ `Version` + `IsConcurrencyToken` |
| `DbUpdateConcurrencyException` → 409 עם הודעה ברורה | ✅ |
| בדיקה עם שני DbContext | ✅ `ConcurrencyTests` |
| פסקה ב-README | ✅ |

## חלק ד – NLog
| דרישה | סטטוס |
|---|---|
| לוג לכל בקשה עם CorrelationId | ✅ `CorrelationIdMiddleware` |
| חריגה → Error | ✅ `ErrorHandlingMiddleware` |
| התנגשות → Warning | ✅ `LoanService` |
| בלי סיסמאות, tokens או תוכן הרשמה | ✅ רק מזהים, method ו-path |

## חלק ה – לקוח
| דרישה | סטטוס |
|---|---|
| התחברות + הרשמה, שמירת JWT ושליחה ב-header | ✅ |
| רשימה עם pagination מול השרת | ✅ |
| כפתור השאלה | ✅ "הפריט הושאל בהצלחה" / "זה כבר מושאל" |
| הצגת 409 | ✅ |
| מצב הדגמה בלי שרת (הציוד מוגדר בקובץ) | ✅ תוספת |

## חלק ו – הגשה
| דרישה | סטטוס |
|---|---|
| שני repositories נפרדים ב-GitHub | ⚠️ באחריותכם |
| `.gitignore` בלי bin/obj/node_modules/סודות | ✅ בשני הפרויקטים |
| README: תיאור, משאב, הרצה, משתמשי דמו | ✅ |
| `dotnet build` + `dotnet test` עוברים | ⚠️ לא ניתן היה לקמפל בסביבה שבה הקוד נכתב. יש להריץ אצלכם |
