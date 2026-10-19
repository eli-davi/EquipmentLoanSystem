namespace EquipmentLoanSystem.Core.DTOs
{
    /// <summary>
    /// תוצאה של פעולה עסקית. שכבת ה-Service מחזירה Result במקום לזרוק חריגות על מקרים צפויים,
    /// וה-Controller מתרגם את ErrorCode לסטטוס HTTP.
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? ErrorMessage { get; }
        public int ErrorCode { get; }

        private Result(bool isSuccess, T? value, string? errorMessage, int errorCode)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
        }

        public static Result<T> Success(T value) => new(true, value, null, 200);
        public static Result<T> Fail(string errorMessage, int errorCode = 400) => new(false, default, errorMessage, errorCode);
        public static Result<T> Unauthorized(string errorMessage) => new(false, default, errorMessage, 401);
        public static Result<T> Forbidden(string errorMessage) => new(false, default, errorMessage, 403);
        public static Result<T> NotFound(string errorMessage) => new(false, default, errorMessage, 404);
        public static Result<T> Conflict(string errorMessage) => new(false, default, errorMessage, 409);
    }
}
