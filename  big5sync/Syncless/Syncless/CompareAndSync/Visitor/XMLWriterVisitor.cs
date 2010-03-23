﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Syncless.CompareAndSync.CompareObject;
using System.Xml;
using System.IO;
using Syncless.CompareAndSync.Enum;


namespace Syncless.CompareAndSync.Visitor
{
    public class XMLWriterVisitor : IVisitor
    {
        #region IVisitor Members

        private const string META_DIR = ".syncless";
        private const string XML_NAME = "syncless.xml";
        private const string METADATAPATH = META_DIR + "\\" + XML_NAME;
        private const string FOLDER = "folder";
        private const string FILE = "file";
        private const string NAME_OF_FOLDER = "name_of_folder";
        private const string NODE_NAME = "name";
        private const string NODE_SIZE = "size";
        private const string NODE_HASH = "hash";
        private const string NODE_LAST_MODIFIED = "last_modified";
        private const string NODE_LAST_CREATED = "last_created";
        private const string XPATH_EXPR = "/meta-data";
        private const string LAST_MODIFIED = "/last_modified";
        private const string NODE_TODO = "todo";
        private const string NODE_ACTION = "action";
        private const string NODE_DATE = "date";
        private const string NODE_NEWNAME = "new_name";
        private const int FIRST_POSITION = 0;
        //private static readonly object syncLock = new object();
        private string[] pathList;

        public void Visit(FileCompareObject file, int numOfPaths)
        {
            for (int i = 0; i < numOfPaths; i++) // HANDLE ALL EXCEPT PROPAGATED
            {
                ProcessMetaChangeType(file, i);
            }
        }

        public void Visit(FolderCompareObject folder, int numOfPaths)
        {
            for (int i = 0; i < numOfPaths; i++)
            {
                ProcessFolderFinalState(folder, i);
            }
        }

        public void Visit(RootCompareObject root)
        {
            pathList = root.Paths;
        }

        private void CreateFileIfNotExist(string path)
        {
            string xmlPath = Path.Combine(path, METADATAPATH);
            if (File.Exists(xmlPath))
                return;

            DirectoryInfo di = Directory.CreateDirectory(Path.Combine(path, META_DIR));
            di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
            XmlTextWriter writer = new XmlTextWriter(xmlPath, null);
            writer.Formatting = Formatting.Indented;
            writer.WriteStartDocument();
            writer.WriteStartElement("meta-data");
            writer.WriteElementString("last_modified", (DateTime.Now.Ticks).ToString());
            writer.WriteElementString(NODE_NAME, GetLastFileIndex(path));
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }

        private void ProcessMetaChangeType(FileCompareObject file, int counter)
        {
            FinalState? changeType = file.FinalState[counter];
            
            if (changeType == null)
            {
                HandleNullCases(file, counter);
                return;
            }

            switch (changeType)
            {
                case FinalState.Created:
                    CreateFileObject(file, counter);
                    break;
                case FinalState.Updated:
                    UpdateFileObject(file, counter);
                    break;
                case FinalState.Deleted:
                    DeleteFileObject(file, counter);
                    break;
                case FinalState.Renamed:
                    RenameFileObject(file, counter);
                    break;
                case FinalState.Unchanged:
                    HandleUnchangedOrPropagatedFile(file, counter);
                    break;
                case FinalState.Propagated:
                    HandleUnchangedOrPropagatedFile(file, counter);
                    break;
            }


        }

        private void UpdateLastModifiedTime(XmlDocument xmlDoc)
        {
            XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + LAST_MODIFIED);
            node.InnerText = DateTime.Now.Ticks.ToString();
        }

        private string getFolderString(string filePath)
        {
            string[] splitWords = filePath.Split('\\');
            string folderPath = "";
            for (int i = 0; i < splitWords.Length; i++)
            {
                if (i == splitWords.Length - 1)
                    break;
                if (folderPath.Equals(""))
                    folderPath = splitWords[i];
                else
                    folderPath = folderPath + "\\" + splitWords[i];
            }

            return folderPath;
        }

        private string GetLastFileIndex(string filePath)
        {
            string[] splitWords = filePath.Split('\\');
            string folderPath = "";
            for (int i = 0; i < splitWords.Length; i++)
            {
                if (i == splitWords.Length - 1)
                    return splitWords[i];
            }

            return folderPath;
        }

