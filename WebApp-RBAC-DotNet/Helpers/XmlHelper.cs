using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Serialization;
using WebAppRBACDotNet.Models;

namespace WebAppRBACDotNet.Helpers
{
    public class XmlHelper
    {
        /// <summary>
        /// Get the exising ObjectID<-->Application Role mappings from Roles.Xml
        /// </summary>
        /// <returns>A List that contains a List for each application role, which 
        /// each contain RoleMapElem mapping entries.</returns>
        public static List<List<RoleMapElem>> GetRoleMappingsFromXml()
        {
            // Initialize a List to contain a list for each application role.
            var list = new List<List<RoleMapElem>>();
            for (int i = 0; i < RoleMapElem.Roles.Length; i++)
            {
                list.Add(new List<RoleMapElem>());
            }

            //Make sure the Roles.xml file exists
            if (!File.Exists(RoleMapElem.RoleMapXMLFilePath))
            {
                return list;
            }

            //Read the xml file into the list
            var fs = new FileStream(RoleMapElem.RoleMapXMLFilePath, FileMode.Open);
            XmlReader reader = XmlReader.Create(fs);
            var x = new XmlSerializer(list.GetType());
            list = (List<List<RoleMapElem>>) x.Deserialize(reader);
            fs.Flush();
            reader.Close();
            fs.Close();
            return list;
        }

        /// <summary>
        /// Remove a ObjectID<-->Application Role mapping from Roles.xml
        /// </summary>
        /// <param name="formCollection">The input from the user, indicating which mapping
        /// to remove.</param>
        public static void RemoveRoleMappingsFromXml(FormCollection formCollection)
        {
            // Make sure the Roles.xml file exists
            if (!File.Exists(RoleMapElem.RoleMapXMLFilePath))
            {
                return;
            }

            // Get the Existing Role Mappings
            List<List<RoleMapElem>> roleMappings = GetRoleMappingsFromXml();

            /// Search Roles.xml for the entry with the correct MappingId, and delete it.
            RoleMapElem toRemove = null;
            foreach (string key in formCollection.Keys)
            {
                if (formCollection[key].Equals("delete"))
                {
                    foreach (var list in roleMappings)
                    {
                        foreach (RoleMapElem elem in list)
                        {
                            if (elem.MappingId.Equals(key))
                            {
                                toRemove = elem;
                            }
                        }
                        if (toRemove != null)
                        {
                            list.Remove(toRemove);
                            toRemove = null;
                        }
                    }
                }
            }

            //Rewrite Roles.xml
            var s = new XmlSerializer(roleMappings.GetType());
            TextWriter writer = new StreamWriter(RoleMapElem.RoleMapXMLFilePath);
            s.Serialize(writer, roleMappings);
            writer.Close();
        }

        /// <summary>
        /// Add a role mapping entry to Roles.xml.  Don't add it if an entry for that 
        /// ObjectID<-->Role pair already exists.
        /// </summary>
        /// <param name="role">The Application Role.</param>
        /// <param name="objectId">The ObjectID.</param>
        public static void AppendRoleMappingToXml(string role, string objectId)
        {
            List<List<RoleMapElem>> mappings;
            int mappingId = 0;

            // See if the Roles.xml file exists
            if (File.Exists(RoleMapElem.RoleMapXMLFilePath))
            {
                mappings = GetRoleMappingsFromXml();
                foreach (var roleList in mappings)
                {
                    // Assign the next available mappingId integer to the new mapping
                    mappingId += roleList.Count;
                }
            }

            // If the Roles.xml file DNE, create a new mappings object, and initialize it.
            else
            {
                mappings = new List<List<RoleMapElem>>();
                for (int i = 0; i < RoleMapElem.Roles.Length; i++)
                {
                    mappings.Add(new List<RoleMapElem>());
                }
            }

            // Iterate over each application role
            for (int i = 0; i < RoleMapElem.Roles.Length; i++)
            {
                if (RoleMapElem.Roles[i].Equals(role))
                {
                    bool exists = false;

                    // If the entry already exists, don't append an identical one
                    foreach (RoleMapElem elem in mappings[i])
                    {
                        if (elem.ObjectId == objectId)
                            exists = true;
                    }
                    
                    // Add the new mapping entry
                    if(!exists)
                        mappings[i].Add(new RoleMapElem(objectId, role, mappingId.ToString(CultureInfo.InvariantCulture)));
                }
            }
            
            // Write the updated or new Roles.xml file
            var s = new XmlSerializer(typeof (List<List<RoleMapElem>>));
            TextWriter writer = new StreamWriter(RoleMapElem.RoleMapXMLFilePath);
            s.Serialize(writer, mappings);
            writer.Close();
        }

        /// <summary>
        /// Reads the applicaiton's tasks from Tasks.xml
        /// </summary>
        /// <returns>A list of <see cref="TaskElem"/>.</returns>
        public static List<TaskElem> GetTaskElemsFromXml()
        {
            if (!File.Exists(TaskElem.TasksXMLFilePath)) return new List<TaskElem>();
            var fs = new FileStream(TaskElem.TasksXMLFilePath, FileMode.Open);
            XmlReader reader = XmlReader.Create(fs);
            var x = new XmlSerializer(typeof (List<TaskElem>));
            var list = (List<TaskElem>) x.Deserialize(reader);
            fs.Flush();
            reader.Close();
            fs.Close();
            return list;
        }

        /// <summary>
        /// Adds a new task to Tasks.xml
        /// </summary>
        /// <param name="formCollection">The user input, namely task name and status.</param>
        public static void AppendTaskElemToXml(FormCollection formCollection)
        {
            List<TaskElem> TaskElems = GetTaskElemsFromXml();
            int taskId = TaskElems.Count == 0
                ? 0
                : Convert.ToInt32(TaskElems[TaskElems.Count - 1].TaskId, CultureInfo.InvariantCulture) + 1;
            TaskElems.Add(new TaskElem(taskId.ToString(CultureInfo.InvariantCulture), formCollection["newtask"],
                "NotStarted"));

            var s = new XmlSerializer(typeof (List<TaskElem>));
            TextWriter writer = new StreamWriter(TaskElem.TasksXMLFilePath);
            s.Serialize(writer, TaskElems);
            writer.Close();
        }

        /// <summary>
        /// Changes the status of an existing task.
        /// </summary>
        /// <param name="formCollection">The user input, namely task name and new status.</param>
        public static void ChangeTaskAttribute(FormCollection formCollection)
        {
            List<TaskElem> taskElems = GetTaskElemsFromXml();

            foreach (TaskElem task in taskElems)
            {
                if (formCollection[task.TaskId] != null)
                {
                    task.Status = formCollection[task.TaskId];
                }
            }

            var s = new XmlSerializer(typeof (List<TaskElem>));
            TextWriter writer = new StreamWriter(TaskElem.TasksXMLFilePath);
            s.Serialize(writer, taskElems);
            writer.Close();
        }
    }
}