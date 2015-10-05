using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyToProcess
{
    public class Misko
    {
        private string m_name;

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        protected static string m_statName;

        public static string StatName
        {
            get { return m_statName; }
            set { m_statName = value; }
        }

    }
}
