using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.YouTube.v3.Data;
using Google.Apis.Util;

namespace youtube_playlist_sorter
{
    public class SortPlaylist
    {
        public async Task<YouTubeService> AuthorizeYouTubeAPI(string internalUserName)
        {
            //Get the YT OAuth client info
            string secretsJson = File.ReadAllText("secrets.json");
            JsonElement ytOAuthJson = JsonDocument.Parse(secretsJson).RootElement.GetProperty("YoutubeOAuth");
            YoutubeOAuth ytOAuth = JsonSerializer.Deserialize<YoutubeOAuth>(ytOAuthJson);

            //Setup the credentials
            var credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = ytOAuth.ClientID,
                    ClientSecret = ytOAuth.ClientSecret
                },
                new[] { YouTubeService.Scope.Youtube },
                internalUserName,
                CancellationToken.None
            );

            //Get the youtube service object
            YouTubeService ytService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credentials,
                ApplicationName = "AppName"
            });

            //Return the service object
            return ytService;
        }

        /// <summary>
        /// Gets user's Private/Public playlists.<br />
        /// Watch later and Like videos playlists are depricated by YouTube's API, so those playlists won't appear in the list.<br />
        /// Playlists that are in the user's playlists page, but are own by other users also won't appear in the list.
        /// </summary>
        /// <param name="ytService"></param>
        /// <returns></returns>
        public async Task<List<Playlist>> GetUsersPlaylists(YouTubeService ytService)
        {
            //Get a list of the playlists
            var playlistsReq = ytService.Playlists.List("snippet,contentDetails,id");
            playlistsReq.Mine = true;//Show the users playlists
            playlistsReq.MaxResults = 50;
            //Loop through all the response pages
            string nextPageToken = null;
            List<Playlist> playlists = new List<Playlist>();
            do
            {
                //Set the page token
                playlistsReq.PageToken = nextPageToken;
                //Make the request
                var playlistsResponse = await playlistsReq.ExecuteAsync();
                //Add the results to the list
                playlists.AddRange(playlistsResponse.Items);
                //Get the next page token
                nextPageToken = playlistsResponse.NextPageToken;
            }
            while (!string.IsNullOrEmpty(nextPageToken));//Loop till we're out of page tokens

            //Return the list of playlists
            return playlists;
        }

        public async Task<List<PlaylistItem>> GetPlaylistItems(YouTubeService ytService, string playlistId, int topCount)
        {
            //Get a list of the playlist items
            var itemsReq = ytService.PlaylistItems.List("snippet,id");
            itemsReq.PlaylistId = playlistId;

            //Loop through the response pages
            string nextPageToken = null;
            List<PlaylistItem> items = new List<PlaylistItem>();
            int countRemaining = topCount;
            do
            {
                //Set the max return count (max 50 at a time)
                int currentIncrement = int.Min(countRemaining, 50);
                itemsReq.MaxResults = currentIncrement;
                countRemaining -= currentIncrement;
                //Set the page token
                itemsReq.PageToken = nextPageToken;
                //Make the request
                var itemsResponse = await itemsReq.ExecuteAsync();
                //Add the results to the list
                items.AddRange(itemsResponse.Items);
                //Get the next page token
                nextPageToken = itemsResponse.NextPageToken;
            }
            while (!string.IsNullOrEmpty(nextPageToken) && countRemaining > 0);//Loop till we're out of page tokens or hit the count limit

            //Return the items
            return items;
        }

        public async Task<IList<Video>> GetVideos(YouTubeService ytService, Repeatable<string> videoIds)
        {
            //Get the videos using the given ids
            var videosReq = ytService.Videos.List("contentDetails,id,localizations,snippet,topicDetails");
            videosReq.Id = videoIds;
            var videoResponse = await videosReq.ExecuteAsync();
            return videoResponse.Items;
        }
        
    }
}
