# Migrations

התיקייה הזו נוצרת ע"י `dotnet ef`. אין לכתוב בה קבצים ידנית.

משורש ה-solution:

```bash
dotnet ef migrations add <Name> -p EquipmentLoanSystem.Data -s EquipmentLoanSystem.API
```

אין צורך להריץ `database update` ידנית - ה-API מריץ `Database.MigrateAsync()` בעלייה,
כך שבסיס הנתונים נוצר, מתעדכן ומקבל את נתוני ה-Seed אוטומטית.
