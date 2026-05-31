using System;
using System.Collections.Generic;

namespace EasySave.Helpers
{
    public static class CommandLineParser
    {
        public static List<int> Parse(string[] args)
        {
            List<int> indices = new List<int>();

            if (args == null || args.Length == 0)
                return indices;

            string arg = args[0].Trim();

            if (arg.Contains("-"))
            {
                ParseRange(arg, indices);
            }
            else if (arg.Contains(";"))
            {
                ParseList(arg, indices);
            }
            else
            {
                ParseSingle(arg, indices);
            }

            return indices;
        }

        private static void ParseRange(string arg, List<int> indices)
        {
            string[] parts = arg.Split('-');

            if (parts.Length != 2)
            {
                Console.WriteLine($"[WARNING] Invalid range format: '{arg}'");
                return;
            }

            bool fromOk = int.TryParse(parts[0].Trim(), out int from);
            bool toOk   = int.TryParse(parts[1].Trim(), out int to);

            if (!fromOk || !toOk || from > to)
            {
                Console.WriteLine($"[WARNING] Invalid range: '{arg}'");
                return;
            }

            for (int i = from; i <= to; i++)
                indices.Add(i);
        }

        private static void ParseList(string arg, List<int> indices)
        {
            string[] parts = arg.Split(';');

            foreach (string part in parts)
            {
                if (int.TryParse(part.Trim(), out int idx))
                    indices.Add(idx);
                else
                    Console.WriteLine($"[WARNING] '{part.Trim()}' is not a number — skipped.");
            }
        }

        private static void ParseSingle(string arg, List<int> indices)
        {
            if (int.TryParse(arg, out int idx))
                indices.Add(idx);
            else
                Console.WriteLine($"[WARNING] Argument '{arg}' not recognized.");
        }
    }
}