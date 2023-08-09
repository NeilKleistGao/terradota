using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using terradota;

const string configurationPath = "../../../Configurations";
const string outputPath = "../../../TerraDota/Items";
string[] configurationFiles = Directory.GetFiles(configurationPath);

foreach (string file in configurationFiles) {
  FileInfo fileInfo = new FileInfo(file);
  string itemName = fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length);
  string outputFile = Path.Combine(outputPath, itemName + ".cs");

  var reader = new StreamReader(fileInfo.FullName);
  string config = reader.ReadToEnd();
  var obj = JObject.Parse(config);
  reader.Close();

  var generator = new Generator(itemName, outputFile);
  if (obj.ContainsKey("description")) { // FIXME: make it work
    generator.Tooltip = obj.GetValue("description").Value<string>();
  }

  generator.Generate();
  reader.Close();
}
