using System;
using System.Collections.Generic;
using System.Threading;

namespace Share.Utils
{
    public class ConsoleCmdHandler
    {
        List<string> cmds = new List<string>();

        public ConsoleCmdHandler()
        {
            ThreadPool.QueueUserWorkItem((object obj) =>
            {
                while (true)
                {
                    var l = Console.ReadLine();
                    lock (cmds)
                    {
                        cmds.Add(l);
                    }
                }
            });
        }

        public void Update()
        {
            lock (cmds)
            {
                cmds.ForEach(cmd =>
                {
                    var arg = cmd.Split(' ');
                    if (arg.Length == 0)
                        return;

                    OnCmd(arg);
                });
                cmds.Clear();
            }
        }

        public Action<string[]> OnCmd;
    }
}
