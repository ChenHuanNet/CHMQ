using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SocketHelper
{
    public class QueueHelper
    {
        /// <summary>
        /// 移除某个话题的队列中的一个成员
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="key"></param>
        /// <param name="item"></param>
        public static void Remove<T>(ref ConcurrentDictionary<string, ConcurrentQueue<T>> queue, string key, T item)
        {
            ConcurrentQueue<T> _cache = new ConcurrentQueue<T>();
            while (true)
            {
                if (queue[key].Count > 0)
                {
                    queue[key].TryDequeue(out T t);
                    if (!t.Equals(item))
                    {
                        _cache.Enqueue(t);
                    }
                }
                else
                {
                    break;
                }
            }
            if (_cache.Count > 0)
            {
                queue[key] = _cache;
            }
            else
            {
                queue.TryRemove(key, out ConcurrentQueue<T> t);
            }
        }
    }
}
