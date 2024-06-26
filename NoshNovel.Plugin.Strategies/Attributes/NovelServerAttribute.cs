﻿namespace NoshNovel.Plugin.Strategies.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NovelServerAttribute : Attribute
    {
        public string HostName { get; }

        public NovelServerAttribute(string hostName)
        {
            HostName = hostName;
        }
    }
}