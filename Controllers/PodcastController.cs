using GileadCommercialDS.Feature.Podcast.Models;
using GileadCommercialDS.Feature.Podcast.Services;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.Resources.Media;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml.Linq;

namespace GileadCommercialDS.Feature.Podcast.Controllers
{
    public class PodcastController : Controller
    {
        private const string PodcastRootPath =
            "/sitecore/content/EDS/MedicalAffairs/MedicalAffairsMaster/Home/Podcasts";

        public ActionResult Index(string podcastSlug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(podcastSlug))
                {
                    return new HttpStatusCodeResult(
                        400,
                        "Podcast slug is required.");
                }

                // ---------------------------------------------------------
                // Get Sitecore database
                // ---------------------------------------------------------
                Item podcastsRoot =
                    Sitecore.Context.Database.GetItem(PodcastRootPath);

                if (podcastsRoot == null)
                {
                    return new HttpStatusCodeResult(
                        500,
                        "Podcast root item was not found.");
                }

                // ---------------------------------------------------------
                // Find PodcastMain using Podcast Slug
                // ---------------------------------------------------------
                Item podcast = podcastsRoot
                    .Children
                    .FirstOrDefault(x =>
                        string.Equals(
                            x["Podcast Slug"],
                            podcastSlug,
                            StringComparison.OrdinalIgnoreCase));

                if (podcast == null)
                {
                    return HttpNotFound(
                        "Podcast not found: " + podcastSlug);
                }

                // ---------------------------------------------------------
                // Podcast fields
                // ---------------------------------------------------------
                string podcastTitle =
                    podcast["Podcast Title"];

                string podcastDescription =
                    podcast["Podcast Description"];

                string podcastAuthor =
                    podcast["Author"];

                string podcastCategory =
                    podcast["Category"];

                string podcastLanguage =
                    podcast["Language"];

                string websiteUrl =
                    podcast["Website URL"];

                string copyright =
                    podcast["Copyright"];

                string podcastType =
                    podcast["Podcast Type"];

                string ownerName =
                    podcast["Owner Name"];

                string ownerEmail =
                    podcast["Owner Email"];

                bool podcastExplicit =
                    podcast["Explicit Content"] == "1";

                // ---------------------------------------------------------
                // Public base URL
                // ---------------------------------------------------------
                string publicBaseUrl =
                    Request.Url.GetLeftPart(UriPartial.Authority);

                string feedUrl =
                    publicBaseUrl +
                    "/podcast/rss/" +
                    podcastSlug;

                // ---------------------------------------------------------
                // Podcast artwork
                // ---------------------------------------------------------
                string artworkUrl =
                    GetImageUrl(
                        podcast,
                        "Podcast Artwork");

                // ---------------------------------------------------------
                // RSS namespaces
                // ---------------------------------------------------------
                XNamespace itunes =
                    "http://www.itunes.com/dtds/podcast-1.0.dtd";

                XNamespace content =
                    "http://purl.org/rss/1.0/modules/content/";

                XNamespace atom =
                    "http://www.w3.org/2005/Atom";

                // ---------------------------------------------------------
                // Build channel
                // ---------------------------------------------------------
                XElement channel =
                    new XElement(
                        "channel",

                        new XElement(
                            "title",
                            podcastTitle),

                        new XElement(
                            "link",
                            websiteUrl),

                        new XElement(
                            "description",
                            podcastDescription),

                        new XElement(
                            "language",
                            podcastLanguage),

                        new XElement(
                            "copyright",
                            copyright),

                        new XElement(
                            "lastBuildDate",
                            DateTimeOffset.UtcNow.ToString(
                                "ddd, dd MMM yyyy HH:mm:ss +0000",
                                CultureInfo.InvariantCulture)),

                        new XElement(
                            atom + "link",

                            new XAttribute(
                                "href",
                                feedUrl),

                            new XAttribute(
                                "rel",
                                "self"),

                            new XAttribute(
                                "type",
                                "application/rss+xml")),

                        new XElement(
                            itunes + "author",
                            podcastAuthor),

                        new XElement(
                            itunes + "summary",
                            podcastDescription),

                        new XElement(
                            itunes + "explicit",
                            podcastExplicit ? "true" : "false"),

                        new XElement(
                            itunes + "type",
                            podcastType),

                        BuildArtworkElement(
                            itunes,
                            artworkUrl),

                        new XElement(
                            itunes + "category",

                            new XAttribute(
                                "text",
                                podcastCategory)),

                        new XElement(
                            itunes + "owner",

                            new XElement(
                                itunes + "name",
                                ownerName),

                            new XElement(
                                itunes + "email",
                                ownerEmail))
                    );

