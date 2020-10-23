using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace FreeRedis.Tests
{
    public class CommandFlagsTests : TestBase
    {
        [Fact]
        public void Test01()
        {
            var methodsCount = typeof(RedisClient).GetMethods().Count();
        }

        [Fact]
        public void Command()
        {
			string UFString(string text)
			{
				if (text.Length <= 1) return text.ToUpper();
				else return text.Substring(0, 1).ToUpper() + text.Substring(1, text.Length - 1);
			}

			var rt = cli.Command();
			//var rt = cli.CommandInfo("mset", "mget", "set", "get", "rename");
			var flags = new List<string>();
			var flags7 = new List<string>();
			var diccmd = new Dictionary<string, (string[], string[])>();

			var sb = string.Join("\r\n\r\n", (rt).OrderBy(a1 => (a1 as object[])[0].ToString()).Select(a1 =>
			{
				var a = a1 as object[];
				var cmd = a[0].ToString();
				var plen = int.Parse(a[1].ToString());
				var firstKey = int.Parse(a[3].ToString());
				var lastKey = int.Parse(a[4].ToString());
				var stepCount = int.Parse(a[5].ToString());

				var aflags = (a[2] as object[]).Select(a => a.ToString()).ToArray();
				var fopts = (a[6] as object[]).Select(a => a.ToString()).ToArray();
				flags.AddRange(aflags);
				flags7.AddRange(fopts);

				diccmd.Add(cmd.ToUpper(), (aflags, fopts));

				var parms = "";
				if (plen > 1)
				{
					for (var x = 1; x < plen; x++)
					{
						if (x == firstKey) parms += "string key, ";
						else parms += $"string arg{x}, ";
					}
					parms = parms.Remove(parms.Length - 2);
				}
				if (plen < 0)
				{
					for (var x = 1; x < -plen; x++)
					{
						if (x == firstKey)
						{
							if (firstKey != lastKey) parms += "string[] keys, ";
							else parms += "string key, ";
						}
						else
						{
							if (firstKey != lastKey) parms += $"string[] arg{x}, ";
							else parms += $"string arg{x}, ";
						}
					}
					if (parms.Length > 0)
						parms = parms.Remove(parms.Length - 2);
				}

				return $@"
//{string.Join(", ", a[2] as object[])}
//{string.Join(", ", a[6] as object[])}
public void {UFString(cmd)}({parms}) {{ }}";
			}));
			flags = flags.Distinct().ToList();
			flags7 = flags7.Distinct().ToList();


			var sboptions = new StringBuilder();
            foreach (var cmd in CommandSets._allCommands)
            {
                if (diccmd.TryGetValue(cmd, out var tryv))
                {
                    sboptions.Append($@"
[""{cmd}""] = new CommandSets(");

                    for (var x = 0; x < tryv.Item1.Length; x++)
                    {
                        if (x > 0) sboptions.Append(" | ");
                        sboptions.Append($"ServerFlag.{tryv.Item1[x].Replace("readonly", "@readonly")}");
                    }

                    sboptions.Append(", ");
                    for (var x = 0; x < tryv.Item2.Length; x++)
                    {
                        if (x > 0) sboptions.Append(" | ");
                        sboptions.Append($"ServerTag.{tryv.Item2[x].TrimStart('@').Replace("string", "@string")}");
                    }

                    sboptions.Append(", LocalStatus.none),");
                }
                else
                {
                    sboptions.Append($@"
[""{cmd}""] = new CommandSets(ServerFlag.none, ServerTag.none, LocalStatus.none), ");
                }
            }

            var optioncode = sboptions.ToString();
		}
	}
}
