using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BlinkStripControl
{
    class MyWebAPIMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var task = new Task<HttpResponseMessage>(() =>
            {
                var resMsg = new HttpResponseMessage();
                resMsg.Content = new StringContent("Hello World!");
                return resMsg;
            });

            task.Start();
            return task;
        }
    }
}
