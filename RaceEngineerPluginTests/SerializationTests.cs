using System.Reflection;

using KLPlugins.RaceEngineer.Car;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Xunit.Abstractions;

namespace RaceEngineerPluginTests {
    public class SerializationTests {

        private readonly ITestOutputHelper _output;

        public SerializationTests(ITestOutputHelper output) {
            this._output = output;
        }

        [Fact]
        public void Deserialize_CarSetup() {
            var fname = "../../../Data/ACC_Test_Setup.json";
            Assert.True(File.Exists(fname), "Test setup file doesn't exist.");
            var txt = File.ReadAllText(fname);
            var Setup = JsonConvert.DeserializeObject<CarSetup>(txt.Replace("\"", "'"));
            Assert.NotNull(Setup);
            _output.WriteLine(JsonConvert.SerializeObject(Setup, Formatting.Indented));
        }


        [Fact]
        public void Serialize_ImmutableWheelsData() {
            var data = new ImmutableWheelsData<int>([1, 2, 3, 4]);
            var json = JsonConvert.SerializeObject(data, new ImmutableWheelsData<int>.JsonConverter());
            Assert.Equal("[1,2,3,4]", json);
        }

        [Fact]
        public void Deserialize_ImmutableWheelsData() {
            var s = "[1,2,3,4]";
            var wheels = JsonConvert.DeserializeObject<ImmutableWheelsData<int>>(s, new ImmutableWheelsData<int>.JsonConverter());
            Assert.NotNull(wheels);
            Assert.Equal(1, wheels.FL);
            Assert.Equal(2, wheels.FR);
            Assert.Equal(3, wheels.RL);
            Assert.Equal(4, wheels.RR);
        }

        [Fact]
        public void Deserialize_TyreInfoPartial() {
            var fname = "../../../Data/TyreInfoPartial.json";
            Assert.True(File.Exists(fname), "Test setup file doesn't exist.");
            var txt = File.ReadAllText(fname);
            var tyreInfo = JsonConvert.DeserializeObject<TyreInfo.Partial>(txt.Replace("\"", "'"));

            Assert.Equal("D", tyreInfo?.ShortName);

            var presLut = new Lut([(25.9, -1.0), (26.9, 0.0), (27.5, 0.0), (28.5, 1.0)]);
            Assert.Equal(presLut, tyreInfo?.IdealPresCurve?.F);
            Assert.Equal(presLut, tyreInfo?.IdealPresCurve?.R);

            var tempLut = new Lut([(60.0, -1.0), (70.0, 0.0), (90.0, 0.0), (100.0, 1.0)]);
            Assert.Equal(tempLut, tyreInfo?.IdealTempCurve?.F);
            Assert.Equal(tempLut, tyreInfo?.IdealTempCurve?.R);

            _output.WriteLine(JsonConvert.SerializeObject(tyreInfo, Formatting.Indented));
        }

        [Fact]
        public void Deserialize_TyreInfoFull() {
            var fname = "../../../Data/TyreInfoFull.json";
            Assert.True(File.Exists(fname), "Test setup file doesn't exist.");
            var txt = File.ReadAllText(fname);
            var tyreInfo = JsonConvert.DeserializeObject<TyreInfo.Partial>(txt.Replace("\"", "'"));

            Assert.Equal("D", tyreInfo?.ShortName);

            var presLutF = new Lut([(25.9, -1.0), (26.9, 0.0), (27.5, 0.0), (28.5, 1.0)]);
            var presLutR = new Lut([(26.9, -1.0), (27.9, 0.0), (28.5, 0.0), (29.5, 1.0)]);
            Assert.Equal(presLutF, tyreInfo?.IdealPresCurve?.F);
            Assert.Equal(presLutR, tyreInfo?.IdealPresCurve?.R);

            var tempLutF = new Lut([(60.0, -1.0), (70.0, 0.0), (90.0, 0.0), (100.0, 1.0)]);
            var tempLutR = new Lut([(61.0, -1.0), (71.0, 0.0), (91.0, 0.0), (101.0, 1.0)]);
            Assert.Equal(tempLutF, tyreInfo?.IdealTempCurve?.F);
            Assert.Equal(tempLutR, tyreInfo?.IdealTempCurve?.R);

            _output.WriteLine(JsonConvert.SerializeObject(tyreInfo, Formatting.Indented));
        }

        [Fact]
        public async void Deserialize_CarInfo() {
            var fname = "../../../Data/CarInfo.json";
            Assert.True(File.Exists(fname), "Test setup file doesn't exist.");
            var txt = File.ReadAllText(fname);
            var carInfo = JsonConvert.DeserializeObject<CarInfo.Partial>(txt.Replace("\"", "'"));

            Assert.Equal(true, carInfo?.IsFullyInitializedAC());

            await VerifyJson(JsonConvert.SerializeObject(carInfo, Formatting.Indented));
        }

        [Fact]
        public async void Deserialize_ACCarInfo() {
            var folderPath = "../../../Data/ACCarInfo";
            var carInfo = ACCarInfo.FromFiles(folderPath);
            await VerifyJson(JsonConvert.SerializeObject(carInfo, Formatting.Indented));
        }
    }
}