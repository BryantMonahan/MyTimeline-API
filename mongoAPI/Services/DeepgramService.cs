using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deepgram;
using Deepgram.Models.Listen.v1.REST;

namespace mongoAPI.Services
{
    public class DeepgramService
    {
        private readonly ListenRESTClient _deepgramClient;
        public DeepgramService()
        {
            Library.Initialize();
            _deepgramClient = new ListenRESTClient();
        }

        async public Task<SyncResponse> TranscribeFileAsync(string url)
        {
            var response = await _deepgramClient.TranscribeUrl(new UrlSource(url), new PreRecordedSchema()
            {
                Model = "nova-3",
                Summarize = "v2"
            });
            if (response == null) throw new Exception("Deepgram API returned a null response");
            return response;
        }
    }
}