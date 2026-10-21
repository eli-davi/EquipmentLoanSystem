using Npgsql;

namespace EquipmentLoanSystem.API.Extensions
{
    /// <summary>
    /// Render (וספקים אחרים) נותנים את מחרוזת החיבור כ-URL:
    ///   postgresql://user:password@host:5432/dbname
    /// Npgsql צריך פורמט Key=Value, ולכן ממירים כאן. מחרוזת שכבר בפורמט Key=Value מוחזרת כמו שהיא.
    /// </summary>
    public static class ConnectionStringHelper
    {
        public static string ToNpgsql(string value)
        {
            if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            var uri = new Uri(value);
            var userInfo = uri.UserInfo.Split(':', 2);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5432,
                Database = uri.AbsolutePath.TrimStart('/'),
                Username = Uri.UnescapeDataString(userInfo[0]),
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
                // Internal URL של Render עובד בלי SSL, External URL דורש SSL - Prefer מתאים לשניהם
                SslMode = string.Equals(query["sslmode"], "require", StringComparison.OrdinalIgnoreCase)
                    ? SslMode.Require
                    : SslMode.Prefer
            };

            return builder.ConnectionString;
        }
    }
}
