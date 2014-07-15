using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace RBACSampleADALv2.Models
{
    public class TaskElem
    {
        public static string TasksXMLFilePath = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "App_Data", "tasks.xml");
        public static String[] taskStatus = { "NotStarted", "InProgress", "Complete", "Blocked" };

        public string Task { get; set; }
        public string Status { get; set; }
        public string TaskId { get; set; }

        //Parameterless constructor required for xml serialization
        public TaskElem() { }

        public TaskElem(string taskId, string task, string status)
        {
            this.TaskId = taskId;
            this.Task = task;
            this.Status = status;
        }

        //returns list to be displayed in TaskTracker page drop-down menu
        public string[] GetStatusMenu()
        {
            string[] statusList = new string[4];
            statusList[0] = Status;
            int i = 1;
            foreach (string str in taskStatus)
            {
                if (!str.Equals(Status))
                {
                    statusList[i] = str;
                    i++;
                }
            }
            return statusList;
        }
    }
}