using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaaS.DTOs
{
    public class Result<T>
    {
        public bool IsSuccess {get; set;}
        public T Data {get; set;} = default!;
        public string Message {get; set;} = string.Empty!;

        public static Result<T> Fail(string message, T data)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Data = data,
                Message = message  
            };
        }

        public static Result<T> Fail(T data)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Data = data,
                Message = "İşlem başarısız oldu"  
            };
        }

        public static Result<T> Fail(string message)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Data = default!,
                Message = message
            };
        }
        
        public static Result<T> Fail()
        {
            return new Result<T>
            {
                IsSuccess = false,
                Data = default!,
                Message = "İşlem başarısız oldu"  
            };
        }
        
        public static Result<T> Success(T data, string message)
        {
            return new Result<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message
            };
        }

        public static Result<T> Success(string message)
        {
            return new Result<T>
            {
                IsSuccess = true,
                Data = default!,
                Message = message
            };
        }

        public static Result<T> Success()
        {
            return new Result<T>
            {
                IsSuccess = true,
                Data = default!,
                Message = "İşlem başarılı"
            };
        }
    }
}