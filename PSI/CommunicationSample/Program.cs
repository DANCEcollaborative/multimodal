using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Microsoft.Psi.Communication;

namespace CommunicationSample
{
    class Program
    {
        static private CommunicationManager manager;
        static void Main(string[] args)
        {
            try
            {
                manager = new CommunicationManager();
                manager.subscribe("TestPsi", ProcessString);
                manager.subscribe("TestPsi", ProcessBytes);
                manager.subscribe("TestPsi", ProcessMessage);
                manager.subscribe("Bazaar_PSI_Text", ProcessMessage);
                manager.subscribe("Bazaar_PSI_Text", ProcessString);
                manager.SendText("PSI_VHT_Text", "This is PSI!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Console.ReadLine();
            }
        }

        static void ProcessString(string s)
        {
            Console.WriteLine("received text message:" + s);
        }

        static void ProcessBytes(byte[] b)
        {
            Console.WriteLine("received bytes message:" + b.Length + "\n" + Encoding.ASCII.GetString(b));
        }
        
        static void ProcessMessage(IMessage message)
        {
            Console.WriteLine("new message received!");
            Console.WriteLine(message);
        }
    }
}