        private void CreateFileObject(FileCompareObject file, int counter)
        {
            XmlDocument xmlDoc = new XmlDocument();

            string xmlPath = Path.Combine(file.GetSmartParentPath(counter), METADATAPATH);
            CreateFileIfNotExist(file.GetSmartParentPath(counter));
            CommonMethods.LoadXML(ref xmlDoc, xmlPath);
            int position = GetPropagated(file);
            DoFileCleanUp(xmlDoc, file.Name);
            XmlText hashText = xmlDoc.CreateTextNode(file.Hash[position]);
            XmlText nameText = xmlDoc.CreateTextNode(file.Name);
            XmlText sizeText = xmlDoc.CreateTextNode(file.Length[position].ToString());
            XmlText lastModifiedText = xmlDoc.CreateTextNode(file.LastWriteTime[counter].ToString());
            XmlText lastCreatedText = xmlDoc.CreateTextNode(file.CreationTime[counter].ToString());

            XmlElement fileElement = xmlDoc.CreateElement(FILE);
            XmlElement hashElement = xmlDoc.CreateElement(NODE_HASH);
            XmlElement nameElement = xmlDoc.CreateElement(NODE_NAME);
            XmlElement sizeElement = xmlDoc.CreateElement(NODE_SIZE);
            XmlElement lastModifiedElement = xmlDoc.CreateElement(NODE_LAST_MODIFIED);
            XmlElement lastCreatedElement = xmlDoc.CreateElement(NODE_LAST_CREATED);

            hashElement.AppendChild(hashText);
            nameElement.AppendChild(nameText);
            sizeElement.AppendChild(sizeText);
            lastModifiedElement.AppendChild(lastModifiedText);
            lastCreatedElement.AppendChild(lastCreatedText);

            fileElement.AppendChild(nameElement);
            fileElement.AppendChild(sizeElement);
            fileElement.AppendChild(hashElement);
            fileElement.AppendChild(lastModifiedElement);
            fileElement.AppendChild(lastCreatedElement);

            XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR);
            node.AppendChild(fileElement);
            RemoveTodoFile(xmlDoc, file);
            CommonMethods.SaveXML(ref xmlDoc, xmlPath);
        }

        private void UpdateFileObject(FileCompareObject file, int counter)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string xmlPath = Path.Combine(file.GetSmartParentPath(counter), METADATAPATH);
            CreateFileIfNotExist(file.GetSmartParentPath(counter));

            CommonMethods.LoadXML(ref xmlDoc, xmlPath);

            int position = GetPropagated(file);
            XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FILE + "[name=" + CommonMethods.ParseXpathString(file.Name) + "]");
            if (node == null)
            {
                CreateFileObject(file, counter);
                return;
            }

            XmlNodeList childNodeList = node.ChildNodes;
            for (int i = 0; i < childNodeList.Count; i++)
            {
                XmlNode nodes = childNodeList[i];
                if (nodes.Name.Equals(NODE_SIZE))
                {
                    nodes.InnerText = file.Length[position].ToString();
                }
                else if (nodes.Name.Equals(NODE_HASH))
                {
                    nodes.InnerText = file.Hash[position];
                }
                else if (nodes.Name.Equals(NODE_NAME))
                {
                    nodes.InnerText = file.Name;
                }
                else if (nodes.Name.Equals(NODE_LAST_MODIFIED))
                {
                    nodes.InnerText = file.LastWriteTime[counter].ToString();
                }
                else if (nodes.Name.Equals(NODE_LAST_CREATED))
                {
                    nodes.InnerText = file.CreationTime[counter].ToString();
                }
            }
            RemoveTodoFile(xmlDoc, file);
            CommonMethods.SaveXML(ref xmlDoc, xmlPath);
        }

        private void RenameFileObject(FileCompareObject file, int counter)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string xmlPath = Path.Combine(file.GetSmartParentPath(counter), METADATAPATH);
            CreateFileIfNotExist(file.GetSmartParentPath(counter));
            CommonMethods.LoadXML(ref xmlDoc, xmlPath);

            XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FILE + "[name=" + CommonMethods.ParseXpathString(file.Name) + "]");
            if (node == null)
            {
                CreateFileObject(file, counter);
                return;
            }
      //    node.FirstChild.InnerText = file.NewName;
            XmlNode newNode = node.Clone();

            XmlNode todoNode = CreateFileToDo(xmlDoc, file, counter , "Rename" );
            node.AppendChild(todoNode);

            newNode.FirstChild.InnerText = file.NewName;
            XmlNode rootNode = xmlDoc.SelectSingleNode(XPATH_EXPR);
            rootNode.AppendChild(newNode);
            CommonMethods.SaveXML(ref xmlDoc, xmlPath);
        }

        private void DeleteFileObject(FileCompareObject file, int counter)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string xmlPath = Path.Combine(file.GetSmartParentPath(counter), METADATAPATH);
            if (File.Exists(xmlPath))
            {
                CommonMethods.LoadXML(ref xmlDoc, xmlPath);

                XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FILE + "[name=" + CommonMethods.ParseXpathString(file.Name) + "]");
                XmlNode childNode = CreateFileToDo(xmlDoc, file, counter, "Delete");
                node.AppendChild(childNode);
                CommonMethods.SaveXML(ref xmlDoc, xmlPath);
            }
        }

        private XmlNode CreateFileToDo(XmlDocument xmlDoc , FileCompareObject file , int counter , string changeType)
        {
            RemoveTodoFile(xmlDoc, file);
            long now = DateTime.Now.Ticks;
            XmlText actionText = xmlDoc.CreateTextNode(changeType);
            XmlText dateText = xmlDoc.CreateTextNode(now.ToString());

            XmlElement todoElement = xmlDoc.CreateElement(NODE_TODO);
            XmlElement actionElement = xmlDoc.CreateElement(NODE_ACTION);
            XmlElement dateElement = xmlDoc.CreateElement(NODE_DATE);

            actionElement.AppendChild(actionText);
            dateElement.AppendChild(dateText);

            todoElement.AppendChild(actionElement);
            todoElement.AppendChild(dateElement);

            if (changeType.Equals("Rename"))
            {
                XmlText renameText = xmlDoc.CreateTextNode(file.NewName);
                XmlElement renameElement = xmlDoc.CreateElement(NODE_NEWNAME);
                renameElement.AppendChild(renameText);
                todoElement.AppendChild(renameElement);
            }

            return todoElement;
        }

        private XmlNode CreateFolderToDo(XmlDocument xmlDoc, FolderCompareObject folder, int counter, string changeType)
        {
            RemoveTodoFolder(xmlDoc, folder);
            long now = DateTime.Now.Ticks;
            XmlText actionText = xmlDoc.CreateTextNode(changeType);
            XmlText dateText = xmlDoc.CreateTextNode(now.ToString());

            XmlElement todoElement = xmlDoc.CreateElement(NODE_TODO);
            XmlElement actionElement = xmlDoc.CreateElement(NODE_ACTION);
            XmlElement dateElement = xmlDoc.CreateElement(NODE_DATE);

            actionElement.AppendChild(actionText);
            dateElement.AppendChild(dateText);

            todoElement.AppendChild(actionElement);
            todoElement.AppendChild(dateElement);

            if (changeType.Equals("Rename"))
            {
                XmlText renameText = xmlDoc.CreateTextNode(folder.NewName);
                XmlElement renameElement = xmlDoc.CreateElement(NODE_NEWNAME);
                renameElement.AppendChild(renameText);
                todoElement.AppendChild(renameElement);
            }

            return todoElement;
        } 

        private void RemoveTodoFile(XmlDocument xmlDoc, FileCompareObject file)
        {
            XmlNode todoNode = xmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FILE + "[name=" + CommonMethods.ParseXpathString(file.Name) + "]/todo");
            if (todoNode == null)
                return;
            todoNode.ParentNode.RemoveChild(todoNode);
        }

        private void RemoveTodoFolder(XmlDocument xmlDoc, FolderCompareObject folder)
        {
            XmlNode todoNode = xmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FOLDER + "[name=" + CommonMethods.ParseXpathString(folder.Name) + "]/todo");
            if (todoNode == null)
                return;
            todoNode.ParentNode.RemoveChild(todoNode);
        }

        private int GetPropagated(FileCompareObject file)
        {
            FinalState?[] states = file.FinalState;
            for (int i = 0; i < states.Length; i++)
            {
                if (FinalState.Propagated == states[i])
                    return i;
            }

            return -1; // never happen
        }

        private int GetPropagated(FolderCompareObject folder)
        {
            FinalState?[] states = folder.FinalState;
            for (int i = 0; i < states.Length; i++)
            {
                if (FinalState.Propagated == states[i])
                    return i;
            }

            return -1; // never happen
        }


        private void DoFileCleanUp(XmlDocument xmlDoc, string name)
        {
            XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FILE + "[name=" + CommonMethods.ParseXpathString(name) + "]");
            if (node == null)
                return;
            node.ParentNode.RemoveChild(node);
        }

        private void DoFolderCleanUp(XmlDocument xmlDoc, string name)
        {
            XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FOLDER + "[name=" + CommonMethods.ParseXpathString(name) + "]");
            if (node == null)
                return;
            node.ParentNode.RemoveChild(node);
        }

        private void HandleUnchangedOrPropagatedFile(FileCompareObject file, int counter)
        {
            string name = Path.Combine(file.GetSmartParentPath(counter), file.Name);
            if (File.Exists(name)) //CREATE OR UPDATED
            {
                bool metaExist = file.MetaExists[counter];
                bool fileExist = file.Exists[counter];
                if (metaExist == true && fileExist == true) //UPDATE
                {
                    UpdateFileObject(file, counter);
                }
                else  //NEW
                {
                    CreateFileObject(file, counter);
                }
            }
            else                 //DELETE OR RENAME
            {
                if (file.NewName != null)
                    RenameFileObject(file, counter);
                else
                    DeleteFileObject(file, counter);
            }
        }

        private void HandleNullCases(FileCompareObject file , int counter)
        {

            string fullPath = Path.Combine(file.GetSmartParentPath(counter), file.Name);
            if (File.Exists(fullPath))
                return;

            XmlDocument xmlDoc = new XmlDocument();
            string xmlPath = Path.Combine(file.GetSmartParentPath(counter), METADATAPATH);

            CommonMethods.LoadXML(ref xmlDoc, xmlPath);

            XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FILE + "[name=" + CommonMethods.ParseXpathString(file.Name) + "]");
