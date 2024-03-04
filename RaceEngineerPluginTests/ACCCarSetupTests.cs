using KLPlugins.RaceEngineer.Car;

using Newtonsoft.Json;

using Xunit.Abstractions;

namespace RaceEngineerPluginTests {
    public class SerializationTests {

        private readonly ITestOutputHelper _output;

        public SerializationTests(ITestOutputHelper output) {
            this._output = output;
        }

        [Fact]
        public void DeserializeCarSetup() {
            var fname = "../../../Data/ACC_Test_Setup.json";
            Assert.True(File.Exists(fname), "Test setup file doesn't exist.");
            var txt = File.ReadAllText(fname);
            var Setup = JsonConvert.DeserializeObject<CarSetup>(txt.Replace("\"", "'"));
            Assert.NotNull(Setup);
            _output.WriteLine(JsonConvert.SerializeObject(Setup, Formatting.Indented));
        }


        [Fact]
        public void SerializeImmutableWheelsData() {
            var data = new ImmutableWheelsData<int>([1, 2, 3, 4]);
            var json = JsonConvert.SerializeObject(data, new ImmutableWheelsDataJsonConverter<int>());
            Assert.Equal("[1,2,3,4]", json);
        }

        [Fact]
        public void DeserializeImmutableWheelsData() {
            var s = "[1,2,3,4]";
            var wheels = JsonConvert.DeserializeObject<ImmutableWheelsData<int>>(s, new ImmutableWheelsDataJsonConverter<int>());
            Assert.NotNull(wheels);
            Assert.Equal(1, wheels.FL);
            Assert.Equal(2, wheels.FR);
            Assert.Equal(3, wheels.RL);
            Assert.Equal(4, wheels.RR);
        }
    }
}