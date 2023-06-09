using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Newtonsoft.Json;

namespace Luna.Cli.Auth
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using (var host = new HostBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddHttpClient("default");
                })
                .Build())
            using (var scope = host.Services.CreateScope())
            {
                var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

                try
                {
                    await UploadFile(httpClientFactory.CreateClient(), "/path/to/your/file.vcf.gz");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        private static async Task UploadFile(HttpClient httpClient, string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                var authnRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://id.lunadna.com/connect/token"),
                    Content = new FormUrlEncodedContent(
                        new Dictionary<string, string>
                        {
                            ["grant_type"] = "client_credentials",
                            ["scope"] = "member.gateway",
                            ["client_id"] = "{YOUR_CLIENT_ID}",
                            ["client_secret"] = "{YOUR_CLIENT_SECRET}"
                        })
                };

                var authnResponse = await httpClient.SendAsync(authnRequest);

                if (!authnResponse.IsSuccessStatusCode)
                    throw new Exception("Failed to authenticate.");

                var accessToken = (JsonConvert.DeserializeAnonymousType(
                    await authnResponse.Content.ReadAsStringAsync(),
                    new
                    {
                        access_token = string.Empty
                    })).access_token;

                var initiateUploadRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri("https://member.lunadna.com/content"),
                    Content = new StringContent(
                        JsonConvert.SerializeObject(new
                        {
                            ParticipantCode = "LN-42424242",
                            ContentTypeTag = "genome-sequence",
                            Source = "{YOUR_CLIENT_ID}",
                            FileSize = fs.Length,
                            Metadata = JsonConvert.SerializeObject(new
                            {
                                FileName = "LN-42424242.vcf.gz"
                            })
                        }),
                        Encoding.UTF8,
                        "application/json")
                };
                initiateUploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var initiateUploadResponse = await httpClient.SendAsync(initiateUploadRequest);

                if (!initiateUploadResponse.IsSuccessStatusCode)
                    throw new Exception("Failed to initiate upload.");

                var uploadParams = JsonConvert.DeserializeAnonymousType(
                    await initiateUploadResponse.Content.ReadAsStringAsync(),
                    new
                    {
                        UploadId = string.Empty,
                        TrackingId = string.Empty,
                        PartCount = 0,
                        PartSize = 0,
                        Urls = new string[] {}
                    });

                var partBuffer = new byte[uploadParams.PartSize];
                var partEtags = new List<string>();
                var filePosition = 0;

                for (var i = 0; i < uploadParams.PartCount; i++)
                {
                    var bytesReadCount = await fs.ReadAsync(partBuffer, filePosition, uploadParams.PartSize);

                    var partUploadRequest = new HttpRequestMessage()
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri(uploadParams.Urls[i]),
                    };

                    partUploadRequest.Content = new ByteArrayContent(partBuffer, 0, bytesReadCount);
                    partUploadRequest.Content.Headers.ContentType =
                        new MediaTypeHeaderValue("binary/octet-stream");

                    var partUploadResponse = await httpClient.SendAsync(
                        partUploadRequest,
                        HttpCompletionOption.ResponseHeadersRead);

                    if (uploadParams.PartCount > 1)
                        partEtags.Add(partUploadResponse.Headers.GetValues("ETag").FirstOrDefault());
                }

                if (uploadParams.PartCount == 1)
                    return;

                var completeUploadRequest = new HttpRequestMessage()
                {
                    Method = HttpMethod.Put,
                    RequestUri = new Uri("https://member.lunadna.com/content"),
                    Content = new StringContent(
                        JsonConvert.SerializeObject(new
                        {
                            UploadId = uploadParams.UploadId,
                            TrackingId = uploadParams.TrackingId,
                            Parts = partEtags
                        }),
                        Encoding.UTF8,
                        "application/json")
                };

                // TODO: check token and refresh if necessary
                completeUploadRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var completeUploadResponse = await httpClient.SendAsync(completeUploadRequest);

                if (!completeUploadResponse.IsSuccessStatusCode)
                    throw new Exception("Failed to complete upload.");
            }
        }
    }
}
