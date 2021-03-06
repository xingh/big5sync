﻿/*
 * 
 * Author: Soh Yuan Chin
 * 
 */

using System.Collections.Generic;
using Syncless.CompareAndSync.Manual.CompareObject;
using Syncless.CompareAndSync.Manual.Visitor;
using Syncless.Notification;

namespace Syncless.CompareAndSync.Manual
{
    /// <summary>
    /// <c>CompareObjectHelper</c> contains methods to traverse the tree of file and folder objects.
    /// </summary>
    public class CompareObjectHelper
    {
        /// <summary>
        /// Specifies whether to use pre or post traversal
        /// </summary>
        private enum TraverseType
        {
            Post,
            Pre
        }

        /// <summary>
        /// Traverse the tree in level order, starting from the <see cref="RootCompareObject"/>.
        /// </summary>
        /// <param name="root">The <see cref="RootCompareObject"/> to start level-traversal from.</param>
        /// <param name="visitor">The <see cref="IVisitor"/> that will be used to visit the tree.</param>
        /// <param name="syncProgress">The <see cref="Progress"/> object to pass in.</param>
        public static void LevelOrderTraverseFolder(RootCompareObject root, IVisitor visitor, Progress syncProgress)
        {
            LevelOrderTraverseFolder(root, root.Paths.Length, visitor, syncProgress);
        }

        private static void LevelOrderTraverseFolder(RootCompareObject root, int numOfPaths, IVisitor visitor, Progress syncProgress)
        {
            Queue<BaseCompareObject> levelQueue = new Queue<BaseCompareObject>();
            RootCompareObject rt;
            FolderCompareObject folder = null;

            levelQueue.Enqueue(root);

            while (levelQueue.Count > 0)
            {
                if (syncProgress != null && syncProgress.State == SyncState.Cancelled)
                    return;

                BaseCompareObject currObj = levelQueue.Dequeue();

                if ((rt = currObj as RootCompareObject) != null)
                    visitor.Visit(rt);
                else if ((folder = currObj as FolderCompareObject) != null)
                    visitor.Visit(folder, numOfPaths);
                else
                    visitor.Visit(currObj as FileCompareObject, numOfPaths);

                Dictionary<string, BaseCompareObject>.ValueCollection values;
                if (rt != null)
                {
                    values = rt.Contents.Values;
                    foreach (BaseCompareObject o in values)
                        levelQueue.Enqueue(o);
                }
                else if (folder != null)
                {
                    values = folder.Contents.Values;
                    foreach (BaseCompareObject o in values)
                        levelQueue.Enqueue(o);
                }

            }
        }

        /// <summary>
        /// Traverse the tree in pre-order, starting from the <see cref="RootCompareObject"/>.
        /// </summary>
        /// <param name="root">The <see cref="RootCompareObject"/> to start level-traversal from.</param>
        /// <param name="visitor">The <see cref="IVisitor"/> that will be used to visit the tree.</param>
        /// <param name="syncProgress">The <see cref="Progress"/> object to pass in.</param>
        public static void PreTraverseFolder(RootCompareObject root, IVisitor visitor, Progress syncProgress)
        {
            TraverseFolderHelper(root, visitor, TraverseType.Pre, syncProgress);
        }

        /// <summary>
        /// Traverse the tree in post-order, starting from the <see cref="RootCompareObject"/>.
        /// </summary>
        /// <param name="root">The <see cref="RootCompareObject"/> to start level-traversal from.</param>
        /// <param name="visitor">The <see cref="IVisitor"/> that will be used to visit the tree.</param>
        /// <param name="syncProgress">The <see cref="Progress"/> object to pass in.</param>
        public static void PostTraverseFolder(RootCompareObject root, IVisitor visitor, Progress syncProgress)
        {
            TraverseFolderHelper(root, visitor, TraverseType.Post, syncProgress);
        }

        // Method will return any time the state of the sync progress becomes cancelled.
        private static void TraverseFolderHelper(RootCompareObject root, IVisitor visitor, TraverseType type, Progress syncProgress)
        {
            if (syncProgress != null && syncProgress.State == SyncState.Cancelled)
                return;

            if (type == TraverseType.Pre)
                visitor.Visit(root);

            Dictionary<string, BaseCompareObject>.ValueCollection values = root.Contents.Values;
            foreach (BaseCompareObject o in values)
            {
                if (syncProgress != null && syncProgress.State == SyncState.Cancelled)
                    return;

                FolderCompareObject fco;
                if ((fco = o as FolderCompareObject) != null)
                    TraverseFolderHelper(fco, root.Paths.Length, visitor, type, syncProgress);
                else
                    visitor.Visit(o as FileCompareObject, root.Paths.Length);
            }

            if (type == TraverseType.Post)
                visitor.Visit(root);
        }

        // Method will return any time the state of the sync progress becomes cancelled.
        private static void TraverseFolderHelper(FolderCompareObject folder, int numOfPaths, IVisitor visitor, TraverseType type, Progress syncProgress)
        {
            if (syncProgress != null && syncProgress.State == SyncState.Cancelled)
                return;

            if (type == TraverseType.Pre)
                visitor.Visit(folder, numOfPaths);

            Dictionary<string, BaseCompareObject>.ValueCollection values = folder.Contents.Values;
            foreach (BaseCompareObject o in values)
            {
                if (syncProgress != null && syncProgress.State == SyncState.Cancelled)
                    return;

                FolderCompareObject fco;
                if ((fco = o as FolderCompareObject) != null)
                    TraverseFolderHelper(fco, numOfPaths, visitor, type, syncProgress);
                else
                    visitor.Visit(o as FileCompareObject, numOfPaths);
            }

            if (type == TraverseType.Post)
                visitor.Visit(folder, numOfPaths);
        }
    }
}