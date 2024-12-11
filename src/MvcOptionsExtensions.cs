using Gainsway.ExceptionsExtensions.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Gainsway.ExceptionsExtensions;

public static class MvcOptionsExtensions
{
    public static void UseGainswayExceptionMiddleware(this MvcOptions options)
    {
        options.Filters.Add<HttpResponseExceptionFilter>();
    }
}
