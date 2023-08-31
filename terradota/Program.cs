using System.Text.Json;
using terradota;

const string configurationPath = "../../../Configurations";
const string outputPath = "../../../TerraDota/Items";
string[] configurationFiles = Directory.GetFiles(configurationPath);

foreach (string file in configurationFiles) {
  FileInfo fileInfo = new FileInfo(file);
  string itemName = fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length);
  string outputFile = Path.Combine(outputPath, itemName + ".cs");

  string config = File.ReadAllText(fileInfo.FullName);
  var data = JsonSerializer.Deserialize<ConfigData>(config);

  var generator = new Generator(itemName, outputFile);
  generator.Tooltip = data.description;
  generator.Default = data.@default;

  generator.Generate();
}
