using System;
using System.Text.Json.Serialization;

namespace SaaS.DTOs
{
    public enum ResultStatus
    {
        Ok = 0,
        Invalid,          // 400 - doğrulama / iş kuralı
        Unauthorized,     // 401 - kimlik doğrulanamadı
        Forbidden,        // 403 - yetki yok
        NotFound,         // 404
        Conflict,         // 409 - çakışma (e-posta kayıtlı, cooldown vb.)
        Error,            // 500 - beklenmeyen
        NotModified,      // 304 - Güncellenmedi
    }

    public interface IResult
    {
        bool IsSuccess { get; }
        ResultStatus Status { get; }
        string Message { get; }
    }

    public sealed class Result : IResult
    {
        public bool IsSuccess { get; }

        [JsonIgnore]
        public ResultStatus Status { get; }

        public string Message { get; }

        private Result(bool isSuccess, ResultStatus status, string message)
        {
            IsSuccess = isSuccess;
            Status = status;
            Message = message;
        }

        public static Result Success(string message = "İşlem başarılı")
        {
            return new Result(true, ResultStatus.Ok, message);
        }

        public static Result NotModified(string message = "İşlem başarılı")
        {
            return new Result(true, ResultStatus.NotModified, message);
        }

        public static Result Fail(string message, ResultStatus status = ResultStatus.Invalid)
        {
            return new Result(false, status, message);
        }

        public static Result NotFound(string message)
        {
            return Fail(message, ResultStatus.NotFound);
        }

        public static Result Conflict(string message)
        {
            return Fail(message, ResultStatus.Conflict);
        }

        public static Result Unauthorized(string message)
        {
            return Fail(message, ResultStatus.Unauthorized);
        }

        public static Result Forbidden(string message)
        {
            return Fail(message, ResultStatus.Forbidden);
        }
    }

    public sealed class Result<T> : IResult
    {
        public bool IsSuccess { get; }

        [JsonIgnore]
        public ResultStatus Status { get; }

        public string Message { get; }

        public T? Data { get; }

        private Result(bool isSuccess, ResultStatus status, string message, T? data)
        {
            IsSuccess = isSuccess;
            Status = status;
            Message = message;
            Data = data;
        }

        public static Result<T> Success(T data, string message = "İşlem başarılı")
        {
            return new Result<T>(true, ResultStatus.Ok, message, data);
        }

        public static Result<T> NotModified(T data, string message = "İşlem başarılı")
        {
            return new Result<T>(true, ResultStatus.NotModified, message, data);
        }

        public static Result<T> Fail(string message, ResultStatus status = ResultStatus.Invalid)
        {
            return new Result<T>(false, status, message, default);
        }
        
        public static Result<T> Invalid(T data, string message = "Girdiğiniz bilgileri kontrol edin")
        {
            return new Result<T>(false, ResultStatus.Invalid, message, data);
        }

        public static Result<T> NotFound(string message)
        {
            return Fail(message, ResultStatus.NotFound);
        }

        public static Result<T> Conflict(string message)
        {
            return Fail(message, ResultStatus.Conflict);
        }

        public static Result<T> Unauthorized(string message)
        {
            return Fail(message, ResultStatus.Unauthorized);
        }

        public static Result<T> Forbidden(string message)
        {
            return Fail(message, ResultStatus.Forbidden);
        }
    }
}