using Xunit;

namespace Jither.Imuse.Scripting
{
    public class FileTests
    {
        [Theory]
        [ScriptData("Scripts/ValidScripts.scripts")]
        public void Parses_valid_scripts(string section, string source)
        {
            var parser = new ImuseScriptParser(source);
            var script = parser.Parse();
            Assert.NotNull(script);
        }
    }
}
