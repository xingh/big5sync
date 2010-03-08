﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompareAndSync.CompareObject;

namespace CompareAndSync.Visitor
{
    public interface IVisitor
    {
        void Visit(FileCompareObject file, int level, string[] currentPaths);
        void Visit(FolderCompareObject folder, int level, string[] currentPaths);
        void Visit(RootCompareObject root);
    }
}