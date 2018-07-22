using System.Collections.Generic;

namespace LINEChannelConnector
{
    public static class CacheService
    {
        public static Dictionary<string, object> caches;

        static CacheService()
        {
            caches = new Dictionary<string, object>();
        }
    }
}
