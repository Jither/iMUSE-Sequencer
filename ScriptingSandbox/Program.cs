﻿using Jither.Imuse.Scripting;
using Jither.Imuse.Scripting.Ast;
using System;

namespace ScriptingSandbox
{
    class Program
    {
        static void Main(string[] args)
        {
            string exampleScript = @"define woodtick-theme       = 0
define bar-theme            = 1
define cartographers-theme  = 2
define laundry-theme        = 3
define inn-theme            = 4
define woodshop-theme       = 5

sounds
{
    'wood'      woodtick-theme
    'woodbar'   bar-theme
    'woodcart'  cartographers-theme
    'woodlaun'  laundry-theme
    'woodinn'   inn-theme
    'woodshop'  woodshop-theme
}

action during woodtick-theme
{
    name is 'Enter Bar'
    shortcut is a
    
    enqueue woodtick-theme marker 0
    {
        if largo is pissed-off
        {
            start-music pissed-off-largo-theme
        }
        else
        {
            for a = 1 to 5 ++
            {
                x = a + b * c / (d - e * b)
                jump-to woodtick-theme track random(1,5) beat 4 tick 400
            }
        }
    }
    enqueue woodtick-theme marker 1
    {
        stop-music bar-theme
        start-music bar-theme
    }
}";
            /*
            string singlelineScript = @"define woodtick-theme = 0 define bar-theme = 1 define cartographers-theme = 2 define laundry-theme = 3 " +
                "define inn-theme = 4 define woodshop-theme = 5 sounds { 'wood' woodtick-theme 'woodbar' bar-theme 'woodcart' cartographers-theme 'woodlaun' laundry-theme " +
                "'woodinn' inn-theme 'woodshop' woodshop-theme } action during woodtick-theme { name is 'Enter Bar' shortcut is a enqueue woodtick-theme marker 0 { " +
                "if largo is pissed-off { start-music pissed-off-largo-theme } else { x = a + b * c / (d - e * b) jump-to woodtick-theme track random(1,5) beat 4 tick 400 " +
                "}} enqueue woodtick-theme marker 1 { stop-music bar-theme start-music bar-theme } }";
            */

            string source = exampleScript;

            var parser = new ScriptParser(source);
            try
            {
                var script = parser.Parse();
                var flattener = new ActionFlattener();
                flattener.Execute(script);
                var printer = new AstPrinter();
                Console.WriteLine(printer.Print(script));
            }
            catch (ParserException ex)
            {
                ConsoleColor defaultColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = defaultColor;
                Console.WriteLine();
                OutputSourceWithPosition(source, ex.Range);
            }
        }

        static void OutputSourceWithPosition(string source, SourceRange range)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            Console.Write(source[..range.Start.Index]);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(source[range.Start.Index..range.End.Index]);
            Console.ForegroundColor = defaultColor;
            Console.WriteLine(source[range.End.Index..]);
        }
    }
}
