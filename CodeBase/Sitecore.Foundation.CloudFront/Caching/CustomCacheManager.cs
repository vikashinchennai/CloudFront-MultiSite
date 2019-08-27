namespace Sitecore.Foundation.CloudFront.Caching
{
    using Sitecore;
    using Sitecore.Caching;
    public static class CustomCacheManager
    {
        private static readonly MyCustomCache Cache;

        static CustomCacheManager()
        {
            Cache = new MyCustomCache("NextGenCustomCache",StringUtil.ParseSizeString("100KB"));
        }

        public static T GetCache<T>(string key)
        {
            return (T)Cache.Getobject(key);
        }

        public static void SetCache(string key, object value)
        {
            Cache.SetObject(key, value);
        }

        public static void ClearAllCache(string key)
        {
            Cache.Remove(key);
        }
    }

    class MyCustomCache : CustomCache
    {
        public MyCustomCache(string name, long maxSize) : base(name, maxSize)
        {

        }

        public new void SetObject(string key, object value)
        {
            base.SetObject(key, value);
        }

        public object Getobject(string key)
        {
            return base.GetObject(key);
        }
    }
}