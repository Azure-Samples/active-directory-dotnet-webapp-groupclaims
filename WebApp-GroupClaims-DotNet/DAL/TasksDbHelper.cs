using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WebAppGroupClaimsDotNet.Models;

namespace WebAppGroupClaimsDotNet.DAL
{
    public class TasksDbHelper
    {
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

        //Delete a task in the db
        public static void DeleteTask(int taskId)
        {
            GroupClaimContext db = new GroupClaimContext();
            Task task = db.Tasks.Find(taskId);
            db.Tasks.Remove(task);
            db.SaveChanges();
        }
    }
}