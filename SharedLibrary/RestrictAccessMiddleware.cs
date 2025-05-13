using Microsoft.AspNetCore.Http;

namespace SharedLibrary;

public   class RestrictAccessMiddleware(RequestDelegate next) 
{
      
    
    public async Task InvokeAsync(HttpContext context)
    {
        var refferer = context.Request.Headers["Referer"].FirstOrDefault();
            if (string.IsNullOrEmpty(refferer))
            {
                    context.Response.StatusCode= StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Access Denied");
                    return;
            }
            else{
                
                await next(context);
            }   

    }
}
