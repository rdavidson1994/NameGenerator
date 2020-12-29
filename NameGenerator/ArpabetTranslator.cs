using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NameGenerator
{
    public static class JsonUtils
    {
        public static Dictionary<string, string> DictionaryFromJsonStream(Stream stream)
        {
            Dictionary<string, string> lookupTable = new Dictionary<string, string>();
            JsonDocument json = JsonDocument.Parse(stream);
            foreach (var jsonProperty in json.RootElement.EnumerateObject())
            {
                lookupTable[jsonProperty.Name] = jsonProperty.Value.GetString();
            }
            return lookupTable;
        }

        public static Dictionary<string, string> DictionaryFromJsonFile(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return DictionaryFromJsonStream(fs);
            }
        }
    }

    public class ArpabetTranslator
    {
        private Dictionary<string, string> lookupTable;
        public ArpabetTranslator(Dictionary<string, string> lookupTable)
        {
            this.lookupTable = lookupTable;
        }

        public static ArpabetTranslator FromStream(Stream stream)
        {
            return new ArpabetTranslator(JsonUtils.DictionaryFromJsonStream(stream));
        }

        public string TranslateArpabetToIpaXml(string arpabetString)
        {
            string unicodePrimaryStressMark = "&#x2c8;";
            string unicodeSecondaryStressMark = "";//"&#x2cc;";
            string[] phones = arpabetString.Split('|', '-');
            List<string> outputPieces = new List<string>();
            foreach (string phone in phones)
            {
                if (phone == "")
                {
                    continue;
                }
                // Add IPA stress marks to account for ARPAbet stress annotations
                if (phone.EndsWith("1"))
                {
                    outputPieces.Add(unicodePrimaryStressMark);
                }
                else if (phone.EndsWith("2"))
                {
                    outputPieces.Add(unicodeSecondaryStressMark);
                }
                // Then trim the stress annotations away
                string trimmedPhone = phone.TrimEnd('0', '1', '2');
                if (lookupTable.TryGetValue(trimmedPhone, out string? ipaPhone))
                {
                    outputPieces.Add(ipaPhone);
                }
                else
                {
                    throw new Exception($"Unrecognized phone {trimmedPhone}, translation failed.");
                }
            }
            return string.Join("", outputPieces);
        }
    }
}
