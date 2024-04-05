using Nez.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Threadlock.Models;

namespace Threadlock.Helpers
{
    public class DialogueLoader
    {
        public static List<DialogueLine> GetDialogue(string location, string id)
        {
            if (!File.Exists($"Content/Data/{location}.json"))
                return null;

            var json = File.ReadAllText($"Content/Data/{location}.json");
            var dialogueDictionary = Json.FromJson<Dictionary<string, List<DialogueLine>>>(json);

            if (dialogueDictionary.TryGetValue(id, out var dialogueSet))
                return dialogueSet;

            return null;
        }
    }
}
