using System.Collections.Generic;

namespace George
{
    //Container for messages. Has an ID and stores and gets a message for a specific language.
    class MessageCode
    {
        public Dictionary<string, string> messagesByLanguage; //Never set because it's imported from the json file
        public string MessageID { get; set; }

        //Returns a message given a language.
        public string GetMessage(string language)
        {
            if (messagesByLanguage.TryGetValue(language, out string value))
                return value;
            else
                return $"{MessageID} - Message does not exist in given language.";
        }
    }
}