                // ---------------------------------------------------------
                // Get Episodes
                // ---------------------------------------------------------
                foreach (Item episode in podcast.Children)
                {
                    if (!IsEpisode(episode))
                    {
                        continue;
                    }

                    // Only published episodes belong in RSS
                    string publishStatus =
                        episode["Publish Status"];

                    if (!string.Equals(
                            publishStatus,
                            "Published",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    XElement episodeElement =
                        BuildEpisodeElement(
                            episode,
                            itunes,
                            content);

                    channel.Add(episodeElement);
                }

                // ---------------------------------------------------------
                // Build RSS document
                // ---------------------------------------------------------
                XDocument rss =
                    new XDocument(
                        new XDeclaration(
                            "1.0",
                            "UTF-8",
                            null),

                        new XElement(
                            "rss",

                            new XAttribute(
                                "version",
                                "2.0"),

                            new XAttribute(
                                XNamespace.Xmlns + "itunes",
                                itunes),

                            new XAttribute(
                                XNamespace.Xmlns + "content",
                                content),

                            new XAttribute(
                                XNamespace.Xmlns + "atom",
                                atom),

                            channel));

                Response.ContentType =
                    "application/rss+xml";

                Response.ContentEncoding =
                    Encoding.UTF8;

                return Content(
                    rss.ToString(),
                    "application/rss+xml",
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Sitecore.Diagnostics.Log.Error(
                    "Podcast RSS generation failed.",
                    ex,
                    this);

                return new HttpStatusCodeResult(
                    500,
                    "Podcast RSS generation failed.");
            }
        }

        // =============================================================
        // Build Episode
        // =============================================================
        [HttpPost]
        public ActionResult CreatePodcast(
    PodcastCreateRequest request)
        {
            try
            {
                PodcastContentService service =
                    new PodcastContentService();

                Sitecore.Data.Items.Item podcastItem =
                    service.CreatePodcast(request);

                return Json(new
                {
                    success = true,
                    itemId = podcastItem.ID.ToString(),
                    itemPath = podcastItem.Paths.FullPath,
                    podcastSlug = podcastItem["Podcast Slug"]
                });
            }
            catch (System.Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpGet]
        public ActionResult GetPodcasts()
        {
            try
            {
                Item podcastsRoot =
                    Sitecore.Context.Database.GetItem(PodcastRootPath);

                if (podcastsRoot == null)
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "Podcast root item was not found."
                        },
                        JsonRequestBehavior.AllowGet);
                }

                var podcasts =
                    podcastsRoot.Children
                        .Where(x => !string.IsNullOrWhiteSpace(x["Podcast Slug"]))
                        .Select(x => new
                        {
                            title = x["Podcast Title"],
                            slug = x["Podcast Slug"]
                        })
                        .ToList();

                return Json(
                    new
                    {
                        success = true,
                        podcasts = podcasts
                    },
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message
                    },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetEpisodes(string podcastSlug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(podcastSlug))
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "Podcast slug is required."
                        },
                        JsonRequestBehavior.AllowGet);
                }

                Item podcastsRoot =
                    Sitecore.Context.Database.GetItem(PodcastRootPath);

                if (podcastsRoot == null)
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "Podcast root item was not found."
                        },
                        JsonRequestBehavior.AllowGet);
                }

                Item podcastItem =
                    podcastsRoot.Children.FirstOrDefault(
                        x => string.Equals(
                            x["Podcast Slug"],
                            podcastSlug,
                            StringComparison.OrdinalIgnoreCase));

                if (podcastItem == null)
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "Podcast was not found: " + podcastSlug
                        },
                        JsonRequestBehavior.AllowGet);
                }

                var episodes =
                    podcastItem.Children
                        .Where(IsEpisode)
                        .Select(x => new
                        {
                            id = x.ID.ToString(),
                            title = x["Episode Title"],
                            episodeNumber = x["Episode Number"],
                            seasonNumber = x["Season Number"]
                        })
                        .ToList();

                return Json(
                    new
                    {
                        success = true,
                        episodes = episodes
                    },
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message
                    },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetEpisodeDetails(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "Episode ID is required."
                        },
                        JsonRequestBehavior.AllowGet);
                }

                ID episodeId;

                if (!ID.TryParse(id, out episodeId))
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "Invalid Episode ID: " + id
                        },
                        JsonRequestBehavior.AllowGet);
                }

                Item episodeItem =
                    Sitecore.Context.Database.GetItem(episodeId);

                if (episodeItem == null)
                {
                    return Json(
                        new
                        {
                            success = false,
                            message = "Episode was not found: " + id
                        },
                        JsonRequestBehavior.AllowGet);
                }

                string artworkUrl =
                    GetImageUrl(
                        episodeItem,
                        "Episode Artwork");

                string mediaUrl =
                    GetFileUrl(
                        episodeItem,
                        "Media File");

                return Json(
                    new
                    {
                        success = true,
                        episode = new
                        {
                            id = episodeItem.ID.ToString(),
                            title = episodeItem["Episode Title"],
                            description = episodeItem["Episode Description"],
                            author = episodeItem["Episode Author"],
                            mediaType = episodeItem["Media Type"],
                            episodeNumber = episodeItem["Episode Number"],
                            seasonNumber = episodeItem["Season Number"],
                            episodeType = episodeItem["Episode Type"],
                            explicitContent =
                                episodeItem["Explicit Content"] == "1",
                            publicationDate =
                                episodeItem["Publication Date"],
                            publishStatus =
                                episodeItem["Publish Status"],
                            guid = episodeItem["GUID"],
                            mediaUrl = mediaUrl,
                            artworkUrl = artworkUrl
                        }
                    },
                    JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(
                    new
                    {
                        success = false,
                        message = ex.Message
                    },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult CreateEpisode(
    EpisodeCreateRequest request)
        {
            try
            {
                PodcastContentService service =
                    new PodcastContentService();

                Sitecore.Data.Items.Item episodeItem =
                    service.CreateEpisode(request);

                return Json(new
                {
                    success = true,
                    itemId = episodeItem.ID.ToString(),
                    itemPath = episodeItem.Paths.FullPath,
                    guid = episodeItem["GUID"]
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost]
        public ActionResult UpdateEpisode(
    EpisodeUpdateRequest request)
        {
            try
            {
                PodcastContentService service =
                    new PodcastContentService();

                Sitecore.Data.Items.Item episodeItem =
                    service.UpdateEpisode(request);

                return Json(new
                {
                    success = true,
                    itemId = episodeItem.ID.ToString(),
                    itemPath = episodeItem.Paths.FullPath,
                    guid = episodeItem["GUID"]
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        private XElement BuildEpisodeElement(
            Item episode,
            XNamespace itunes,
            XNamespace content)
        {
            string title =
                episode["Episode Title"];

            string description =
                episode["Episode Description"];

            string guid =
                episode["GUID"];

            string author =
                episode["Episode Author"];

            if (string.IsNullOrWhiteSpace(author))
            {
                author = episode.Parent["Author"];
            }

            string mediaType =
                episode["Media Type"];

            string episodeType =
                episode["Episode Type"];

            string episodeNumber =
                episode["Episode Number"];

            string seasonNumber =
                episode["Season Number"];

            bool explicitContent =
                episode["Explicit Content"] == "1";

            DateTime publicationDate =
                GetPublicationDate(episode);

            string publicationDateRfc =
                new DateTimeOffset(
                    publicationDate.ToUniversalTime())
                .ToString(
                    "ddd, dd MMM yyyy HH:mm:ss +0000",
                    CultureInfo.InvariantCulture);

            string mediaUrl =
                GetFileUrl(
                    episode,
                    "Media File");
            string episodeArtworkUrl =
                GetImageUrl(
                    episode,
                    "Episode Artwork");

            string mediaMimeType =
                GetMediaMimeType(
                    mediaType);

            long mediaLength =
                GetMediaLength(
                    episode,
                    "Media File");

            XElement enclosure =
                new XElement(
                    "enclosure",

                    new XAttribute(
                        "url",
                        mediaUrl),

                    new XAttribute(
                        "length",
                        mediaLength),

                    new XAttribute(
                        "type",
                        mediaMimeType));

            XElement item =
                new XElement(
                    "item",

                    new XElement(
                        "title",
                        title),

                    new XElement(
                        "description",
                        description),

                    new XElement(
                        content + "encoded",
                        description),

                    new XElement(
                        "guid",

                        new XAttribute(
                            "isPermaLink",
                            "false"),

                        guid),

                    new XElement(
                        "pubDate",
                        publicationDateRfc),

                    enclosure,

                    BuildArtworkElement(
                        itunes,
                        episodeArtworkUrl),

                    new XElement(
                        itunes + "author",
                        author),

                    new XElement(
                        itunes + "summary",
                        description),

                    new XElement(
                        itunes + "explicit",
                        explicitContent
                            ? "true"
                            : "false"),

                    new XElement(
                        itunes + "episodeType",
                        episodeType));

            if (!string.IsNullOrWhiteSpace(
                    episodeNumber))
            {
                item.Add(
                    new XElement(
                        itunes + "episode",
                        episodeNumber));
            }

            if (!string.IsNullOrWhiteSpace(
                    seasonNumber))
            {
                item.Add(
                    new XElement(
                        itunes + "season",
                        seasonNumber));
            }

            return item;
        }

        // =============================================================
        // Identify Episode
        // =============================================================

        private bool IsEpisode(Item item)
        {
            return item.TemplateID ==
                   Sitecore.Data.ID.Parse(
                       "{5E90D457-5CA9-4E46-AAEE-670AD00711D8}");
        }

        // =============================================================
        // Artwork
        // =============================================================

        private string GetImageUrl(
      Item item,
      string fieldName)
        {
            Sitecore.Data.Fields.ImageField imageField =
                item.Fields[fieldName];

            if (imageField == null ||
                imageField.MediaItem == null)
            {
                return string.Empty;
            }

            string mediaUrl =
                MediaManager.GetMediaUrl(
                    imageField.MediaItem);

            return MakeAbsoluteUrl(mediaUrl);
        }

        private XElement BuildArtworkElement(
            XNamespace itunes,
            string artworkUrl)
        {
            if (string.IsNullOrWhiteSpace(
                    artworkUrl))
            {
                return null;
            }

            return new XElement(
                itunes + "image",

                new XAttribute(
                    "href",
                    artworkUrl));
        }

        // =============================================================
        // Media
        // =============================================================

        private string GetFileUrl(
    Item item,
    string fieldName)
        {
            Sitecore.Data.Fields.FileField fileField =
                item.Fields[fieldName];

            if (fileField == null ||
                fileField.MediaItem == null)
            {
                return string.Empty;
            }

            string mediaUrl =
                MediaManager.GetMediaUrl(
                    fileField.MediaItem);

            return MakeAbsoluteUrl(mediaUrl);
        }
        private string MakeAbsoluteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            if (Uri.IsWellFormedUriString(
                    url,
                    UriKind.Absolute))
            {
                return url;
            }

            string baseUrl =
                Request.Url.GetLeftPart(
                    UriPartial.Authority);

            return baseUrl.TrimEnd('/') +
                   "/" +
                   url.TrimStart('/');
        }
        private long GetMediaLength(
       Item item,
       string fieldName)
        {
            Sitecore.Data.Fields.FileField fileField =
                item.Fields[fieldName];

            if (fileField == null ||
                fileField.MediaItem == null)
            {
                return 0;
            }

            Sitecore.Resources.Media.Media media =
                Sitecore.Resources.Media.MediaManager.GetMedia(
                    fileField.MediaItem);

            if (media == null)
            {
                return 0;
            }

            using (System.IO.Stream stream = media.GetStream().Stream)
            {
                if (stream == null)
                {
                    return 0;
                }

                return stream.Length;
            }
        }

        private string GetMediaMimeType(
            string mediaType)
        {
            if (string.Equals(
                    mediaType,
                    "Video",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "video/mp4";
            }

            return "audio/mpeg";
        }

        // =============================================================
        // Publication date
        // =============================================================

        private DateTime GetPublicationDate(Item episode)
        {
            Sitecore.Data.Fields.DateField dateField =
                episode.Fields["Publication Date"];

            if (dateField == null)
            {
                return DateTime.UtcNow;
            }

            DateTime publicationDate =
                dateField.DateTime;

            if (publicationDate == DateTime.MinValue)
            {
                return DateTime.UtcNow;
            }

            return publicationDate;
        }
    }
}