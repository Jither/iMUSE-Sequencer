using System;

namespace Jither.Imuse.Commands
{
    /// <summary>
    /// Indicates that the method should not be exposed to interpreter as a command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class NoScriptingAttribute : Attribute
    {

    }
}
