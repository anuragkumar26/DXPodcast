using System;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using System.IO;
using Sitecore.Resources.Media;
using GileadCommercialDS.Feature.Podcast.Models;

namespace GileadCommercialDS.Feature.Podcast.Services
{
    public class PodcastContentService
    {
        private static readonly ID PodcastTemplateId =
            new ID("{FAD9B1B7-2579-451A-B1D3-0E4691ADB17F}");

        private const string PodcastsParentPath =
            "/sitecore/content/EDS/MedicalAffairs/MedicalAffairsMaster/Home/Podcasts";

        public Item CreatePodcast(PodcastCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (string.IsNullOrWhiteSpace(request.PodcastTitle))
            {
                throw new ArgumentException(
                    "Podcast Title is required.");
            }

            Database database = Factory.GetDatabase("master");

            Item parentItem =
                database.GetItem(PodcastsParentPath);

            if (parentItem == null)
            {
                throw new InvalidOperationException(
                    "Podcast parent folder was not found: " +
                    PodcastsParentPath);
            }

            string slug =
                GenerateSlug(request.PodcastTitle);

            // Prevent duplicate podcast slugs.
            Item existingPodcast =
                parentItem.Children.FirstOrDefault(
                    x => string.Equals(
                        x["Podcast Slug"],
                        slug,
                        StringComparison.OrdinalIgnoreCase));

            if (existingPodcast != null)
            {
                throw new InvalidOperationException(
                    "A podcast with this slug already exists: " +
                    slug);
            }

            string itemName =
                Sitecore.Data.Items.ItemUtil.ProposeValidItemName(
                    request.PodcastTitle);

            Item templateItem =
                database.GetItem(PodcastTemplateId);

            if (templateItem == null)
            {
                throw new InvalidOperationException(
                    "PodcastMain template was not found.");
            }

            using (new SecurityDisabler())
            {
                Item podcastItem =
                    parentItem.Add(
                        itemName,
                        new TemplateItem(templateItem));

                if (podcastItem == null)
                {
                    throw new InvalidOperationException(
                        "Unable to create PodcastMain item.");
                }

                using (new EditContext(podcastItem))
                {
                    podcastItem["Podcast Title"] =
                        request.PodcastTitle;

                    podcastItem["Podcast Description"] =
                        request.PodcastDescription;

                    podcastItem["Author"] =
                        request.Author;

                    podcastItem["Category"] =
                        request.Category;

                    podcastItem["Language"] =
                        request.Language;

                    podcastItem["Explicit Content"] =
                        request.ExplicitContent ? "1" : "0";

                    podcastItem["Podcast Type"] =
                        request.PodcastType;

                    podcastItem["Website URL"] =
                        request.WebsiteUrl;

                    podcastItem["Copyright"] =
                        request.Copyright;

                    podcastItem["Owner Name"] =
                        request.OwnerName;

                    podcastItem["Owner Email"] =
                        request.OwnerEmail;

                    // System-generated value.
                    podcastItem["Podcast Slug"] =
                        slug;

                    // Upload podcast artwork to Sitecore Media Library.
                    if (request.PodcastArtwork != null &&
                        request.PodcastArtwork.ContentLength > 0)
                    {
                        Item artworkItem =
                            UploadPodcastArtwork(
                                database,
                                request.PodcastArtwork,
                                itemName);

                        if (artworkItem != null)
                        {
                            Sitecore.Data.Fields.ImageField artworkField =
                                podcastItem.Fields["Podcast Artwork"];

                            if (artworkField == null)
                            {
                                throw new InvalidOperationException(
                                    "Podcast Artwork field was not found.");
                            }

                            artworkField.MediaID =
                                artworkItem.ID;
                        }
                    }
                }

                return podcastItem;
            }
        }
        private Item UploadPodcastArtwork(
    Database database,
    System.Web.HttpPostedFileBase artworkFile,
    string podcastItemName)
        {
            if (artworkFile == null ||
                artworkFile.ContentLength <= 0)
            {
                return null;
            }

            Item mediaLibraryRoot =
                database.GetItem("/sitecore/media library");

            if (mediaLibraryRoot == null)
            {
                throw new InvalidOperationException(
                    "Sitecore Media Library root was not found.");
            }

            Item podcastsFolder =
                mediaLibraryRoot.Children.FirstOrDefault(
                    x => string.Equals(
                        x.Name,
                        "Podcasts",
                        StringComparison.OrdinalIgnoreCase));

            if (podcastsFolder == null)
            {
                podcastsFolder =
                    mediaLibraryRoot.Add(
                        "Podcasts",
                        new TemplateItem(
                            database.GetTemplate(
                                Sitecore.TemplateIDs.Folder)));
            }

            string folderName =
                Sitecore.Data.Items.ItemUtil.ProposeValidItemName(
                    podcastItemName);

            Item podcastArtworkFolder =
                podcastsFolder.Children.FirstOrDefault(
                    x => string.Equals(
                        x.Name,
                        folderName,
                        StringComparison.OrdinalIgnoreCase));

            if (podcastArtworkFolder == null)
            {
                podcastArtworkFolder =
                    podcastsFolder.Add(
                        folderName,
                        new TemplateItem(
                            database.GetTemplate(
                                Sitecore.TemplateIDs.Folder)));
            }

            string fileName =
                Path.GetFileName(
                    artworkFile.FileName);

            string mediaItemName =
                Sitecore.Data.Items.ItemUtil.ProposeValidItemName(
                    Path.GetFileNameWithoutExtension(fileName));

            string extension =
                Path.GetExtension(fileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException(
                    "Artwork file must have a valid file extension.");
            }

            Sitecore.Resources.Media.MediaCreatorOptions options =
    new Sitecore.Resources.Media.MediaCreatorOptions
    {
        Database = database,
        Destination =
            podcastArtworkFolder.Paths.FullPath +
            "/" +
            mediaItemName,
        FileBased = false,
        IncludeExtensionInItemName = false,
        Language = Sitecore.Globalization.Language.Parse("en"),
        Versioned = false
    };

            Sitecore.Resources.Media.MediaCreator mediaCreator =
                new Sitecore.Resources.Media.MediaCreator();

            Item mediaItem;

            using (Stream stream = artworkFile.InputStream)
            {
                mediaItem =
                    mediaCreator.CreateFromStream(
                        stream,
                        fileName,
                        options);
            }

            if (mediaItem == null)
            {
                throw new InvalidOperationException(
                    "Unable to upload podcast artwork to Sitecore Media Library.");
            }

            return mediaItem;
        }

