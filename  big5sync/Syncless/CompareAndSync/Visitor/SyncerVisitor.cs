﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompareAndSync.CompareObject;
using System.IO;

namespace CompareAndSync.Visitor
{
    public class SyncerVisitor : IVisitor
    {
        #region IVisitor Members

        public void Visit(FileCompareObject file, int level, string[] currentPaths)
        {
            int maxPriorityPos = 0;
            for (int i = 0; i < currentPaths.Length; i++)
            {
                if (file.Priority[i] > file.Priority[maxPriorityPos])
                    maxPriorityPos = i;
            }

            if (file.Priority[maxPriorityPos] > 0)
            {
                switch (file.ChangeType[maxPriorityPos])
                {
                    case MetaChangeType.Delete:
                        DeleteFile(file, currentPaths, maxPriorityPos);
                        break;
                    case MetaChangeType.New:
                    case MetaChangeType.Update:
                        CopyFile(file, currentPaths, maxPriorityPos);
                        break;

                    case MetaChangeType.Rename:

                        break;
                }
            }

            //Basic logic: Look for highest priority and propagate it.

        }

        public void Visit(FolderCompareObject folder, int level, string[] currentPaths)
        {
            int maxPriorityPos = 0;
            for (int i = 0; i < currentPaths.Length; i++)
            {
                if (folder.Priority[maxPriorityPos] > folder.Priority[i])
                    maxPriorityPos = i;
            }

            if (folder.Priority[maxPriorityPos] > 0)
            {
                switch (folder.ChangeType[maxPriorityPos])
                {
                    case MetaChangeType.Delete:
                        DeleteFolder(folder, currentPaths, maxPriorityPos);
                        break;
                    case MetaChangeType.New:
                        CreateFolder(folder, currentPaths, maxPriorityPos);
                        break;
                }
            }
        }

        public void Visit(RootCompareObject root)
        {
            //Do nothing
        }

        #endregion

        #region File Methods

        private void CopyFile(FileCompareObject fco, string[] currentPaths, int srcFilePos)
        {
            string src = Path.Combine(currentPaths[srcFilePos], fco.Name);
            bool fileExists = false;

            for (int i = 0; i < currentPaths.Length; i++)
            {
                if (i != srcFilePos)
                {                    
                    try
                    {
                        if (fco.Priority[i] != fco.Priority[srcFilePos])
                        {
                            fileExists = File.Exists(Path.Combine(currentPaths[i], fco.Name));
                            File.Copy(src, Path.Combine(currentPaths[i], fco.Name), true);
                            fco.CreationTime[i] = fco.CreationTime[srcFilePos];
                            fco.Exists[i] = true;
                            if (fileExists)
                                fco.FinalState[i] = FinalState.Updated;
                            else
                                fco.FinalState[i] = FinalState.Created;
                            fco.Hash[i] = fco.Hash[srcFilePos];
                            fco.LastWriteTime[i] = fco.LastWriteTime[srcFilePos];
                            fco.Length[i] = fco.LastWriteTime[srcFilePos];
                        }
                        else
                        {
                            fco.FinalState[i] = FinalState.Unchanged;
                        }
                    }
                    catch (Exception)
                    {
                        //Throw to conflict queue
                    }
                }
            }
            fco.FinalState[srcFilePos] = FinalState.Propagated;
        }

        private void DeleteFile(FileCompareObject fco, string[] currentPaths, int srcFilePos)
        {
            for (int i = 0; i < currentPaths.Length; i++)
            {
                if (i != srcFilePos)
                {
                    if (fco.Priority[i] != fco.Priority[srcFilePos])
                    {
                        try
                        {
                            File.Delete(Path.Combine(currentPaths[i], fco.Name));
                            fco.Exists[i] = false;
                            fco.FinalState[i] = FinalState.Deleted;
                        }
                        catch (Exception)
                        {
                            //Throw to conflict queue
                        }
                    }
                    else
                    {
                        fco.FinalState[i] = FinalState.Unchanged;
                    }
                }
            }
            fco.FinalState[srcFilePos] = FinalState.Propagated;
        }

        #endregion

        #region Folder Methods

        private void CreateFolder(FolderCompareObject folder, string[] currentPaths, int srcFilePos)
        {
            for (int i = 0; i < currentPaths.Length; i++)
            {
                if (i != srcFilePos)
                {
                    if (!Directory.Exists(Path.Combine(currentPaths[i], folder.Name)))
                    {
                        try
                        {
                            Directory.CreateDirectory(Path.Combine(currentPaths[i], folder.Name));
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        private void DeleteFolder(FolderCompareObject folder, string[] currentPaths, int srcFilePos)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
