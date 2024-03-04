using KLPlugins.RaceEngineer.Car;
using Newtonsoft.Json;

using Xunit.Abstractions;

namespace RaceEngineerPluginTests {
    public class ACCCarSetupTests {

        private readonly ITestOutputHelper _output;

        public ACCCarSetupTests(ITestOutputHelper output) {
            this._output = output;
        }


        [Fact]
        public void ReadFromJson() {
            var fname = "../../../Data/ACC_Test_Setup.json";
            Assert.True(File.Exists(fname), "Test setup file doesn't exist.");
            var txt = File.ReadAllText(fname);
            var Setup = JsonConvert.DeserializeObject<CarSetup>(txt.Replace("\"", "'"));
            Assert.NotNull(Setup); 
            _output.WriteLine(JsonConvert.SerializeObject(Setup, Formatting.Indented));
        }
    }
}