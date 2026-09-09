using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SaaS.DTOs;

namespace SaaS.Extensions
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult(this DTOs.IResult result)
        {
            return new ObjectResult(result)
            {
                StatusCode = ToStatusCode(result.Status)
            };
        }
        private static int ToStatusCode(ResultStatus status)
        {
            switch (status)
            {
                case ResultStatus.Ok:
                    return StatusCodes.Status200OK;
                case ResultStatus.Invalid:
                    return StatusCodes.Status400BadRequest;
                case ResultStatus.Unauthorized:
                    return StatusCodes.Status401Unauthorized;
                case ResultStatus.NotFound:
                    return StatusCodes.Status404NotFound;
                case ResultStatus.Forbidden:
                    return StatusCodes.Status403Forbidden;
                case ResultStatus.Conflict:
                    return StatusCodes.Status409Conflict;
                case ResultStatus.Error:
                    return StatusCodes.Status500InternalServerError;
                case ResultStatus.NotModified:
                    return StatusCodes.Status304NotModified;
                default:
                    return StatusCodes.Status500InternalServerError;
            }
        }
    }
}