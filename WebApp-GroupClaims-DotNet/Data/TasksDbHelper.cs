/************************************************************************************************
The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
***********************************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using WebApp_GroupClaims_DotNet.Models;

namespace WebApp_GroupClaims_DotNet.Data
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