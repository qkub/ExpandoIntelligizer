![Icon](https://raw.github.com/Fody/BasicFodyAddin/master/Icons/package_icon.png)

      This is a fody add-in that helps transfering Dictionary&lt;string,object&gt; type of data structures into C# properties.
      The reason for this i to have a Dictionary a like repository with strongly typed content.
      In order to have intellisense support it is advised to leave repositories in a separate project. 
      
      ------------------------------------------------------------------------------------------------------------
      Classes need to implement ISeedSource interface:

      public interface ISeedSource
      {
        object GetKeyValue(string key);
        void SetKeyValue(string key,object value);
        IDictionary&lt;string, object&gt; Data {get;}
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
          var expandoDict = Expando as IDictionary&lt;String, Object&gt;     ;
          expandoDict.Add(new KeyValuePair&lt;string, object&gt; ("Perestroika", "HasThisPayload"));
        }
      }
      
      ------------------------------------------------------------------------------------------------------------
      var repository = new SeedRepository();
      var pereContent = repository.Perestroika;  // &lt;----- dictionary key is now a property