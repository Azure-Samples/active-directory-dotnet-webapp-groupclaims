using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web;
using WebAppGroupClaimsDotNet.Models;

namespace WebAppGroupClaimsDotNet.DAL
{
    public class TasksDbHelper
    {
        public static List<Task> GetAllTasks(List<string> objectIds)
        {
            // Get all tasks that the user has created or has been authorized to view.
            GroupClaimContext db = new GroupClaimContext();
            return db.Tasks.Where(
                t => t.SharedWith.Any(
                    a => objectIds.Contains(a.AadObjectID)))
                    .ToList();
        }

        public static Task GetTask(int taskId)
        {
            // Get a specific Task from the db.
            GroupClaimContext db = new GroupClaimContext();
            Task task = db.Tasks.Find(taskId);
            var captureSharedWith = task.SharedWith;
            return task;
        }

        public static void AddTask(string taskText, string userObjectId, string userName)
        {
            // Add a new task to the db
            GroupClaimContext db = new GroupClaimContext();
            Task newTask = new Task
            {
                Status = "NotStarted",
                TaskText = taskText,
                Creator = userObjectId,
                SharedWith = new List<AadObject>(),
                CreatorName = userName,
            };

            // Get the AadObject representing from the user if it exists
            AadObject user = db.AadObjects.Find(userObjectId);
            if (user != null)
            {
                // Update the user's display name in case it has changed
                user.DisplayName = userName;
            }
            else
            {
                user = new AadObject
                {
                    AadObjectID = userObjectId,
                    DisplayName = userName,
                };
            }

            newTask.SharedWith.Add(user);
            db.Tasks.Add(newTask);
            db.SaveChanges();
        }

        public static void UpdateTask(int taskId, string status)
        {
            // Update an existing task in the db
            GroupClaimContext db = new GroupClaimContext();
            Task task = db.Tasks.Find(taskId);
            var captureSharedWith = task.SharedWith;
            if (task == null)
                throw new Exception("Task Not Found in DB");
            task.Status = status;
            db.SaveChanges();
        }

        public static void DeleteTask(int taskId)
        {
            //Delete a task in the db
            GroupClaimContext db = new GroupClaimContext();
            Task task = db.Tasks.Find(taskId);
            db.Tasks.Remove(task);
            db.SaveChanges();
        }

        public static void AddShare(int taskId, string objectId, string displayName)
        {
            //Share a task with a user or group
            GroupClaimContext db = new GroupClaimContext();
            AadObject aadObject = db.AadObjects.Find(objectId);
            if (aadObject != null)
            {
                aadObject.DisplayName = displayName;
            }
            else
            {
                aadObject = new AadObject
                {
                    AadObjectID = objectId,
                    DisplayName = displayName,
                };
            }
            Task task = db.Tasks.Find(taskId);
            List<AadObject> shares = task.SharedWith.ToList();
            shares.Add(aadObject);
            task.SharedWith = shares;
            db.SaveChanges();
        }

        public static void DeleteShare(int taskId, string objectId)
        {
            // Remove access to a task for a user or group
            GroupClaimContext db = new GroupClaimContext();
            Task task = db.Tasks.Find(taskId);
            List<AadObject> shares = task.SharedWith.ToList();
            List<AadObject> aadObjects = shares.Where(a => a.AadObjectID.Equals(objectId)).ToList();
            if (aadObjects.Count > 0)
                shares.Remove(aadObjects.First());
            task.SharedWith = shares;
            db.SaveChanges();
        }
    }
}