//          if (node != null)
//             node.ParentNode.RemoveChild(node);
            XmlNode todoNode = CreateFileToDo(xmlDoc, file, counter, "Delete");
            node.AppendChild(todoNode);
            CommonMethods.SaveXML(ref xmlDoc, xmlPath);
        }

        private void ProcessFolderFinalState(FolderCompareObject folder, int counter)
        {

            FinalState?[] finalStateList = folder.FinalState;
            FinalState? changeType = finalStateList[counter];
            switch (changeType)
            {
                case FinalState.Created:
                    CreateFolderObject(folder, counter);
                    break;
                case FinalState.Deleted:
                    DeleteFolderObject(folder, counter);
                    break;
                case FinalState.Renamed:
                    RenameFolderObject(folder, counter);
                    break;
                case FinalState.Propagated:
                    HandleUnchangedOrPropagatedFolder(folder, counter);
                    break;
            }
        }


        private void CreateFolderObject(FolderCompareObject folder, int counter)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string xmlPath = Path.Combine(folder.GetSmartParentPath(counter), METADATAPATH);
            CreateFileIfNotExist(folder.GetSmartParentPath(counter));

            CommonMethods.LoadXML(ref xmlDoc, xmlPath);

            DoFolderCleanUp(xmlDoc, folder.Name);
            XmlText nameText = xmlDoc.CreateTextNode(folder.Name);
            XmlElement nameOfFolder = xmlDoc.CreateElement(NODE_NAME);
            XmlElement nameElement = xmlDoc.CreateElement(FOLDER);
            nameOfFolder.AppendChild(nameText);
            nameElement.AppendChild(nameOfFolder);
            XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR);
            node.AppendChild(nameElement);

            string subFolderXML = Path.Combine(folder.GetSmartParentPath(counter), folder.Name);
            CreateFileIfNotExist(subFolderXML);
            RemoveTodoFolder(xmlDoc, folder);
            CommonMethods.SaveXML(ref xmlDoc, xmlPath);
        }

        private void RenameFolderObject(FolderCompareObject folder, int counter)
        {

            if (Directory.Exists(Path.Combine(folder.GetSmartParentPath(counter), folder.Name)))
            {
                XmlDocument xmlDoc = new XmlDocument();
                string xmlPath = Path.Combine(folder.GetSmartParentPath(counter), METADATAPATH);

                CommonMethods.LoadXML(ref xmlDoc, xmlPath);

                XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FOLDER + "[name=" + CommonMethods.ParseXpathString(folder.Name) + "]");
                if (node == null)
                {
                    CreateFolderObject(folder, counter);
                    return;
                }

                //node.FirstChild.InnerText = folder.NewName;
                XmlNode newNode = node;
                newNode.FirstChild.InnerText = folder.NewName;
                XmlNode todoNode = CreateFolderToDo(xmlDoc, folder, counter, "Rename");
                node.AppendChild(todoNode);

                XmlNode rootNode = xmlDoc.SelectSingleNode(XPATH_EXPR);
                rootNode.AppendChild(newNode);
                CommonMethods.SaveXML(ref xmlDoc, xmlPath);
            }
            else
            {
                XmlDocument newXmlDoc = new XmlDocument();
                string editOldXML = Path.Combine(Path.Combine(folder.GetSmartParentPath(counter), folder.NewName), METADATAPATH);

                CommonMethods.LoadXML(ref newXmlDoc, editOldXML);

                XmlNode xmlNameNode = newXmlDoc.SelectSingleNode(XPATH_EXPR + "/name");
                xmlNameNode.InnerText = folder.NewName;
                //CreateFolderToDo(newXmlDoc, folder, counter, "Rename");
                CommonMethods.SaveXML(ref newXmlDoc, editOldXML);

                string parentXML = Path.Combine(folder.GetSmartParentPath(counter), METADATAPATH);
                XmlDocument parentXmlDoc = new XmlDocument();
                CommonMethods.LoadXML(ref parentXmlDoc, parentXML);
                XmlNode parentXmlFolderNode = parentXmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FOLDER + "[name=" + CommonMethods.ParseXpathString(folder.Name) + "]");
                XmlNode newNode = parentXmlFolderNode.Clone();
                newNode.FirstChild.InnerText = folder.NewName;
                XmlNode todoNode = CreateFolderToDo(parentXmlDoc, folder, counter, "Rename");
                parentXmlFolderNode.AppendChild(todoNode);
                XmlNode rootNode = parentXmlDoc.SelectSingleNode(XPATH_EXPR);
                rootNode.AppendChild(newNode);
                CommonMethods.SaveXML(ref parentXmlDoc, Path.Combine(folder.GetSmartParentPath(counter), METADATAPATH));
            }
        }

        private void DeleteFolderObject(FolderCompareObject folder, int counter)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string xmlPath = Path.Combine(folder.GetSmartParentPath(counter), METADATAPATH);
            if (File.Exists(xmlPath))
            {
                CommonMethods.LoadXML(ref xmlDoc, xmlPath);

                XmlNode node = xmlDoc.SelectSingleNode(XPATH_EXPR + "/" + FOLDER + "[name=" + CommonMethods.ParseXpathString(folder.Name) + "]");
       //         if (node == null)
       //             return;
       //         node.ParentNode.RemoveChild(node);
                XmlNode todoNode = CreateFolderToDo(xmlDoc, folder, counter, "Delete");
                node.AppendChild(todoNode);
                CommonMethods.SaveXML(ref xmlDoc, xmlPath);
            }
        }

        private void HandleUnchangedOrPropagatedFolder(FolderCompareObject folder, int counter)
        {
            string name = Path.Combine(folder.GetSmartParentPath(counter), folder.Name);
            if (Directory.Exists(name)) //CREATE 
            {
                bool metaExist = folder.MetaExists[counter];
                bool folderExist = folder.Exists[counter];
                if (folderExist == true) //CREATE
                {
                    CreateFolderObject(folder, counter);
                }
            }
            else                 //DELETE OR RENAME
            {
                if (folder.NewName != null)
                    RenameFolderObject(folder, counter);
                else
                    DeleteFolderObject(folder, counter);
            }
        }

        #endregion
    }
}
