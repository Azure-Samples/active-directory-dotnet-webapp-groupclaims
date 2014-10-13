using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebAppGroupClaimsDotNet.DAL;
using WebAppGroupClaimsDotNet.Models;

namespace WebAppGroupClaimsDotNet.Utils
{
    // Handles all interaction with the database for Tasks and Roles.
    // Token database access is handled automatically by ADAL 
    public class DbAccess
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

        // Remove the owner mappings from the db so that the list of owners is refreshed each time someone logs in.
        public static void RemoveExistingOwnerMappings()
        {
            GroupClaimContext db = new GroupClaimContext();
            var existingOwners = from m in db.RoleMappings
                                 where (m.Role == "Owner")
                                 select m;
            db.RoleMappings.RemoveRange(existingOwners.ToList());
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

        // Get all tasks from the db.
        public static List<Task> GetAllTasks()
        {
            GroupClaimContext db = new GroupClaimContext();
            return db.Tasks.ToList();            
        }

        // Add a task to the db.
        public static void AddTask(string taskText)
        {
            GroupClaimContext db = new GroupClaimContext();
            Task newTask = new Task
            {
                Status = "NotStarted",
                TaskText = taskText
            };
            db.Tasks.Add(newTask);
            db.SaveChanges();
        }

        //Update an existing task in the db.
        public static void UpdateTask(int taskId, string status)
        {
            GroupClaimContext db = new GroupClaimContext();
            Task task = db.Tasks.Find(taskId);
            task.Status = status;
            db.SaveChanges();
        }

    }
}