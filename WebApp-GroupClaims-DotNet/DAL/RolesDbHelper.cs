using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebAppGroupClaimsDotNet.Models;

namespace WebAppGroupClaimsDotNet.DAL
{
    public class RolesDbHelper
    {
        // Add a new role mapping if it doesn't exist already.
        public static void AddRoleMapping(string objectId, string role)
        {
            GroupClaimContext db = new GroupClaimContext();
            var checkIfExists = from m in db.RoleMappings
                                where (m.ObjectId == objectId && m.Role == role)
                                select m;
            if (checkIfExists.ToList().Count > 0)
                return;

            RoleMapping newMap = new RoleMapping
            {
                ObjectId = objectId,
                Role = role
            };
            db.RoleMappings.Add(newMap);
            db.SaveChanges();
        }

        // Get all the role mappings from the db.
        public static List<RoleMapping> GetAllRoleMappings()
        {
            GroupClaimContext db = new GroupClaimContext();
            return db.RoleMappings.ToList();
        }

        // Remove a role mapping if it exists.
        public static void RemoveRoleMapping(int mappingId)
        {
            GroupClaimContext db = new GroupClaimContext();
            RoleMapping rm = db.RoleMappings.Find(mappingId);
            db.RoleMappings.Remove(rm);
            db.SaveChanges();
        }
    }
}