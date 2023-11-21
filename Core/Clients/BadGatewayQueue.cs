using Core.Enums;
using System.Collections.Generic;

namespace Core.Clients
{
    public class BadGatewayQueue
    {
        private readonly Queue<XHRWrapper> _queue = new Queue<XHRWrapper>();

        public void Enqueue(XHRWrapper options)
        {
            if (!options.NoQueue && options.Method != HttpMethod.GET)
            {
                options.Retry = true;
                _queue.Enqueue(options);
            }
        }

        public XHRWrapper Dequeue() => _queue.Dequeue();

        public XHRWrapper Peek() => _queue.Peek();

        public int Count => _queue.Count;
    }
}
