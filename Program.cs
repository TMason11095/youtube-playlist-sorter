using System.Xml;
using youtube_playlist_sorter;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/sortplaylist", async () =>
{
    //Log into the Youtube API
    SortPlaylist playlistSorter = new SortPlaylist();
    var ytService = await playlistSorter.AuthorizeYouTubeAPI("user2");

    //Get the users playlists
    //var playlists = await test.GetUsersPlaylists(ytService);

    //Get the videos from one of the playlists
    var items = await playlistSorter.GetPlaylistItems(ytService, playlistId: "PL6P_tGMt5ar0-9nscsxdLZQQ4l10GVfFE", topCount: 25);
    var videoIds = items.Select(i => i.Snippet.ResourceId.VideoId).ToList();
    var videos = await playlistSorter.GetVideos(ytService, videoIds: videoIds);

    //Get the wiki topics for all the videos
    var wikiTopics = videos.Where(v => v.TopicDetails is not null).SelectMany(v => v.TopicDetails.TopicCategories).Distinct().ToList();
    videos.Where(v => v.TopicDetails?.TopicCategories.Any(c => c == "https://en.wikipedia.org/wiki/Music") ?? false).Select(v => v.Snippet.Title);

    //Show video titles as they do in the browser (uses "en" title if avaialable) (NOTE: Not yet modular for the user's specific default language)
    videos.Select(v => v.Localizations?.Where(l => l.Key == "en").Select(l => l.Value.Title).FirstOrDefault() ?? v.Snippet.Title);

    //Default audio language
    videos.Select(v => $"{v.Snippet.DefaultAudioLanguage ?? "n/a"} - {v.Snippet.Title}");
    //Less commonly set, but Snippet.DefaultLanguage is also set sometimes

    //Snippet.Tags[]
    videos.Where(v => v.Snippet.Tags is not null).SelectMany(v => v.Snippet.Tags).Distinct();

    videos.Select(v => $"{v.ContentDetails.Duration} - {XmlConvert.ToTimeSpan(v.ContentDetails.Duration)} - {v.Snippet.Title}");
    videos.Where(v => XmlConvert.ToTimeSpan(v.ContentDetails.Duration).TotalMinutes >= 10).Select(v => $"{v.ContentDetails.Duration} - {v.Snippet.Title}");

    string breakpoint = "";
})
.WithName("PostSortPlaylist")
.WithOpenApi();

app.Run();
