using Newtonsoft.Json;
using PuppeteerSharp;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleStrawpollBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var strawPollId = ""; //Id to strawpoll - IE https://strawpoll.com/YOURIDHERE
            var vote = ""; //Name of vote option
            int minDelay = 1500; //Min delay milliseconds between votes (random)
            int maxDelay = 3500; //Max delay milliseconds between votes (random)
            int votes = 500; //Amount of votes

            Random rnd = new Random();

            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);

            var url = $"https://strawpoll.com/";

            var options = new LaunchOptions
            {
                Headless = true,
                Timeout = 3000,
            };

            string voteId = string.Empty;

            string requestUrl = "https://api2.strawpoll.com/pollvote";

            using (var browser = await Puppeteer.LaunchAsync(options))
            {
                using (var page = await browser.NewPageAsync())
                {
                    await page.SetRequestInterceptionAsync(true);
                    page.Request += async (sender, e) =>
                    {
                        if (e.Request.Url == requestUrl)
                        {
                            var payload = new Payload()
                            {
                                Method = HttpMethod.Post,
                                PostData = JsonConvert.SerializeObject(new StrawpollData()
                                {
                                    checked_answers = new[] { voteId },
                                    name = null,
                                    poll_hash = strawPollId
                                }),
                                Url = requestUrl
                            };

                            await e.Request.ContinueAsync(payload);
                        }
                        else
                        {
                            await e.Request.ContinueAsync();
                        }
                    };

                    await page.GoToAsync($"{url}{strawPollId}");

                    await page.WaitForSelectorAsync("input[name='poll_answer']");

                    var radioOptions = await page.QuerySelectorAllAsync(".b-radio.radio");

                    foreach (var radioOption in radioOptions)
                    {
                        var controlSpan = await (await radioOption.QuerySelectorAsync("span.control-label"))?.GetPropertyAsync("innerHTML");

                        if (controlSpan?.RemoteObject?.Value?.ToString()?.Trim() == vote)
                        {
                            var input = await (await radioOption.QuerySelectorAsync("input")).GetPropertyAsync("value");

                            if (!string.IsNullOrEmpty(input?.RemoteObject?.Value?.ToString()))
                                voteId = input?.RemoteObject?.Value?.ToString();
                        }
                    }

                    for (int i = 0; i <= votes; i++)
                    {
                        await page.GoToAsync(requestUrl);
                        Console.WriteLine($"Voted #{i + 1} {vote} | {voteId} | {strawPollId}");
                        await page.DeleteCookieAsync();
                        await Task.Delay(rnd.Next(minDelay, maxDelay));
                    }

                    Console.WriteLine($"Finished. Vote attempts: {votes}");
                    Console.ReadLine();
                }
            }
        }
    }

    public class StrawpollData
    {
        public string[] checked_answers { get; set; }
        public string name { get; set; }
        public string poll_hash { get; set; }
    }
}
