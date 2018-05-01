using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace InMemory
{
    /// <summary>
    /// This is a sample of using RateLimiter.
    /// Put [RequestLimiterByIpFilter] before your MVC controllers which you want to limit entry limit based on client IP.
    /// </summary>
    public class RequestLimiterByIpFilterAttribute : ActionFilterAttribute
    {
        private static readonly RateLimiter DefaultIpBasedRateLimiter = new RateLimiter(2000, 3600, nameof(RequestLimiterByIpFilterAttribute));
        private RateLimiter IpBasedRateLimiter { get; }


        public RequestLimiterByIpFilterAttribute()
        { }

        public RequestLimiterByIpFilterAttribute(int maxTries, int inPeriod, string filterName)
        {
            IpBasedRateLimiter = new RateLimiter(maxTries, inPeriod, filterName);
        }


        public override void OnActionExecuting(HttpActionContext filterContext)
        {
            //var methodName = $"{filterContext.ActionDescriptor.ControllerDescriptor.ControllerName}.{filterContext.ActionDescriptor.ActionName}";
            var ip = HttpContext.Current.ClientIpAddress() ?? "";
            if (!IpBasedRateLimiter?.CanProceed(ip) ?? !DefaultIpBasedRateLimiter.CanProceed(ip))
            {
                filterContext.Response = new HttpResponseMessage(HttpStatusCode.Forbidden); // new HttpStatusCodeResult(429);
                //Logger.Fatal($"{ip} could not enter {methodName} because of many tries");
            }
            base.OnActionExecuting(filterContext);
        }
    }
}