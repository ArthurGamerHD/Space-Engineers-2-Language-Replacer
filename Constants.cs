namespace SE2_Language_Replacer;

public static class Constants
{
    public const string PartialDefTemplate =
        "{{\n  \"$Bundles\": {{\n" +
        "    \"System.Runtime\": \"1.0.0.0\",\n" +
        "    \"VRage\": \"1.5.0.1424\"\n" +
        "  }},\n" +
        "  \"$Type\": \"VRage:Keen.VRage.ContentPipeline.Definitions.PartialDefinitionDiff\",\n" +
        "  \"$Value\": {{\n" +
        "    \"BaseDefinition\": \"{0}\",\n" +
        "    \"PartialDefinitionKind\": \"Override\",\n" +
        "    \"Manipulator\": {{\n" +
        "      \"Guid\": \"{1}\",\n" +
        "      \"Resources\": {{\n        \"$DeltaEncoded\": true,\n" +
        "        \"Keys\": [\n {2}\n" +
        "        ],\n" +
        "        \"Changed\": [\n{3}" +
        "        ],\n" +
        "        \"Removed\": []\n" +
        "      }}\n" +
        "    }},\n" +
        "    \"DefinitionType\": \"VRage:Keen.VRage.Library.Localization.CultureInfoAssetDefinition\",\n" +
        "    \"PriorityOverride\": true\n" +
        "  }}\n" +
        "}}";

    public const string PartialDefInsertTemplate =
        "          {{\n" +
        "            \"Kind\": \"Insert\",\n" +
        "            \"Index\": {0},\n" +
        "            \"Value\": {{\n" +
        "              \"ContentId\": \"{1}\",\n" +
        "              \"Asset\": \"{{G}}{2}\"\n" +
        "            }}\n" +
        "          }}";

    public const string CacheFileHandlerTemplate = 
        "      {{\n" +
        "        \"ResourceHandle\": \"{{G}}{0}\",\n" +
        "        \"FileHandle\": \"{1}\"\n" +
        "      }},";
    
    public const string DefaultLocGUID = "df143f5b-70d0-4ccd-8da2-fc1336a83772";
    
    public const string ResourceHandlerGUID = 
        $"        \"ResourceHandle\": \"{{G}}{DefaultLocGUID}\",";
    
}