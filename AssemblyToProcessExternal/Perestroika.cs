using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace AssemblyToProcessExternal
{
    public enum ETest
    {
        Skip, 
        Cont
    }

    public class Perestroika
    {
        public Perestroika()
        {

        }

        public string BorisJelcin { get; set; }
        public ETest EnumTest { get; set; }
        public IList<string> List { get; set; }
    }
        
}
