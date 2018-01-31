using System;

namespace EastFive.Xrm
{
    [AttributeUsage(AttributeTargets.Method)]
    public class XrmEntryAttribute : Attribute
    {
        public XrmEntryAttribute()
        {
        }
    }
}
