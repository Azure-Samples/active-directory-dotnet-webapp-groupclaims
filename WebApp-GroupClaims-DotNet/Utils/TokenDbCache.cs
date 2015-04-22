using Microsoft.IdentityModel.Clients.ActiveDirectory;
using WebAppGroupClaimsDotNet.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using WebAppGroupClaimsDotNet.DAL;

namespace WebAppGroupClaimsDotNet.Utils
{
    public class TokenDbCache : TokenCache
    {
        private GroupClaimContext db = new GroupClaimContext();
        string userObjId;
        TokenCacheEntry Cache;

        public TokenDbCache(string userObjectId)
        {
           // associate the cache to the current user of the web app
            userObjId = userObjectId;
            
            this.AfterAccess = AfterAccessNotification;
            this.BeforeAccess = BeforeAccessNotification;
            this.BeforeWrite = BeforeWriteNotification;

            // look up the entry in the DB
            Cache = db.TokenCacheEntries.FirstOrDefault(c => c.userObjId == userObjId);
            // place the entry in memory
            this.Deserialize((Cache == null) ? null : Cache.cacheBits);
        }

        // clean the db of all tokens associated with the user.
        public override void Clear()
        {
            base.Clear();

            var entry = db.TokenCacheEntries.FirstOrDefault(e => e.userObjId == userObjId);
            db.TokenCacheEntries.Remove(entry);
            db.SaveChanges();
        }

        // Notification raised before ADAL accesses the cache.
        // This is your chance to update the in-memory copy from the DB, if the in-memory version is stale
        void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            if (Cache == null)
            {
                // first time access
                Cache = db.TokenCacheEntries.FirstOrDefault(c => c.userObjId == userObjId);
            }
            else
            {   
                // retrieve last write from the DB
                var dbCache = db.TokenCacheEntries.FirstOrDefault(c => c.userObjId == userObjId);
                             
                // if the in-memory copy is older than the persistent copy, update the in-memory copy
                if (dbCache.LastWrite > Cache.LastWrite)
                    Cache = dbCache;
            }
            this.Deserialize((Cache == null) ? null : Cache.cacheBits);
        }
        // Notification raised after ADAL accessed the cache.
        // If the HasStateChanged flag is set, ADAL changed the content of the cache
        void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if state changed
            if (this.HasStateChanged)
            {
                // retrieve last write from the DB
                Cache = db.TokenCacheEntries.FirstOrDefault(e => e.userObjId == userObjId);
                
                if (Cache == null)
                {
                    Cache = new TokenCacheEntry
                    {
                        userObjId = userObjId,
                    };
                }
                Cache.LastWrite = DateTime.Now;
                Cache.cacheBits = this.Serialize();
                
                //// update the DB and the lastwrite                
                db.Entry(Cache).State = Cache.TokenCacheEntryID == 0 ? EntityState.Added : EntityState.Modified;                
                db.SaveChanges();
                this.HasStateChanged = false;
            }
        }
        void BeforeWriteNotification(TokenCacheNotificationArgs args)
        {
            // if you want to ensure that no concurrent write take place, use this notification to place a lock on the entry
        }
    }
}