using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using System.IO;

public class Config {
    private static Dictionary<string, JSONNode> configs = new Dictionary<string, JSONNode>();

    public static JSONNode GetConfig(string configName) {
        if (!configs.ContainsKey(configName))
        {
            TextAsset config = Resources.Load<TextAsset>("Config/" + configName);
            configs[configName] = JSON.Parse(config.text);
        }
        return configs[configName]; 
    }
}