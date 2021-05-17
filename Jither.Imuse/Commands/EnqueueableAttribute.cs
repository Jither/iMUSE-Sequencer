using System;

namespace Jither.Imuse.Commands
{
    /// <summary>
    /// Indicates that the command can be queued by iMUSE.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EnqueueableAttribute : Attribute
    {

    }
}
