using System.Web;

namespace GileadCommercialDS.Feature.Podcast.Models
{
    public class PodcastCreateRequest
    {
        public string PodcastTitle { get; set; }
        public string PodcastDescription { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public string Language { get; set; }
        public bool ExplicitContent { get; set; }
        public string PodcastType { get; set; }
        public string WebsiteUrl { get; set; }
        public string Copyright { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }

        public HttpPostedFileBase PodcastArtwork { get; set; }
    }
}