using System.Collections.Generic;

namespace UIStockChecker.Utils
{
    public static class DiscordCommands
    {
        public static List<string> SplitArgs(string args)
        {
            var argList = new List<string>();

            if (args == null)
            {
                return argList;
            }

            string arg = "";
            bool foundQuotation = false;

            for (int i = 0; i < args.Length; i++)
            {
                string letter = args.Substring(i, 1);

                if (letter.Equals("\""))
                {
                    foundQuotation = foundQuotation ? false : true;
                }

                if (letter.Equals(" ") && !foundQuotation || !foundQuotation && letter.Equals("\""))
                {
                    argList.Add(arg);
                    arg = "";
                    continue;
                }

                if (!letter.Equals("\""))
                {
                    arg += letter;
                }

            }

            return argList;
        }
    }
}