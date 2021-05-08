using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit.Sdk;

namespace Jither.Imuse.Scripting
{
    public class ScriptDataAttribute : DataAttribute
    {
        private readonly string path;

        public ScriptDataAttribute(string path)
        {
            this.path = path;
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            if (testMethod == null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException($"Could not find script file at path: {path}");
            }

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);

            string section = null;
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                if (line.StartsWith("#"))
                {
                    if (section != null)
                    {
                        yield return new object[] { section, builder.ToString() };
                        builder.Clear();
                    }
                    section = line[1..].Trim();
                }
                else
                {
                    builder.AppendLine(line);
                }
            }

            yield return new object[] { section, builder.ToString() };
        }
    }
}
