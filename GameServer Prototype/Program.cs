#define COPY_JSONDATA

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LiteNetLib;
using LiteNetLib.Utils;
using Network;

using MongoDB.Driver;
using System.Threading.Tasks;

namespace GameServer_Prototype
{
    class Program
    {

#if COPY_JSONDATA
        static string FRONTEND_PATH = @"D:\Lavoro\BattleFox Studio\Alessio\AutoBattler-Prototype\Assets\Data\JSON\";
#endif
        static bool Exit = false;
        static void Main(string[] args)
        {
            Console.Title = "AutoBattler Game Server Prototype";

#if COPY_JSONDATA
            System.IO.File.Copy("JSON Data/Minions/DebugMinions.json", FRONTEND_PATH + "Minions/DebugMinions.json", true);
            System.IO.File.Copy("JSON Data/Generals/DebugGenerals.json", FRONTEND_PATH + "Generals/DebugGenerals.json", true);
            System.IO.File.Copy("JSON Data/Research/DebugResearch.json", FRONTEND_PATH + "Research/DebugResearch.json", true);
#endif

            ServerConsole.Log("Testing db connection");
            MongoClient client = new MongoClient("mongodb://localhost:27017");

            Registry.Initialize();

            Server server = new Server();

            server.Start(9050);

            Task inputThread = Task.Factory.StartNew(HandleInput);

            while (!Exit)
            {
                server.PollEvents();
                Thread.Sleep(15);
            }            
            server.Stop();
            inputThread.Dispose();
        }        

        static void HandleInput()
        {
            while(true)
            {
                ConsoleKeyInfo k = Console.ReadKey(true);
                if (k != null)
                {
                    if (k.Key == ConsoleKey.Q)
                    {
                        Exit = true;
                        break;
                    } else if (k.Key == ConsoleKey.C)
                    {
                        Console.Clear();
                    } else if (k.Key == ConsoleKey.M)
                    {
                        foreach(MinionDataStructure m in Registry.GetAllMinions())
                        {
                            ServerConsole.Log(m.ToJson());
                        }
                    }
                }
            }
        }
    }    
}
