using System;
using System.Web;

namespace GileadCommercialDS.Feature.Podcast.Models
{
    public class EpisodeCreateRequest
    {
        public string PodcastSlug { get; set; }

        public string EpisodeTitle { get; set; }
        public string EpisodeDescription { get; set; }
        public string EpisodeAuthor { get; set; }

        public string MediaType { get; set; }
        public string MediaFileId { get; set; }

        public int EpisodeNumber { get; set; }
        public int SeasonNumber { get; set; }

        public string EpisodeType { get; set; }
        public bool ExplicitContent { get; set; }

        public DateTime PublicationDate { get; set; }

        public string PublishStatus { get; set; }

        public HttpPostedFileBase MediaFile { get; set; }

        public HttpPostedFileBase EpisodeArtwork { get; set; }
    }
}