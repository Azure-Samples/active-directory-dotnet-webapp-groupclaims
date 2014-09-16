using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebAppRBACDotNet.DAL;
using WebAppRBACDotNet.Models;

namespace WebAppRBACDotNet.Utils
{
    public class DbAccess
    {
        public static void AddRoleMapping(string objectId, string role)
        {
            RbacContext db = new RbacContext();
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

        public static void RemoveExistingOwnerMappings()
        {
            RbacContext db = new RbacContext();
            var existingOwners = from m in db.RoleMappings
                                 where (m.Role == "Owner")
                                 select m;
            db.RoleMappings.RemoveRange(existingOwners.ToList());
            db.SaveChanges();
        }


        public static List<RoleMapping> GetAllRoleMappings()
        {
            RbacContext db = new RbacContext();
            return db.RoleMappings.ToList();
        }

        public static void RemoveRoleMapping(int mappingId)
        {
            RbacContext db = new RbacContext();
            RoleMapping rm = db.RoleMappings.Find(mappingId);
            db.RoleMappings.Remove(rm);
            db.SaveChanges();
        }

        public static List<Task> GetAllTasks()
        {
            RbacContext db = new RbacContext();
            return db.Tasks.ToList();            
        }

        public static void AddTask(string taskText)
        {
            RbacContext db = new RbacContext();
            Task newTask = new Task
            {
                Status = "NotStarted",
                TaskText = taskText
            };
            db.Tasks.Add(newTask);
            db.SaveChanges();
        }

        public static void UpdateTask(int taskId, string status)
        {
            RbacContext db = new RbacContext();
            Task task = db.Tasks.Find(taskId);
            task.Status = status;
            db.SaveChanges();
        }

    }
}