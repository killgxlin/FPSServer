using System;
using System.Collections.Generic;
using System.Threading;

namespace Share.Utils
{
    public class ConsoleCmdHandler
    {
        private readonly List<string> cmds = new List<string>();

        public Action<string[]> OnCmd;

        public ConsoleCmdHandler()
        {
            ThreadPool.QueueUserWorkItem(obj =>
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
    }
}