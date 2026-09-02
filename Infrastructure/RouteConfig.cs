using System.Web.Mvc;
using System.Web.Routing;

namespace GileadCommercialDS.Feature.Podcast.Infrastructure
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute(
                name: "PodcastRss",
                url: "podcast/rss",
                defaults: new
                {
                    controller = "Podcast",
                    action = "Index"
                });
        }
    }
}