using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Pipelines;

namespace GileadCommercialDS.Feature.Podcast.Pipelines
{
    public class RegisterPodcastRoute
    {
        public virtual void Process(PipelineArgs args)
        {
            string[] namespaces =
            {
                "GileadCommercialDS.Feature.Podcast.Controllers"
            };

            // RSS
            RouteTable.Routes.MapRoute(
                name: "PodcastRSS",
                url: "podcast/rss/{podcastSlug}",
                defaults: new
                {
                    controller = "Podcast",
                    action = "Index",
                    podcastSlug = UrlParameter.Optional
                },
                namespaces
            );

            // Create Podcast
            RouteTable.Routes.MapRoute(
                name: "PodcastCreate",
                url: "podcast/create",
                defaults: new
                {
                    controller = "Podcast",
                    action = "CreatePodcast"
                },
                namespaces
            );
            // Get Podcasts
            RouteTable.Routes.MapRoute(
                name: "PodcastGet",
                url: "podcast/getpodcasts",
                defaults: new
                {
                    controller = "Podcast",
                    action = "GetPodcasts"
                },
                namespaces
            );

            // Get Episodes
            RouteTable.Routes.MapRoute(
                name: "EpisodeGet",
                url: "podcast/episode/get",
                defaults: new
                {
                    controller = "Podcast",
                    action = "GetEpisodes"
                },
                namespaces
            );
            // Get Episode Details
            RouteTable.Routes.MapRoute(
                name: "EpisodeGetDetails",
                url: "podcast/episode/getdetails",
                defaults: new
                {
                    controller = "Podcast",
                    action = "GetEpisodeDetails"
                },
                namespaces
            );

            RouteTable.Routes.MapRoute(
                name: "EpisodeCreate",
                url: "podcast/episode/create",
                defaults: new
                {
                controller = "Podcast",
                action = "CreateEpisode"
                },
                namespaces
            );

            RouteTable.Routes.MapRoute(
                name: "EpisodeUpdate",
                url: "podcast/episode/update",
                defaults: new
                {
                    controller = "Podcast",
                    action = "UpdateEpisode"
                },
                namespaces
            );
        }
    }
}