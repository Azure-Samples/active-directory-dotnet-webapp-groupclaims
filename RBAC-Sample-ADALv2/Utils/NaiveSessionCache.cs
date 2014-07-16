using System.Security.Cryptography;
using System.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace RBACSampleADALv2.Utils
{
    public class NaiveSessionCache : TokenCache
    {
        private static readonly object FileLock = new object();
        private readonly string CacheId = string.Empty;
        private string UserObjectId = string.Empty;

        /// <summary>
        /// Constructor that registers notification callbacks and loads the token cache.
        /// In this application, we are configuring a token cache using cookies so that our
        /// access token can be accessed throughout our application for various needs.
        /// </summary>
        /// <param name="userId">The user's objectID</param>
        public NaiveSessionCache(string userId)
        {
            UserObjectId = userId;
            CacheId = UserObjectId + "_TokenCache";

            AfterAccess = AfterAccessNotification;
            BeforeAccess = BeforeAccessNotification;
            lock (FileLock)
            {
                object cache = HttpContext.Current.Session[CacheId];
                Deserialize(cache != null
                    ? ProtectedData.Unprotect((byte[]) cache, null, DataProtectionScope.LocalMachine)
                    : null);
            }
        }

        /// <summary>
        /// Empties the persistent store.
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            HttpContext.Current.Session.Remove(CacheId);
        }

        /// <summary>
        /// Triggered right before ADAL needs to access the cache.
        /// Reload the cache from the persistent store in case it changed since the last access.
        /// </summary>
        /// <param name="args">Arguments for the TokenCacheNotification</param>
        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                object cache = HttpContext.Current.Session[CacheId];
                Deserialize(cache != null
                    ? ProtectedData.Unprotect((byte[]) cache, null, DataProtectionScope.LocalMachine)
                    : null);
            }
        }

        /// <summary>
        /// Triggered right after ADAL accessed the cache.
        /// </summary>
        /// <param name="args">Arguments for the TokenCacheNotification</param>
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (HasStateChanged)
            {
                lock (FileLock)
                {
                    // reflect changes in the persistent store
                    HttpContext.Current.Session[CacheId] = ProtectedData.Protect(Serialize(), null,
                        DataProtectionScope.LocalMachine);

                    // once the write operation took place, restore the HasStateChanged bit to false
                    HasStateChanged = false;
                }
            }
        }
    }
}