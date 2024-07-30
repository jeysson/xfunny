using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XFunny.QAccess
{
    // Summary:
    //     Indicates that a class, property, or field will not be stored in a persistent
    //     data store.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface)]
    public sealed class NonPersistentAttribute : Attribute
    {
        // Summary:
        //     Specifies the System.Type of this instance. This member supports the .NET
        //     Framework infrastructure and cannot be used directly from your code.
        //
        // Returns:
        //     [To be supplied]
        public static readonly Type AttributeType;

        // Summary:
        //     Initializes a new instance of the DevExpress.Xpo.NonPersistentAttribute class.
    }
}