        private Item UploadEpisodeArtwork(
    Database database,
    System.Web.HttpPostedFileBase artworkFile,
    string episodeItemName)
        {
            if (artworkFile == null ||
                artworkFile.ContentLength <= 0)
            {
                return null;
            }

            Item mediaLibraryRoot =
                database.GetItem("/sitecore/media library");

            if (mediaLibraryRoot == null)
            {
                throw new InvalidOperationException(
                    "Sitecore Media Library root was not found.");
            }

            Item podcastsFolder =
                mediaLibraryRoot.Children.FirstOrDefault(
                    x => string.Equals(
                        x.Name,
                        "Podcasts",
                        StringComparison.OrdinalIgnoreCase));

            if (podcastsFolder == null)
            {
                podcastsFolder =
                    mediaLibraryRoot.Add(
                        "Podcasts",
                        new TemplateItem(
                            database.GetTemplate(
                                Sitecore.TemplateIDs.Folder)));
            }

            string folderName =
                Sitecore.Data.Items.ItemUtil.ProposeValidItemName(
                    episodeItemName);

            Item episodeArtworkFolder =
                podcastsFolder.Children.FirstOrDefault(
                    x => string.Equals(
                        x.Name,
                        folderName,
                        StringComparison.OrdinalIgnoreCase));

            if (episodeArtworkFolder == null)
            {
                episodeArtworkFolder =
                    podcastsFolder.Add(
                        folderName,
                        new TemplateItem(
                            database.GetTemplate(
                                Sitecore.TemplateIDs.Folder)));
            }

            string fileName =
                Path.GetFileName(
                    artworkFile.FileName);

            string mediaItemName =
                Sitecore.Data.Items.ItemUtil.ProposeValidItemName(
                    Path.GetFileNameWithoutExtension(fileName));

            string extension =
                Path.GetExtension(fileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException(
                    "Artwork file must have a valid file extension.");
            }

            Sitecore.Resources.Media.MediaCreatorOptions options =
                new Sitecore.Resources.Media.MediaCreatorOptions
                {
                    Database = database,
                    Destination =
                        episodeArtworkFolder.Paths.FullPath +
                        "/" +
                        mediaItemName,
                    FileBased = false,
                    IncludeExtensionInItemName = false,
                    Language =
                        Sitecore.Globalization.Language.Parse("en"),
                    Versioned = false
                };

            Sitecore.Resources.Media.MediaCreator mediaCreator =
                new Sitecore.Resources.Media.MediaCreator();

            Item mediaItem;

            using (Stream stream = artworkFile.InputStream)
            {
                mediaItem =
                    mediaCreator.CreateFromStream(
                        stream,
                        fileName,
                        options);
            }

            if (mediaItem == null)
            {
                throw new InvalidOperationException(
                    "Unable to upload episode artwork to Sitecore Media Library.");
            }

            return mediaItem;
        }
        public Item UpdateEpisode(EpisodeUpdateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (string.IsNullOrWhiteSpace(request.EpisodeItemId))
            {
                throw new ArgumentException(
                    "EpisodeItemId is required.");
            }

            ID episodeId;

            if (!ID.TryParse(
                request.EpisodeItemId,
                out episodeId))
            {
                throw new ArgumentException(
                    "Invalid EpisodeItemId: " +
                    request.EpisodeItemId);
            }

            Database database = Factory.GetDatabase("master");

            Item episodeItem =
                database.GetItem(episodeId);

            if (episodeItem == null)
            {
                throw new InvalidOperationException(
                    "Episode item was not found: " +
                    request.EpisodeItemId);
            }

            using (new SecurityDisabler())
            {
                episodeItem.Editing.BeginEdit();

                try
                {
                    episodeItem["Episode Title"] =
                        request.EpisodeTitle;

                    episodeItem["Episode Description"] =
                        request.EpisodeDescription;

                    episodeItem["Episode Author"] =
                        request.EpisodeAuthor;

                    episodeItem["Media Type"] =
                        request.MediaType;

                    episodeItem["Episode Number"] =
                        request.EpisodeNumber.ToString();

                    episodeItem["Season Number"] =
                        request.SeasonNumber.ToString();

                    episodeItem["Episode Type"] =
                        request.EpisodeType;

                    episodeItem["Explicit Content"] =
                        request.ExplicitContent ? "1" : "0";

                    episodeItem["Publication Date"] =
                        Sitecore.DateUtil.ToIsoDate(
                            request.PublicationDate);

                    episodeItem["Publish Status"] =
                        request.PublishStatus;

                    // Replace media file if a new file was uploaded.
                    if (request.MediaFile != null &&
                        request.MediaFile.ContentLength > 0)
                    {
                        Item mediaItem =
                            UploadEpisodeMedia(
                                database,
                                request.MediaFile,
                                episodeItem.Name);

                        if (mediaItem != null)
                        {
                            Sitecore.Data.Fields.FileField fileField =
                                episodeItem.Fields["Media File"];

                            if (fileField == null)
                            {
                                throw new InvalidOperationException(
                                    "Media File field was not found.");
                            }

                            fileField.MediaID =
                                mediaItem.ID;
                        }
                    }

                    // Replace episode artwork if a new image was uploaded.
                    if (request.EpisodeArtwork != null &&
                        request.EpisodeArtwork.ContentLength > 0)
                    {
                        Item artworkItem =
                            UploadEpisodeArtwork(
                                database,
                                request.EpisodeArtwork,
                                episodeItem.Name);

                        if (artworkItem != null)
                        {
                            Sitecore.Data.Fields.ImageField artworkField =
                                episodeItem.Fields["Episode Artwork"];

                            if (artworkField == null)
                            {
                                throw new InvalidOperationException(
                                    "Episode Artwork field was not found.");
                            }

                            artworkField.MediaID =
                                artworkItem.ID;
                        }
                    }

                    episodeItem.Editing.EndEdit();
                }
                catch
                {
                    episodeItem.Editing.CancelEdit();
                    throw;
                }
            }

            return episodeItem;
        }
        public Item CreateEpisode(EpisodeCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (string.IsNullOrWhiteSpace(request.PodcastSlug))
            {
                throw new ArgumentException(
                    "Podcast Slug is required.");
            }

            if (string.IsNullOrWhiteSpace(request.EpisodeTitle))
            {
                throw new ArgumentException(
                    "Episode Title is required.");
            }

            Database database = Factory.GetDatabase("master");

            Item podcastsParent =
                database.GetItem(PodcastsParentPath);

            if (podcastsParent == null)
            {
                throw new InvalidOperationException(
                    "Podcast parent folder was not found: " +
                    PodcastsParentPath);
            }

            Item podcastItem =
                podcastsParent.Children.FirstOrDefault(
                    x => string.Equals(
                        x["Podcast Slug"],
                        request.PodcastSlug,
                        StringComparison.OrdinalIgnoreCase));

            if (podcastItem == null)
            {
                throw new InvalidOperationException(
                    "Podcast was not found for slug: " +
                    request.PodcastSlug);
            }

            Item episodeTemplate =
                database.GetItem(
                    new ID("{5E90D457-5CA9-4E46-AAEE-670AD00711D8}"));

            if (episodeTemplate == null)
            {
                throw new InvalidOperationException(
                    "PodcastEpisodes template was not found.");
            }

            string itemName =
                Sitecore.Data.Items.ItemUtil.ProposeValidItemName(
                    request.EpisodeTitle);

            using (new SecurityDisabler())
            {
                Item episodeItem =
                    podcastItem.Add(
                        itemName,
                        new TemplateItem(episodeTemplate));

                if (episodeItem == null)
                {
                    throw new InvalidOperationException(
                        "Unable to create Podcast Episode item.");
                }

                using (new EditContext(episodeItem))
                {
                    episodeItem["Episode Title"] =
                        request.EpisodeTitle;

                    episodeItem["Episode Description"] =
                        request.EpisodeDescription;

                    episodeItem["Episode Author"] =
                        request.EpisodeAuthor;

                    episodeItem["Media Type"] =
                        request.MediaType;

                    episodeItem["Episode Type"] =
                        request.EpisodeType;

                    episodeItem["Explicit Content"] =
                        request.ExplicitContent ? "1" : "0";

                    episodeItem["Episode Number"] =
                        request.EpisodeNumber.ToString();

                    episodeItem["Season Number"] =
                        request.SeasonNumber.ToString();

                    episodeItem["Publication Date"] =
                        Sitecore.DateUtil.ToIsoDate(
                            request.PublicationDate);

                    episodeItem["Publish Status"] =
                        request.PublishStatus;

                    // Generate the GUID once and store it in Sitecore.
                    episodeItem["GUID"] =
                        Guid.NewGuid().ToString();

                    // Upload media file to Sitecore Media Library.
                    if (request.MediaFile != null &&
                        request.MediaFile.ContentLength > 0)
                    {
                        Item mediaItem =
                            UploadEpisodeMedia(
                                database,
                                request.MediaFile,
                                itemName);

                        if (mediaItem != null)
                        {
                            Sitecore.Data.Fields.FileField fileField =
                                episodeItem.Fields["Media File"];

                            if (fileField == null)
                            {
                                throw new InvalidOperationException(
                                    "Media File field was not found on the Episode template.");
                            }

                            fileField.MediaID =
                                mediaItem.ID;
                        }
                    }

                    // Upload episode artwork to Sitecore Media Library.
                    if (request.EpisodeArtwork != null &&
                        request.EpisodeArtwork.ContentLength > 0)
                    {
                        Item artworkItem =
                            UploadEpisodeArtwork(
                                database,
                                request.EpisodeArtwork,
                                itemName);

                        if (artworkItem != null)
                        {
                            Sitecore.Data.Fields.ImageField artworkField =
                                episodeItem.Fields["Episode Artwork"];

                            if (artworkField == null)
                            {
                                throw new InvalidOperationException(
                                    "Episode Artwork field was not found.");
                            }

                            artworkField.MediaID =
                                artworkItem.ID;
                        }
                    }
                }

                return episodeItem;
            }
        }
        private Item UploadEpisodeMedia(
    Database database,
    System.Web.HttpPostedFileBase mediaFile,
    string episodeItemName)
        {
            if (mediaFile == null ||
                mediaFile.ContentLength <= 0)
            {
                return null;
            }

            Item mediaLibraryRoot =
                database.GetItem("/sitecore/media library");

            if (mediaLibraryRoot == null)
            {
                throw new InvalidOperationException(
                    "Sitecore Media Library root was not found.");
            }

            Item podcastsFolder =
                mediaLibraryRoot.Children.FirstOrDefault(
                    x => string.Equals(
                        x.Name,
                        "Podcasts",
                        StringComparison.OrdinalIgnoreCase));

            if (podcastsFolder == null)
            {
                podcastsFolder =
                    mediaLibraryRoot.Add(
                        "Podcasts",
                        new TemplateItem(
                            database.GetTemplate(
                                Sitecore.TemplateIDs.Folder)));
            }

            string podcastFolderName =
                Sitecore.Data.Items.ItemUtil.ProposeValidItemName(
                    episodeItemName);

            Item episodeFolder =
                podcastsFolder.Children.FirstOrDefault(
                    x => string.Equals(
                        x.Name,
                        podcastFolderName,
                        StringComparison.OrdinalIgnoreCase));

            if (episodeFolder == null)
            {
                episodeFolder =
                    podcastsFolder.Add(
                        podcastFolderName,
                        new TemplateItem(
                            database.GetTemplate(
                                Sitecore.TemplateIDs.Folder)));
            }

            string fileName =
                Path.GetFileName(mediaFile.FileName);

            string mediaItemName =
                Sitecore.Data.Items.ItemUtil.ProposeValidItemName(
                    Path.GetFileNameWithoutExtension(fileName));

            string extension =
                Path.GetExtension(fileName);

            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException(
                    "Media file must have a valid file extension.");
            }

            Sitecore.Resources.Media.MediaCreatorOptions options =
                new Sitecore.Resources.Media.MediaCreatorOptions
                {
                    Database = database,
                    Destination =
                        episodeFolder.Paths.FullPath +
                        "/" +
                        mediaItemName,
                    FileBased = false,
                    IncludeExtensionInItemName = false,
                    Language = Sitecore.Globalization.Language.Parse("en"),
                    Versioned = false
                };

            Sitecore.Resources.Media.MediaCreator mediaCreator =
                new Sitecore.Resources.Media.MediaCreator();

            Item mediaItem;

            using (Stream stream = mediaFile.InputStream)
            {
                mediaItem =
                    mediaCreator.CreateFromStream(
                        stream,
                        fileName,
                        options);
            }

            if (mediaItem == null)
            {
                throw new InvalidOperationException(
                    "Unable to upload episode media to Sitecore Media Library.");
            }

            return mediaItem;
        }

        private string GenerateSlug(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return string.Empty;
            }

            string slug =
                title.Trim().ToLowerInvariant();

            slug =
                System.Text.RegularExpressions.Regex.Replace(
                    slug,
                    @"[^a-z0-9]+",
                    "-");

            slug =
                slug.Trim('-');

            return slug;
        }
    }
}