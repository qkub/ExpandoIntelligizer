
using AssemblyToProcessExternal;
using System;
using System.Collections.Generic;
using System.Dynamic;

namespace AssemblyToProcess.SEREPO
{
    public interface ISeedSource
    {
        object GetKeyValue(string key);
        void SetKeyValue(string key,object value);
        IDictionary<string, object> Data {get;}
    }

    public class SeedRepository : ISeedSource
    {
        private ExpandoObject expando = new ExpandoObject();

        public object GetKeyValue(string key)
        {
            return Data[key];
        }

        public void SetKeyValue(string key,object value)
        {
            Data[key] = value;
        }

        public SeedRepository()
        {
            var expandoDict = Expando as IDictionary<String, Object>;
            expandoDict.Add(new KeyValuePair<string, object>("NamedList1", "test"));
            expandoDict.Add(new KeyValuePair<string, object>("SimpleObject", new Misko()));
            expandoDict.Add(new KeyValuePair<string, object>("Perestroika", new Perestroika()));            
        }

        public string NamedList1Or
        {
            get
            {
                return (string)GetKeyValue("NamedList1");
            }
            set
            {
                SetKeyValue("NamedList1", value);
            }
        }

        public string NamedList1Or2
        {
            get
            {
                object retVal = null;
                Data.TryGetValue("NamedList1", out retVal);
                return (string)retVal;
            }
            set
            {
                Data["NamedList1"] = value;
            }
        }

        public ExpandoObject Expando
        {
            get
            {
                return expando;
            }

            set
            {
                expando = value;
            }
        }

        public IDictionary<string, object> Data
        {
            get
            {
                var expandoDict = (this.Expando as IDictionary<string, object>);               
                return expandoDict;
            }
        }
    }
    
   
}
