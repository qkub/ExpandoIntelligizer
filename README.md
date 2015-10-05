![Icon](https://raw.github.com/Fody/BasicFodyAddin/master/Icons/package_icon.png)

      This is a fody add-in that helps transfering Dictionary<string,object> type of data structures into C# properties.
      The reason for this i to have a Dictionary a like repository with strongly typed content.
      In order to have intellisense support it is advised to leave repositories in a separate project. 
      
      ------------------------------------------------------------------------------------------------------------
      Classes need to implement ISeedSource interface:

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
          expandoDict.Add(new KeyValuePair<string, object> ("Perestroika", "HasThisPayload"));
        }
      }
      
      ------------------------------------------------------------------------------------------------------------
      var repository = new SeedRepository();
      var pereContent = repository.Perestroika;  // <----- dictionary key is now a property